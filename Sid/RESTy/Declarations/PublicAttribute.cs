using System;

namespace RESTy.Declarations
{
    /// <summary>
    /// Public attribute means that RESTy will put an Access-Control-Allow-Origin header value of
    /// '*' on the resonse headers for the endpoint.
    /// </summary>
    public class PublicAttribute : Attribute
    {
    }
}

