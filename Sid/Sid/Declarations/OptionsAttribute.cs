using System;
using System.Net.Http;

namespace Sid.Declarations
{
    public class OptionsAttribute : BaseRouteAttribute
    {
        /// <summary>
        /// Indicates that the given method should be associated with an HTTP OPTIONS request.
        /// </summary>
        public OptionsAttribute(string path) : base(path) {}

        public override HttpMethod Method { get { return HttpMethod.Options; } }
    }
}
