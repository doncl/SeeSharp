using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Net;
using RESTy.WebServices;

namespace RESTy.Declarations
{
    /// <summary>
    /// Indicates that a parameter should be populated via extraction from the cookie
    /// via Name Lookup.
    /// Parameter name binding is not supported.
    /// </summary>
    public class CookieAttribute : BaseParamAttribute
    {
        readonly string cookieName;

        /// <summary>
        /// Get cookie by case-sensitive name.  
        /// </summary>
        /// <param name="cookieName">Cookie name.</param>
        public CookieAttribute(string cookieName)
        {
            this.cookieName = cookieName;
        }

        public override object GetParamValue(IRequest request, RESTyModule moduleInstance)
        {
            var cookie = request.Cookies[cookieName];
            if (cookie == null) {
                return null;
            }
            return cookie.Value;
        }
    }
}

