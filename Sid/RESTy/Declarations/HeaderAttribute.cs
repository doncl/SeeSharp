using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Net;
using RESTy.WebServices;

namespace RESTy.Declarations
{
    /// <summary>
    /// Indicates that a parameter should be populated via extraction from the request headers
    /// via Name Lookup.
    /// Parameter name binding is not supported.
    /// </summary>
    public class HeaderAttribute : BaseParamAttribute
    {
        readonly string headerName;

        /// <summary>
        /// Get header by case-sensitive name.  The decorated parameter must be an
        /// IEnumerable&lt;string&gt;.
        /// </summary>
        /// <param name="headerName">Header name.</param>
        public HeaderAttribute(string headerName)
        {
            this.headerName = headerName;
        }

        public override object GetParamValue(IRequest request, RESTyModule moduleInstance) {
            if (request.Headers.ContainsKey(headerName)) {
                return request.Headers[headerName].FirstOrDefault();
            }
            return null;
        }
    }
}

