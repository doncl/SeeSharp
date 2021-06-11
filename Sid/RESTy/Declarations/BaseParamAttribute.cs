using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using RESTy.WebServices;

namespace RESTy.Declarations
{
    /// <summary>
    /// A base class for attributes that will be used to describe how parameters are to be 
    /// extracted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class BaseParamAttribute : DocumentedAttribute
    {
        public virtual ParameterInfo ParameterInfo { get; set; }
        public abstract object GetParamValue(IRequest request, RESTyModule moduleInstance);
    }
}

