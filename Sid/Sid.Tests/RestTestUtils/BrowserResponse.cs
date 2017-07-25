using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Sid.WebServices;

namespace Sid.WebServices.Test
{
    /// <summary>
    /// A minimal BrowserResponse object for use in IntegrationTests.
    /// </summary>
    public class BrowserResponse
    {
        public HttpStatusCode StatusCode {get; set;}

        // As JSON.
        public string Body {get; set;}
        public NameValueCollection Headers {get; set;}
    }

    /// <summary>
    /// Minimal Server Response.
    /// </summary>
    public class RestfulTestServerResponse
    {
        public HttpStatusCode Code {get; set;}
        public string Body {get; set;}
    }

    /// <summary>
    /// Restful test server request for use by IntegrationTests.
    /// </summary>
    public class RestfulTestServerRequest : IRequest
    {
        // This is only used by the SwaggerModule.
        public string SiteBase {
            get {
                throw new NotImplementedException();
            }
        }
        public Uri Url {get; set;}
        public Match Match {get; set;}
        public HttpMethod HttpMethod {get; set;}
        public string Query {get; set;}    
        public string RawUrl {get; set;}
        public Stream InputStream {get; set;}
        public long ContentLength { get; set; }
        public RestValueCollection  Headers {get; set;}
        public RestValueCollection QueryString {get; set;}
        public CookieCollection Cookies {get; set; }
    }
}

