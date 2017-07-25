using System;
using System.Collections.Generic;
using System.Reflection;
using Sid.WebServices;

namespace Sid.Declarations
{
    /// <summary>
    /// Indicates that a parameter should be populated via extraction from the URI.
    /// </summary>
    public class PathAttribute : BaseParamAttribute
    {
        public override object GetParamValue(IRequest request, SidModule moduleInstance)
                        
        {
            var match = request.Match;  
            if (match == null || !match.Success) {
                throw new Exception(String.Format("FATAL ERROR - Param {0} missing regex " +
                    "match for resolving argument", ParameterInfo.Name));
            }
            var value = match.Groups[ParameterInfo.Name].Value;
            return string.IsNullOrEmpty(value) ? null : ParameterInfo.Convert(value);
        }
    }
}

