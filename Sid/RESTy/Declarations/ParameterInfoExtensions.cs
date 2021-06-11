using System;
using System.Collections.Generic;
using System.Reflection;


namespace RESTy.Declarations
{
    static public class ParameterInfoExtensions
    {
        static readonly IDictionary<Type, Func<string, object>> coercers = 
            new Dictionary<Type, Func<string, object>>{
            {typeof(bool), HandleBool},
            {typeof(DateTime), HandleDate},
        };

        /// <summary>
        /// Converts types, special-casing boolean behavior so we can treat 1's and 0's as
        /// bools.
        /// </summary>
        /// <returns>The converted value</returns>
        /// <param name="parameterInfo">The parameter info to convert.</param>
        /// <param name="value">The input value.</param>
        static public object Convert(this ParameterInfo parameterInfo, object value)
        {
            var parameterType = parameterInfo.ParameterType;
            var underlyingType = Nullable.GetUnderlyingType(parameterType);
            if (null != underlyingType) {
                parameterType = underlyingType;
            }

            if (value is string && coercers.ContainsKey(parameterType)) {
                var s = value as string;
                return coercers[parameterType](s);
            }
            return System.Convert.ChangeType(value, parameterInfo.ParameterType);
        }

        /// <summary>
        /// Coerces string value to bool, as appropriate.
        /// </summary>
        /// <returns>true or false as appropriate.</returns>
        /// <param name="value">Boolean in string form.</param>
        static object HandleBool(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            value = value.Trim();
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                value.Equals("yes", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            int i;
            if (!int.TryParse(value, out i)) {
                return false;
            }
            return (i != 0);
        }

        /// <summary>
        /// Coerces string value to datetime, as appropriate.
        /// </summary>
        /// <returns>The parsed date.</returns>
        /// <param name="value">The input value.</param>
        static object HandleDate(string value)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(value, out dateTime)) {
                return null;
            }
            return dateTime;
        }
    }
}

