using System;
using System.Collections.Generic;
using System.Net;

namespace RESTy.WebServices
{
    /// <summary>
    /// This the response object that the RestfulMethodDispatcher class generates as its end 
    /// product.
    /// </summary>
    public class RestfulResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}
