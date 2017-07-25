using System;
using System.Net.Http;

namespace Sid.Declarations
{
    /// <summary>
    /// Attribute used to indicate that a method is a Sid endpoint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class BaseRouteAttribute : DocumentedAttribute
    {
        public readonly string Path;

        protected BaseRouteAttribute(string path)
        {
            this.Path = path;
        }

        public virtual bool IsAllowed(BaseParamAttribute baseParamAttribute, out string reason)
        {
            reason = null;
            return true;
        }

        /// <summary>
        /// The HttpMethod associated with this route.
        /// </summary>
        public abstract HttpMethod Method { get; }
    }
}
