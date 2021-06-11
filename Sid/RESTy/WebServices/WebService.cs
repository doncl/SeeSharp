using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Amib.Threading;
using HttpStatusCode = System.Net.HttpStatusCode;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;

namespace RESTy.WebServices
{
    /// <summary>
    /// RESTy.  This is a fairly thin wrapper around the Mono HttpListener class.
    /// </summary>
    public class WebService
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
            );

        readonly HttpListener listener = new HttpListener();

        SmartThreadPool threadPool;

        // This member handles all the requests.
        RestfulMethodDispatcher methodDispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="RESTy.WebServices.WebService"/> class.
        /// </summary>
        /// <param name="restyModuleAssemblies">Names of assemblies to scan for SidModules.</param>
        /// <param name="successHandler">SuccessHandler; this is a delegate that, if set, 
        /// executes as the last thing before the response hits the wire.  It allows users to
        /// set response headers.</param>
        /// <param name="prefixes">The set of prefixes for the HttpListener class that RESTy 
        /// wraps.</param>
        public WebService(IEnumerable<Assembly> restyModuleAssemblies, 
            System.Func<IRequest, Uri, bool> IsValidOriginHandler, params string[] prefixes)
        {
            SetupJsonDefaults();

            if (!HttpListener.IsSupported) {
                throw new NotSupportedException("Not supported on this");
            }

            threadPool = new SmartThreadPool();

            if (prefixes == null || prefixes.Length == 0) {
                throw new ArgumentException("prefixes");
            }

            foreach (var s in prefixes) {
                listener.Prefixes.Add(s);
            }

            methodDispatcher = new RestfulMethodDispatcher(restyModuleAssemblies) {
                IsValidOrigin = IsValidOriginHandler,
            };

            listener.Start();
        }

        /// <summary>
        /// Starts the WebService.   Uses the SmartThreadPool to queue requests.
        /// </summary>
        public void Run()
        {
            threadPool.QueueWorkItem(() => {
                log.Info("WebService running...");
                    try {
                        while (listener.IsListening) {
                            threadPool.QueueWorkItem(ctx => {
                                try {
                                    HandleRequest(ctx);
                                }
                                catch (Exception ex){
                                log.ErrorFormat("Exception in WebService {0}, stack" +
                                    "{1}", ex.Message, ex.StackTrace);
                                    HandleDispatchException(ex, ctx);
                                } 
                                finally {
                                    // always close the stream
                                    ctx.Response.OutputStream.Close();
                                    ctx.Response.Close();
                                }
                            }, listener.GetContext());
                        }
                    }
                    catch (Exception ex) { 
                    log.ErrorFormat("Exception in WebService {0}, stack {1}", 
                            ex.Message, ex.StackTrace);
                    } // suppress any exceptions
                });
        }

        /// <summary>
        /// Sets JSON defaults.
        /// </summary>
        static void SetupJsonDefaults()
        {
            var settings = new JsonSerializerSettings { 
                ContractResolver = new CamelCaseExceptForDictioanries(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            JsonConvert.DefaultSettings = () => settings;
        }

        /// <summary>
        /// Handles the request by first converting it to one of our Request objects.
        /// Then calls the MethodDispatcher to perform the request.  
        /// </summary>
        /// <param name="ctx">HttpListenerContext object, with request.</param>
        private void HandleRequest(HttpListenerContext ctx)
        {
            var request = Request.FromHttpListenerRequest(ctx.Request);
            var response = methodDispatcher.ResolveRequest(request);
            if (ctx.Response.Headers == null) {
                ctx.Response.Headers = new WebHeaderCollection();
            }
            WriteToOutput(ctx, response);
        }

        /// <summary>
        /// Manages the actual write to the HttpListenerContext Response.
        /// </summary>
        /// <param name="ctx">HttpListenerContext object.</param>
        /// <param name="response">Response to write to the output.</param>
        private static void WriteToOutput(HttpListenerContext ctx, RestfulResponse response)
        {
            if (response != null) {
                ctx.Response.StatusCode = (int)response.StatusCode;
                // Propagate the headers back in the response.
                if (response.Headers != null) {
                    foreach (var header in response.Headers) {
                        ctx.Response.Headers.Add(header.Key, header.Value);
                    }
                }
                var responseString = response.Body;
                if (!string.IsNullOrEmpty(responseString)) {
                    var buf = Encoding.UTF8.GetBytes(responseString);
                    ctx.Response.ContentLength64 = buf.Length;
                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                }
            }
        }

        /// <summary>
        /// This shouldn't get hit; RestfulMethodDispatcher.ResolveResponse is supposed to 
        /// catch errors, but if something slips through, this is a stopgap.
        /// This is equivalent to the Nancy 'Oh Noes!....page'.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="ctx"></param>
        private static void HandleDispatchException(Exception ex, HttpListenerContext ctx)
        {
            var errMsg = string.Format("RestfulMethodDispatcher incorrectly threw an untrapped"
                + " error = {0}, request = {1} stack = {2}", 
                ex.Message, ctx.Request.RawUrl, ex.StackTrace);

            log.Error(errMsg);

            var error = new RestfulResponse{
                StatusCode = HttpStatusCode.InternalServerError,
                Body = errMsg,
            };
            WriteToOutput(ctx, error);
        }

        /// <summary>
        /// Shut it all down.
        /// </summary>
        public void Stop()
        {

            listener.Stop();
            threadPool.Shutdown();
            listener.Close();
        }
    }

    public class CamelCaseExceptForDictioanries : CamelCasePropertyNamesContractResolver
    {
        protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
        {
            var contract = base.CreateDictionaryContract(objectType);
            contract.PropertyNameResolver = x => x;
            return contract;
        }
    }
}

