using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Sid.WebServices
{
    /// <summary>
    /// This is an abstraction for the HttpListenerRequest; so we can have an IntegrationTest
    /// request object also.   The Headers and QueryString are changed a bit from the 
    /// HttpListener to make them easier to deal with.
    /// </summary>
    public interface IRequest
    {
        Match Match { get; set; }

        string SiteBase { get; }

        Uri Url { get; set; }

        HttpMethod HttpMethod { get; set; }

        Stream InputStream { get; set; }

        long ContentLength { get; set; }

        RestValueCollection Headers { get; set; }

        CookieCollection Cookies {get; set;}

        RestValueCollection QueryString { get; set; }
    }

    /// <summary>
    /// A wrapper around the Dictionary object that returns null when a key is not found,
    /// rather than throwing an exception. 
    /// </summary>
    public class RestValueCollection : Dictionary<string, List<string>>
    {
        public RestValueCollection() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public new IEnumerable<string> this [string name] {
            get {
                return (ContainsKey(name)) ? base[name] : Enumerable.Empty<string>();
            }

            set {
                base[name] = value.ToList();
            }
        }

        /// <summary>
        /// Gets the first header with a given key value, or null.
        /// </summary>
        public string FirstHeaderValue(string key)
        {
            return this[key].FirstOrDefault();
        }

        /// <summary>
        /// Returns all the headers.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>>  GetAllHeaders()
        {
            return this.Keys.ToDictionary(k => k, k => string.Join(":", this[k]));
        }
    }

    /// <summary>
    /// The real implementation of IRequest; this is the thing that the RestulMethodDispatcher
    /// knows how to handle.
    /// </summary>
    public class Request : IRequest
    {
        public static Dictionary<string, HttpMethod> SupportedHttpMethods {get; private set;}

        public string SiteBase {
            get {
                // BUGBUG:  Do we want to hide port 80, i.e. make it implicit?
                return Url.Scheme + "://" + GetHostName(Url.Host) + ":" + Url.Port;
            }
        }

        public Match Match { get; set; }

        public Uri Url { get; set; }

        public HttpMethod HttpMethod { get; set; }

        public Stream InputStream { get; set; }

        public long ContentLength { get; set; }

        public RestValueCollection Headers { get; set; }

        public CookieCollection Cookies {get; set;}

        public RestValueCollection QueryString { get; set; }

        static Request()
        {
            SupportedHttpMethods = 
                new Dictionary<string, HttpMethod>(StringComparer.OrdinalIgnoreCase) {
                { "GET", HttpMethod.Get },
                { "PUT", HttpMethod.Put },
                { "POST", HttpMethod.Post },
                { "OPTIONS", HttpMethod.Options },
                { "DELETE", HttpMethod.Delete },
            };
        }

        /// <summary>
        /// Transforms an HttpListenerRequest to a Request.  
        /// </summary>
        /// <returns>The http listener request.</returns>
        /// <param name="listenerRequest">Listener request.</param>
        public static Request FromHttpListenerRequest(HttpListenerRequest listenerRequest)
        {
            var queryString = listenerRequest.QueryString
                                .ToRestValueCollectionNoSplitOnCommas();
            var headers = listenerRequest.Headers.ToRestValueCollectionNoSplitOnCommas();

            HttpMethod httpMethod;

            if (!SupportedHttpMethods.TryGetValue(listenerRequest.HttpMethod, out httpMethod)){
                throw new Exception(
                    String.Format("Error - do not know how to handle HttpMethod \"{0}\", " +
                    "supported methods are {1}",
                        listenerRequest.HttpMethod,
                        String.Join(",", SupportedHttpMethods.Keys)));
            }

            var newUrl = StripRedundantUriSeparators(listenerRequest);
            return new Request {
                Url = newUrl,
                HttpMethod = httpMethod,
                Headers = headers,
                Cookies = listenerRequest.Cookies,
                QueryString = queryString,
                InputStream = listenerRequest.InputStream,
                ContentLength = listenerRequest.ContentLength64
            };
        }

        /// <summary>
        /// In some situations, the front end sends in Uri's with multiple slashes in them, 
        /// e.g. 'http://api.scout.com//content/sites/15'.  We need to be tolerant of that.
        /// </summary>
        /// <returns>The redundant URI separators.</returns>
        /// <param name="listenerRequest">Listener request.</param>
        static Uri StripRedundantUriSeparators(HttpListenerRequest listenerRequest)
        {
            var inUrl = listenerRequest.Url;
            var absolutePath = listenerRequest.Url.AbsolutePath.Replace("//", "/");
            var urlString = inUrl.ToString();
            var newUrlString = urlString.Replace(inUrl.AbsolutePath, absolutePath);
            var newUrl = new Uri(newUrlString);
            return newUrl;
        }

        /// <summary>
        /// Helper to return the hostname.
        /// </summary>
        /// <returns>The host name.</returns>
        /// <param name="hostName">Host name.</param>
        private static string GetHostName(string hostName)
        {
            IPAddress address;

            if (IPAddress.TryParse(hostName, out address)) {
                return (address.AddressFamily == AddressFamily.InterNetworkV6)
                           ? string.Concat("[", address.ToString(), "]")
                           : address.ToString();
            }

            return hostName;
        }
    }

    /// <summary>
    /// A bit of glue code to map old C# 1.0(ish) NameValueCollection object (which is what the
    /// HttpListenerRequest object has for headers and querystrings) to a more modern 
    /// Dictionary construct, which is much easier to extract values from.
    /// </summary>
    public static class NameValueCollectionExtensions
    {
        public static RestValueCollection ToRestValueCollectionSplitOnCommas(
            this NameValueCollection collection)
        {
            var restValueCollection = new RestValueCollection();
            foreach (var keyObject in collection) {
                var key = keyObject as string;
                if (key == null) {
                    continue;
                }
                var commaSeparatedValueList = collection[key];
                var values = commaSeparatedValueList.Split(',')
                    .Where(value => value != null).ToList();
                restValueCollection[key] = values;
            }
            return restValueCollection;
        }

        public static RestValueCollection ToRestValueCollectionNoSplitOnCommas(
            this NameValueCollection collection)
        {
            var restValueCollection = new RestValueCollection();
            foreach (var keyObject in collection) {
                var key = keyObject as string;
                if (key == null) {
                    continue;
                }
                var collectionValues = collection.GetValues(key);
                if (collectionValues == null) {
                    continue;
                }
                var values = collectionValues.ToList();
                restValueCollection[key] = values;
            }
            return restValueCollection;
        }

    }
}
