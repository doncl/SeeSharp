using System;
using System.Collections.Generic;
using System.Linq;
using Sid.Declarations;

namespace Sid.WebServices
{
    /// <summary>
    /// The base class for classes that implement Restful services for the Scout Api.
    /// </summary>
    public abstract class SidModule
    {
        /// <summary>
        /// The log4net log for this class. See mordred/Scout.Mordred.Server/Program.cs
        /// for more info.
        /// </summary>
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
            );


        public WebServices.IRequest RestfulRequest { get; set; }

        /// <summary>
        /// A list of headers to be sent along with the request.
        /// </summary>
        readonly Dictionary<string, string> responseHeaders = new Dictionary<string, string>();

        public Dictionary<string, string> ResponseHeaders {
            get {
                return responseHeaders;
            }
        }

        protected SidModule() 
        {
            Type moduleType = GetType();
            String rootPath = DeclarationHelper.GetRootPath(moduleType).Path;
            if (rootPath.StartsWith("/") || rootPath.EndsWith("/")) {
                throw new InvalidOperationException(
                    "Root path " + rootPath + " on module " + moduleType +
                    " may not begin or end with a slash"
                );
            }
        }

        /// <summary>
        /// Adds a cookie to the response.
        /// </summary>
        protected void AddResponseHeader(string key, string value)
        {
            if (String.IsNullOrWhiteSpace(key)) {
                log.Warn("Attempt to insert header with null / empty key");
                return;
            }
            if (value == null) {
                if (responseHeaders.ContainsKey(key)) {
                    responseHeaders.Remove(key);
                }
            } else {
                responseHeaders[key] = value;
            }
        }

        /// <summary>
        /// Parse comma delimited list of strings.
        /// </summary>
        /// <returns>The list.</returns>
        /// <param name="list">List.</param>
        protected static IList<string> GetStringListFromString(string list)
        {
            return string.IsNullOrEmpty(list) ? null : list.Split(',');
        }


        /// <summary>
        /// Convert list of string integers into array of integers.
        /// </summary>
        /// <returns>The array.</returns>
        /// <param name="list">List.</param>
        protected static IList<int> GetIntArrayFromString(string list)
        {
            var strings = GetStringListFromString(list);
            return strings != null ? strings.Select(int.Parse).ToList() : null;
        }
    }
}
