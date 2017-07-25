using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace Sid.WebServices.Test
{
    /// <summary>
    /// This is more bits of Nancy's testing framework, extracted out.
    /// </summary>
    public interface IBrowserContextValues 
    {
        Stream Body {get; set; }
        string BodyString {get; set; }
        NameValueCollection Headers { get; set; }
        string Protocol { get; set; }
        NameValueCollection QueryString { get; set; }
        CookieCollection Cookies { get; set; }
        string UserHostAddress { get; set; }
    }

    public class BrowserContext : IBrowserContextValues
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserContext"/> class.
        /// </summary>
        public BrowserContext()
        {
            Values.Headers = new NameValueCollection();
            Values.Protocol = "http";
            Values.QueryString = new NameValueCollection();
            Values.Cookies = new CookieCollection();
            Values.BodyString = String.Empty;
            Values.UserHostAddress = "127.0.0.1";
        }

        /// <summary>
        /// Gets or sets the that should be sent with the HTTP request.
        /// </summary>
        /// <value>A <see cref="Stream"/> that contains the body that should be sent with the 
        /// HTTP request.</value>
        Stream IBrowserContextValues.Body { get; set; }

        /// <summary>
        /// Gets or sets the protocol that should be sent with the HTTP request.
        /// </summary>
        /// <value>A <see cref="string"/> contains the the protocol that should be sent with 
        /// the HTTP request.</value>
        string IBrowserContextValues.Protocol { get; set; }

        /// <summary>
        /// Gets or sets the querystring
        /// </summary>
        NameValueCollection IBrowserContextValues.QueryString { get; set; }

        /// <summary>
        /// Gets or sets the user host address
        /// </summary>
        string IBrowserContextValues.UserHostAddress { get; set; }

        /// <summary>
        /// Gets or sets the body string
        /// </summary>
        string IBrowserContextValues.BodyString { get; set; }

        /// <summary>
        /// Gets or sets the headers that should be sent with the HTTP request.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> instance that contains the headers 
        /// that should be sent with the HTTP request.</value>
        NameValueCollection IBrowserContextValues.Headers { get; set; }

        /// <summary>
        /// Gets or sets the cookies.
        /// </summary>
        /// <value>The cookies.</value>
        CookieCollection IBrowserContextValues.Cookies {get; set;}

        /// <summary>
        /// Adds a body to the HTTP request.
        /// </summary>
        /// <param name="body">A string that should be used as the HTTP request body.</param>
        public void Body(string body)
        {
            Values.BodyString = body;
        }

        /// <summary>
        /// Adds a application/json request body to the <see cref="Browser"/>.
        /// </summary>
        /// <param name="model">The model to be serialized to json.</param>
        public void JsonBody<TModel>(TModel model)
        {
            var contextValues =
                (IBrowserContextValues)this;

            contextValues.Body = new MemoryStream();
            #if true
            var serializer = new JsonSerializer();
            var streamWriter = new StreamWriter(contextValues.Body);
            var jsonTextWriter = new JsonTextWriter(streamWriter);
            serializer.Serialize(jsonTextWriter, model);
            streamWriter.Flush();
            #else
            JsonSerializer.SerializeToStream(model, contextValues.Body);
            #endif
            contextValues.Body.Position = 0;
            Header("Content-Type", "application/json");
        }

        /// <summary>
        /// Adds a body to the HTTP request.
        /// </summary>
        /// <param name="body">A stream that should be used as the HTTP request body.</param>
        /// <param name="contentType">Content type of the HTTP request body. Defaults to 
        /// 'application/octet-stream'</param>
        public void Body(Stream body, string contentType = null)
        {
            Values.Body = body;
            Header("Content-Type", contentType ?? "application/octet-stream");
        }

        /// <summary>
        /// Adds a header to the HTTP request.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        public void Header(string name, string value)
        {
            Values.Headers.Add(name, value);
        }

        /// <summary>
        /// Configures the request to be sent over HTTP.
        /// </summary>
        public void HttpRequest()
        {
            this.Values.Protocol = "http";
        }

        /// <summary>
        /// Configures the request to be sent over HTTPS.
        /// </summary>
        public void HttpsRequest()
        {
            this.Values.Protocol = "https";
        }

        /// <summary>
        /// Adds a query string entry
        /// </summary>
        public void Query(string key, string value)
        {
            Values.QueryString.Add(key, value);
        }

        /// <summary>
        /// Sets the user host address.
        /// </summary>
        public void UserHostAddress(string userHostAddress)
        {
            Values.UserHostAddress = userHostAddress;
        }

        private IBrowserContextValues Values
        {
            get { return this; }
        }
    }
}

