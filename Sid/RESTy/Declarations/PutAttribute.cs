using System;
using System.Net.Http;

namespace RESTy.Declarations
{
    /// <summary>
    /// Indicates that the given method should be associated with an HTTP PUT request.
    /// </summary>
    public class PutAttribute : BaseRouteAttribute
    {
        public PutAttribute(string path) : base(path) {}

        public override HttpMethod Method { get { return HttpMethod.Put; } }
    }
}

