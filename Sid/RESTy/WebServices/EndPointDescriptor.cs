using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Net.Http;
using RESTy.Declarations;

namespace RESTy.WebServices
{
    /// <summary>
    /// Base interface to make it easier to unit test things.
    /// </summary>
    public interface IEndPointDescriptor
    {
        Type ModuleType { get; set; }  
        string Path { get; set; }      
        HttpMethod WebMethod { get; set; } 
        MethodInfo MethodInfo { get; set; }
        BaseRouteAttribute BaseRouteAttribute {get; set;}
        List<BaseParamAttribute> Parameters { get; set; }
        string Name {get; set; }
        bool Public {get; set;}
    }

    /// <summary>
    /// End point descriptor.  This has all the information necessary to execute a method.
    /// </summary>
    public class EndPointDescriptor : IEndPointDescriptor
    {
        public Type ModuleType { get; set; }  // A pointer to the Module this is for.
        public string Path { get; set; }      // Full path to the method, including module.
        public HttpMethod WebMethod { get; set; }  // The HTTP Verb.
        public MethodInfo MethodInfo { get; set; }
        public BaseRouteAttribute BaseRouteAttribute {get; set;}
        public List<BaseParamAttribute> Parameters { get; set; }
        public string Name {get; set; }
        public bool Public { get; set; }

        public EndPointDescriptor(Type moduleType, MethodInfo methodInfo, string rootPath) 
        {
            MethodInfo = methodInfo;
            ModuleType = moduleType;
            Name = methodInfo.Name;

            var attrs = MethodInfo.GetCustomAttributes(true);

            Public = attrs.Any(attr => attr is PublicAttribute);

            BaseRouteAttribute = 
               attrs.SingleOrDefault(attr => attr is BaseRouteAttribute) as BaseRouteAttribute;

            if (BaseRouteAttribute == null) {
                throw new Exception(String.Format("FATAL ERROR - Module {0}, Method {1} " +
                    "is lacking any sort of BaseRouteAttribute declaration",
                    moduleType.Name, Name));
            }

            Path = String.Join("/", rootPath, BaseRouteAttribute.Path).Trim('/');
            WebMethod = BaseRouteAttribute.Method;

            Parameters = new List<BaseParamAttribute>();
            var parmInfos = DeclarationHelper.GetParameters(MethodInfo);
            foreach (var parmInfo in parmInfos) {

                var baseParamAttribute = parmInfo.GetCustomAttributes(true)
                    .SingleOrDefault(attr => attr is BaseParamAttribute) as BaseParamAttribute;

                if (baseParamAttribute == null) {
                    throw new Exception(String.Format("FATAL ERROR - Module {0}, Method {1} " +
                        "Parameter {2} is lacking any sort of BaseParamAttribute declaration",
                        ModuleType.Name, Name, parmInfo.Name));
                }

                string reason;
                if (!BaseRouteAttribute.IsAllowed(baseParamAttribute, out reason)) {
                    throw new Exception(String.Format("FATAL ERROR - Module {0}, Method {1} " +
                        "Parameter {2}, {3} attribute not allowable on {4} route because '{5}'.",
                        ModuleType.Name, Name, parmInfo.Name,
                        baseParamAttribute, BaseRouteAttribute, reason
                    ));
                }

                baseParamAttribute.ParameterInfo = parmInfo;
                Parameters.Add(baseParamAttribute);
            }
        }
    }
}
