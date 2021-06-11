using System;
using System.Net.Http;

namespace RESTy.Declarations
{
    /// <summary>
    /// Indicates that the given method should be associated with an HTTP POST request.
    /// </summary>
    public class PostAttribute : BaseRouteAttribute
    {
        public PostAttribute(string path) : base(path) {}

        public override HttpMethod Method { get { return HttpMethod.Post; } }
    }
}
