using System;

namespace Sid.Declarations
{
    /// <summary>
    /// Public attribute means that Sid will put an Access-Control-Allow-Origin header value of
    /// '*' on the resonse headers for the endpoint.
    /// </summary>
    public class PublicAttribute : Attribute
    {
    }
}

