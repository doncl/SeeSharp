using System;
using System.Net.Http;

namespace RESTy.Declarations
{
    /// <summary>
    /// Indicates that the given method should be associated with an HTTP GET request.
    /// </summary>
    public class GetAttribute : BaseRouteAttribute
    {
        public GetAttribute(string path) : base(path) {}

        public override HttpMethod Method { get { return HttpMethod.Get; } }
    }
}
