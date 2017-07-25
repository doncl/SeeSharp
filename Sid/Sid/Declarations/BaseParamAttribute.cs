using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Sid.WebServices;

namespace Sid.Declarations
{
    /// <summary>
    /// A base class for attributes that will be used to describe how parameters are to be 
    /// extracted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class BaseParamAttribute : DocumentedAttribute
    {
        public virtual ParameterInfo ParameterInfo { get; set; }
        public abstract object GetParamValue(IRequest request, SidModule moduleInstance);
    }
}

