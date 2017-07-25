using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Text;
using Sid.WebServices;
using System.Reflection;

namespace Sid.WebServices.Test
{
    /// <summary>
    /// This is code cobbled together from the nancy test framework for use in our integration
    /// tests; as little of this code is used as possible.
    /// </summary>
    public class Browser
    {
        public RestfulMethodDispatcher MethodDispatcher {get; set;}

        public Browser(IEnumerable<Assembly> sidModuleAssemblies)
        {
            MethodDispatcher = new RestfulMethodDispatcher(sidModuleAssemblies) {
                IsValidOrigin = (req, uri) => true,
            };
        }

        /// <summary>
        /// Performs a DELETE requests.
        /// </summary>
        /// <param name="path">The path that is being requested.</param>
        /// <param name="browserContext">An closure for providing browser context for the 
        /// request.</param>
        /// <returns>An <see cref="BrowserResponse"/> instance of the executed request.
        /// </returns>
        public BrowserResponse Delete(string path, 
                                      Action<BrowserContext> browserContext = null)
        {
            return HandleRequest(HttpMethod.Delete, path, browserContext);
        }

        /// <summary>
        /// Performs a GET request.
        /// </summary>
        /// <param name="path">The path that is being requested.</param>
        /// <param name="browserContext">An closure for providing browser context for the 
        /// request.</param>
        /// <returns>An <see cref="BrowserResponse"/> instance of the executed request.
        /// </returns>
        public BrowserResponse Get(string path, Action<BrowserContext> browserContext = null)
        {
            return HandleRequest(HttpMethod.Get, path, browserContext);
        }

        /// <summary>
        /// Performs an OPTIONS request.
        /// </summary>
        /// <param name="path">The path that is being requested.</param>
        /// <param name="browserContext">An closure for providing browser context for the 
        /// request.</param>
        /// <returns>An <see cref="BrowserResponse"/> instance of the executed request.
        /// </returns>
        public BrowserResponse Options(string path, 
            Action<BrowserContext> browserContext = null)
        {
            return HandleRequest(HttpMethod.Options, path, browserContext);
        }

        /// <summary>
        /// Performs a POST request.
        /// </summary>
        /// <param name="path">The path that is being requested.</param>
        /// <param name="browserContext">An closure for providing browser context for the 
        /// request.</param>
        /// <returns>An <see cref="BrowserResponse"/> instance of the executed request.
        /// </returns>
        public BrowserResponse Post(string path, Action<BrowserContext> browserContext = null)
        {
            return HandleRequest(HttpMethod.Post, path, browserContext);
        }

        /// <summary>
        /// Performs a PUT request.
        /// </summary>
        /// <param name="path">The path that is being requested.</param>
        /// <param name="browserContext">An closure for providing browser context for the 
        /// request.</param>
        /// <returns>An <see cref="BrowserResponse"/>Instance of the executed request.
        /// </returns>
        public BrowserResponse Put(string path, Action<BrowserContext> browserContext = null)
        {
            return HandleRequest(HttpMethod.Put, path, browserContext);
        }

        private BrowserResponse HandleRequest(HttpMethod method, string path, 
                                            Action<BrowserContext> browserContext)
        {
            var request =
                CreateRequest(method, path, browserContext ?? DefaultBrowserContext);

            var restfulResponse = MethodDispatcher.ResolveRequest(request);
            var response = new BrowserResponse{
              StatusCode = restfulResponse.StatusCode,
              Body = restfulResponse.Body,
              Headers = new NameValueCollection(),
            };

            if (restfulResponse.Headers != null) {
                foreach (var header in restfulResponse.Headers) {
                    response.Headers.Add(header.Key, header.Value);
                }
            }

            return response;
        }

        private void DefaultBrowserContext(BrowserContext context)
        {
            context.HttpRequest();
        }

        private static void BuildRequestBody(IBrowserContextValues contextValues)
        {
            if (contextValues.Body != null) {
                return;
            }

            if (string.IsNullOrEmpty(contextValues.BodyString)) {
                contextValues.Body = Stream.Null;
            } else {
                var bodyContents = contextValues.BodyString;
                var bodyBytes = bodyContents != null ? Encoding.UTF8.GetBytes(bodyContents) : 
                    new byte[] { };

                contextValues.Body = new MemoryStream(bodyBytes);
            }
        }

        private RestfulTestServerRequest CreateRequest(HttpMethod method, string path, 
                Action<BrowserContext> browserContext)
        {
            var context =
                new BrowserContext();

            browserContext.Invoke(context);

            var contextValues =
                (IBrowserContextValues)context;

            if (String.IsNullOrEmpty(contextValues.UserHostAddress)) {
                throw new Exception("Need a valid host address");
            }

            path = path.TrimStart('/');
            var uriString = contextValues.Protocol + "://" + contextValues.UserHostAddress +
                "/" + path;

            Uri uri;

            if (!Uri.TryCreate(uriString, UriKind.Absolute,  out uri)) {
                throw new Exception(String.Format("Cannot create uri from {0}", uri));
            }

            BuildRequestBody(contextValues);

            var queryString = contextValues.QueryString.ToRestValueCollectionNoSplitOnCommas();
            var headers = contextValues.Headers.ToRestValueCollectionSplitOnCommas();
            headers["Origin"] = new [] {"http://" + path.TrimStart('/')};

            return new RestfulTestServerRequest{
                Url = uri,
                Cookies = contextValues.Cookies,
                HttpMethod = method,
                InputStream = contextValues.Body,
                RawUrl= path,
                Headers = headers,
                QueryString = queryString,
            };
        }
    }
}

