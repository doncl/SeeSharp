using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Sid.Declarations;

namespace Sid.WebServices
{
    /// <summary>
    /// Restful method dispatcher.
    /// </summary>
    public class RestfulMethodDispatcher
    {
        private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Handler to execute so Sid user can decide if the origin is valid or not.
        public Func<IRequest, Uri, bool> IsValidOrigin { get; set; }

        /// <summary>
        /// These are keyed by HTTP verb.  All the GET's are grouped together, etc.
        /// </summary>
        public static Dictionary<HttpMethod, EndPointGroup> WebMethodEndPoints =
            new Dictionary<HttpMethod, EndPointGroup>();

        /// <summary>
        /// Grovels through the domain, looking for BaseModule types, methods, and parameters.
        /// <param name="sidModuleAssemblies">List of assemblies for Sid to look through (with
        /// reflection) for classes derived from SidModule.
        /// </summary>
        public RestfulMethodDispatcher(IEnumerable<Assembly> sidModuleAssemblies)
        {
            // Get all the BaseModule types and their endpoints.
            var baseModuleTypes = DeclarationHelper.GetModuleTypes(sidModuleAssemblies);
            var endPointDescriptors = GetEndpoints(baseModuleTypes);

            // Group them into verbs, e.g. 'GET', 'POST', 'DELETE', etc.
            GroupEndPointsByMethod(endPointDescriptors);
        }

        /// <summary>
        /// Groups the endpoints by their HTTP method, to make it easier to process them at
        /// request time.
        /// </summary>
        /// <param name="endPointDescriptors">The list of endpoints.</param>
        static void GroupEndPointsByMethod(IEnumerable<EndPointDescriptor> endPointDescriptors)
        {
            var endPointDescriptorByVerb = endPointDescriptors.GroupBy(desc => desc.WebMethod);

            // Now, add them to an EndPointGroup, and build the trie for each one.
            foreach (var grouping in endPointDescriptorByVerb) {
                var endpointGroup = new EndPointGroup(grouping.ToList());
                WebMethodEndPoints[grouping.Key] = endpointGroup;
            }
        }

        /// <summary>
        /// Given the list of BaseModule types, builds up the list of EndPointDescriptors,
        /// including parameters.
        /// </summary>
        /// <param name="baseModuleTypes">The types of the BaseModules.</param>
        /// <returns>Constructed list of endpoint descriptors.</returns>
        IEnumerable<EndPointDescriptor> GetEndpoints(IEnumerable<Type> baseModuleTypes)
        {
            var endPointDescriptors = new List<EndPointDescriptor>();
            foreach (var baseModuleType in baseModuleTypes) {
                var rootPath = DeclarationHelper.GetRootPath(baseModuleType);

                // Get the Endpoints for this module.
                var methodInfos = DeclarationHelper.GetEndpointMethods(baseModuleType);
                foreach (var methodInfo in methodInfos) {
                    endPointDescriptors.Add(new EndPointDescriptor(baseModuleType, methodInfo, 
                        rootPath.Path));
                }
            }
            return endPointDescriptors;
        }

        /// <summary>
        /// This is where the good stuff happens.   Incoming requests are Resolved using the
        /// EndPointGroup.   This is where there is room for potential optimizations; there are
        /// two reflection calls happening at runtime for each request.  
        /// 1)  An instantiation of the BaseModule - this is because each BaseModule has a 
        /// responseHeaders dictionary that cannot be shared.  Obviously, some scheme that 
        /// avoids reflection can be devised.
        /// 2) The actual invocation of the MethodInfo.   
        /// </summary>
        /// <param name="request">An IRequest object encapsulating the request.</param>
        /// <returns>A RestfulResponse object that can be serialized and written to the
        /// response.</returns>
        public RestfulResponse ResolveRequest(IRequest request)
        {
            SidModule moduleInstance = null;
            try {
                // Resolve the endpoint the request is for.
                Regex pathRegex;
                var endPoint = ResolveEndPoint(request, out pathRegex);

                // Instantiate the module.
                moduleInstance = Activator.CreateInstance(endPoint.ModuleType) as SidModule;
                moduleInstance.RestfulRequest = request;
                request.Match = GetPathRegexMatch(request, endPoint, pathRegex);

                // Determine if it's an OPTIONS request or not.
                var optionsRequest = request.HttpMethod == HttpMethod.Options;

                // Create CORS-specific headers if appropriate, or throw Forbidden exception.
                DoCORSHandling(request, moduleInstance.ResponseHeaders, endPoint, 
                    optionsRequest);

                // Get the arguments and execute the method.
                var args = GetArguments(request, endPoint, moduleInstance);
                var ret = endPoint.MethodInfo.Invoke(moduleInstance, args);

                // Create the Access-Control-Expose-Headers header, if the endpoint added any
                // response headers.
                CopyResponseHeadersToAccessControlExposeHeadersKey(optionsRequest,
                        moduleInstance.ResponseHeaders);
        
                // Serialize the return value.
                var json = ret != null ? JsonConvert.SerializeObject(ret) : null;

                // Construct a RestfulResponse object from the return value.
                var successResponse = new RestfulResponse {
                    StatusCode = HttpStatusCode.OK,
                    Body = json,
                    Headers = moduleInstance.ResponseHeaders,
                };

                return successResponse;
            } catch (Exception ex) {
                return HandleDispatchException(ex, moduleInstance);
            }
        }

        /// <summary>
        /// If the endpoint (the specified endpoint if it's not OPTIONS, or the endpoint 
        /// referred to by the OPTIONS pre-flight request if it is) has the [Public] attribute,
        /// then just set the Access-Allow-Control-Allow-Origin to '*'.  Otherwise, invoke the
        /// SID-user-supplied handler for evaluating the Origin.   
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <param name="responseHeaders">Headers to return to caller.</param>
        /// <param name="endPoint">End point descriptor.</param>
        /// <param name="optionsRequest">Is it an OPTIONS request?</param>
        void DoCORSHandling(IRequest request, IDictionary<string, string> responseHeaders, 
                            IEndPointDescriptor endPoint, bool optionsRequest)
        {
            var publicEndpoint = optionsRequest ? GetReferredEndpointPublicStatus(request) :
                                                endPoint.Public;
            // Call the IsValidOrigin handler, so clients can determine if they want to 
            // allow the request.
            if (publicEndpoint) {
                responseHeaders["Access-Control-Allow-Origin"] = "*";
            }
            else {
                InvokeIsValidOriginHandler(request, responseHeaders);
            }
        }

        /// <summary>
        /// Resolves/finds the endpoint described by the request object.  Also returns
        /// </summary>
        /// <returns>The endpoint object.</returns>
        /// <param name="request">Request.</param>
        /// <param name="pathRegex">Optional regex gets generated if there are [Path] 
        /// parameters for this endpoint.</param>
        static IEndPointDescriptor ResolveEndPoint(IRequest request, out Regex pathRegex)
        {
            EndPointGroup group;
            if (!WebMethodEndPoints.TryGetValue(request.HttpMethod, out group)) {
                throw new ApiException(HttpStatusCode.MethodNotAllowed,
                    String.Format("Method {0} not supported", request.HttpMethod));
            }
            // Search in our data structures to resolve the request.
            var endPoint = group.ResolveRequest(request.Url, out pathRegex);
            if (endPoint == null) {
                throw new ApiException(HttpStatusCode.NotFound);
            }
            return endPoint;
        }

        /// <summary>
        // If it's not an OPTIONS request, and we have accumulated some response 
        // headers from the invoked call, copy their keys into a comma-separated string
        // and make that the value of a new Access-Control-Expose-Headers header.
        /// </summary>
        /// <param name="optionsRequest">Is it an OPTIONS request?</param>
        /// <param name="responseHeaders">Response headers.</param>
        static void CopyResponseHeadersToAccessControlExposeHeadersKey(bool optionsRequest, 
            Dictionary<string, string> responseHeaders)
        {
            if (!optionsRequest && responseHeaders.Any()) {
                if (responseHeaders.Any()) {
                    var headers = new List<string>(responseHeaders.Count);
                    headers.AddRange(responseHeaders.Select(header => header.Key));
                    responseHeaders["Access-Control-Expose-Headers"] = 
                        string.Join(",", headers);
                }
            }
        }

        /// <summary>
        /// Code the invokes the Sid user's IsValidOrigin handler.
        /// </summary>
        /// <param name="request">The Sid IRequest object.</param>
        /// <param name="responseHeaders">Response headers we plan to return to caller."
        /// </param>
        void InvokeIsValidOriginHandler(IRequest request, 
                IDictionary<string, string> responseHeaders)
        {
            var forbidCode = HttpStatusCode.Forbidden;

            if (IsValidOrigin == null) {
                throw new ApiException(forbidCode, "No IsValidOrigin handler supplied.");
            }

            Uri origin;
            var originString = request.Headers["Origin"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(originString) ||
                originString.IndexOf("chrome-extension://") == 0) {
                // This request did not come from a browser. We can permit it.
                return;
            }

            if (!Uri.TryCreate(originString, UriKind.Absolute, out origin)) {
                throw new ApiException(forbidCode, 
                    string.Format("Unparseable origin \"{0}\"", originString));
            }

            if (!IsValidOrigin(request, origin)) {
                    throw new ApiException(forbidCode, string.Format("IsValidOrigin handler " +
                    "return false for request {0} {1}, origin: {2}", request.HttpMethod, request.Url, origin));
            }
            responseHeaders["Access-Control-Allow-Origin"] = originString;
        }

        /// <summary>
        /// For an OPTIONS request, we need to check for the Public attribute
        /// on the endpoint for which the OPTIONS request is querying about.
        /// </summary>
        /// <returns>Returns whether the referredEndpoint (the one the OPTIONS request refers
        /// to) has a [Public] attribute or not.</param>
        static bool GetReferredEndpointPublicStatus(IRequest request)
        {
            var publicEndpoint = false;

            var verbString = request.Headers["Access-Control-Request-Method"].FirstOrDefault();
            if (!string.IsNullOrEmpty(verbString) && 
                Request.SupportedHttpMethods.ContainsKey(verbString)) {

                var verb = Request.SupportedHttpMethods[verbString];

                EndPointGroup actualGroup;
                Regex rgx;  // Not used here.

                if (WebMethodEndPoints.TryGetValue(verb, out actualGroup)) {
                    var actualEndPoint = actualGroup.ResolveRequest(request.Url, out rgx);
                    if (actualEndPoint != null) {
                        publicEndpoint = actualEndPoint.Public;
                    }
                }
            }
            return publicEndpoint;
        }

        /// <summary>
        /// Processes parameters, and uses the BaseParamAttribute-derived members of the
        /// ParmWrapper objects for the EndPointDescriptor to polymorphically extract the 
        /// appropriate values for each argument.
        /// </summary>
        /// <param name="request">IRequest object.</param>
        /// <param name="endPoint">Endpoint desciptor; has the ParmWrapper objects.</param>
        /// <param name="moduleInstance">Module for this endpoint.</param>
        /// <returns>An array of objects, suitable for using with MethodInfo.Invoke.</returns>
        static public object[] GetArguments(IRequest request, IEndPointDescriptor endPoint, 
                                     SidModule moduleInstance)
        {
            var args = new object[endPoint.Parameters.Count];
            for (var i = 0; i < args.Length; i++) {
                var parm = endPoint.Parameters[i];
                args[i] = parm.GetParamValue(request, moduleInstance);
            }
            return args;
        }

        /// <summary>
        /// If the path has replaceable tokens, we'll need a Regex Match object, for the 
        /// PathParams to use.   If there are none, this returns null.   This function does
        /// some validation, making sure that if we have PathAttributes, we must have a 
        /// non-null Regex, and that it matches the Url in the request.  
        /// The contract for this function is:  if it returns without error, the caller can be
        /// assured that either there are no PathParameters, OR it has a non-null valid Match
        /// object that has Groups in it that can be used to extract the appropriate values 
        /// from the request.
        /// </summary>
        /// <param name="request">Request object.</param>
        /// <param name="endPoint">Endpoint object; has list of parms.</param>
        /// <param name="pathRegex">The path regex generated earlier for this request.</param>
        /// <returns>The Match object for ParmWrappers to use.</returns>
        static Match GetPathRegexMatch(IRequest request, IEndPointDescriptor endPoint, 
                                       Regex pathRegex)
        {
            Match match = null;
            if (endPoint.Parameters.Any(p => p is PathAttribute)) {
                if (pathRegex == null) {
                    throw new ApiException(HttpStatusCode.InternalServerError,
                        String.Format("FATAL ERROR handling request for Url {0}" +
                        "no regex was found for extracting the path params",
                            request.Url.AbsolutePath));
                }
                match = pathRegex.Match(request.Url.AbsolutePath);
                if (!match.Success) {
                    throw new ApiException(HttpStatusCode.InternalServerError,
                        String.Format("FATAL ERROR - don't know how to resolve uri {0} " +
                        "with path params", request.Url.AbsolutePath));
                }
            }
            return match;
        }

        /// <summary>
        /// Construct a RestfulResponse from an exception.
        /// </summary>
        /// <param name="ex">Exception to process.</param>
        /// <param name="moduleInstance">Module for the method that had the exception.</param>
        /// <returns></returns>
        static RestfulResponse HandleDispatchException(Exception ex, SidModule moduleInstance)
        {
            var e = ex.InnerException ?? ex;
            HttpStatusCode errorCode;
            if (e is ApiException) {
                errorCode = ((ApiException)e).Code;
            } else if (e is ArgumentException) {
                errorCode = HttpStatusCode.BadRequest;
            } else{
                errorCode = HttpStatusCode.InternalServerError;
            }

            log.Error(e.Message);

            var body = new {
                Message = e.Message,
                StackTrace = e.StackTrace,
            };
            return new RestfulResponse() {
                StatusCode = errorCode,
                Body = JsonConvert.SerializeObject(body),
                Headers = moduleInstance == null ? null : moduleInstance.ResponseHeaders
            };
        }
    }
}

