using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Sid.WebServices;

namespace Sid.Declarations
{
    /// <summary>
    /// Indicates that a parameter should be populated by deserializing the stream in the
    /// ParmWrapper that will be supplied at Bind time. 
    /// method.
    /// </summary>
    public class BindAttribute : BaseParamAttribute
    {
        static JsonSerializer serializer;

        public override object GetParamValue(IRequest request, SidModule moduleInstance){
            if (request.InputStream == Stream.Null) {
                // If there is no request body to deserialize, create a default instance of the
                // type of object specified, and inject it with property values that were
                // specified on the querystring.
                var o = Activator.CreateInstance(ParameterInfo.ParameterType);
                foreach (var kvp in request.QueryString) {
                    var key = kvp.Key[0].ToString().ToUpper() + kvp.Key.Substring(1);
                    var attribute = ParameterInfo.ParameterType.GetProperty(key);
                    if (attribute != null) {
                        var type = Nullable.GetUnderlyingType(attribute.PropertyType) ??
                            attribute.PropertyType;
                        var strVal = kvp.Value.FirstOrDefault();
                        if (string.IsNullOrEmpty(strVal)) {
                            continue;
                        }
                        object val;
                        if (strVal.StartsWith("{") || strVal.StartsWith("[") || strVal.StartsWith("\"")) {
                            val = JsonConvert.DeserializeObject(strVal, type);
                        } else if (type.IsEnum) {
                            val = Enum.Parse(type, strVal, true);
                        } else {
                            val = Convert.ChangeType(strVal, type);
                        }
                        attribute.SetValue(o, val);
                    }
                }
                return o;
            }
            using (var reader = new StreamReader(request.InputStream)) {
                var type = ParameterInfo.ParameterType;
                if (serializer == null) {
                    serializer = JsonSerializer.CreateDefault();
                }
                using(StreamReader streamReader = new StreamReader(request.InputStream)) {
                    var o = serializer.Deserialize(streamReader, type);
                    return o;
                }

            }
        }
    }
}

