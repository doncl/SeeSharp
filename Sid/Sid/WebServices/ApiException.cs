using System;
using System.Net;

namespace Sid.WebServices
{
    public class ApiException : Exception
    {
        public HttpStatusCode Code { get; private set; }

        public ApiException(HttpStatusCode code) : this(code, string.Empty) {}

        public ApiException(HttpStatusCode code, String message) : base(message)
        {
            Code = code;
        }
    }
}

