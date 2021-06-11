using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using RESTy.WebServices;
using IRequest = RESTy.WebServices.IRequest;

namespace RESTy.Declarations
{
    /// <summary>
    /// Indicates that a parameter should be populated via extraction from the query string.
    /// </summary>
    public class QueryAttribute : BaseParamAttribute
    {
        public bool IsDefaultSet { get; private set; }

        private object defaultValue;

        public override ParameterInfo ParameterInfo {
            get {
                return base.ParameterInfo;
            }
            set {
                base.ParameterInfo = value;
                if (Default != null && !Default.GetType()
                    .IsAssignableFrom(value.ParameterType)) {
                    throw new Exception(String.Format("FATAL ERROR - Module {0}, Method " +
                        "{1}, Parameter {2}: Default member type {3} does not match parameter " 
                        + "type {4}", 
                        value.Member.DeclaringType.Name, value.Member.Name, value.Name, 
                        Default.GetType(), value.ParameterType));
                }
            }
        }

        public object Default {
            get { return defaultValue; }
            set {
                defaultValue = value;
                IsDefaultSet = true;
            }
        }

        public override object GetParamValue(IRequest request, RESTyModule moduleInstance)
        {
            var queryStringValue = request.QueryString[ParameterInfo.Name]
                        .FirstOrDefault();

            if (string.IsNullOrEmpty(queryStringValue)) {
                return Default;
            }
            var paramType = ParameterInfo.ParameterType;

            if (paramType.IsPrimitive || paramType.Equals(typeof(string))) {
                return ParameterInfo.Convert(queryStringValue);
            }
            if (paramType.IsEnum) {
                return Enum.Parse(paramType, queryStringValue, true);
            }
            if (queryStringValue.StartsWith("{") || queryStringValue.StartsWith("[") ||
                queryStringValue.StartsWith("\"")) {
                return JsonConvert.DeserializeObject(queryStringValue, paramType);
            }
            return ParameterInfo.Convert(queryStringValue);
       }
    }
}
