using System;
using System.Net.Http;

namespace Sid.Declarations
{
    /// <summary>
    /// Indicates that the given method should be associated with an HTTP DELETE request.
    /// </summary>
    public class DeleteAttribute : BaseRouteAttribute
    {
        public DeleteAttribute(string path) : base(path) {}

        public override HttpMethod Method { get { return HttpMethod.Delete; } }
    }
}
