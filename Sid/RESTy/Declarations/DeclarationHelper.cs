using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using RESTy.WebServices;

namespace RESTy.Declarations
{
    /// <summary>
    /// A collection of helper methods for dealing with declarative routes.
    /// </summary>
    public static class DeclarationHelper
    {
        // These dictionaries cache various pieces of various reflection lookups that we're
        // doing as part of the operation of this class.  We don't have to worry about things
        // like size limiting or expiration, since the amount of data in any one of these
        // caches is never very large, and the values in the caches can't change except with a
        // code change.

        private readonly static Dictionary<Type, RootPathAttribute> RootPathCache =
            new Dictionary<Type, RootPathAttribute>();

        private readonly static Dictionary<Type, IEnumerable<MethodInfo>> MethodCache =
            new Dictionary<Type, IEnumerable<MethodInfo>>();

        private readonly static Dictionary<MethodInfo, IEnumerable<ParameterInfo>> ParameterCache =
            new Dictionary<MethodInfo, IEnumerable<ParameterInfo>>();

        /// <summary>
        /// Gets a list of all currently loaded BaseModule types.
        /// </summary>
        public static IEnumerable<Type> GetModuleTypes(IEnumerable<Assembly> RESTyModuleAssemblies)
        {
            return RESTyModuleAssemblies.SelectMany(asm1 => asm1.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(RESTyModule)) && !t.IsAbstract);
        }

        /// <summary>
        /// Given a suitably-decorated BaseModule module type, returns the RootPathAttribute
        /// with which it is decorated.  Performs caching.
        /// </summary>
        public static RootPathAttribute GetRootPath(Type moduleType)
        {
            if (!RootPathCache.ContainsKey(moduleType)) {
                RootPathCache[moduleType] = moduleType
                    .GetCustomAttributes(true)
                    .Single(attr => attr is RootPathAttribute) as RootPathAttribute;
            }
            return RootPathCache[moduleType];
        }

        /// <summary>
        /// Given a suitably-decorated BaseModule module type, returns the MethodInfo objects
        /// representing its publicly-visible endpoints.  Performs caching.
        /// </summary>
        public static IEnumerable<MethodInfo> GetEndpointMethods(Type moduleType)
        {
            if (!MethodCache.ContainsKey(moduleType)) {
                MethodCache[moduleType] = moduleType.GetMethods(
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                )
                // Only provide methods with a route attribute.
                .Where(method => method.IsDefined(typeof(BaseRouteAttribute)))
                // Only show methods not tagged as "hidden".
                .Where(method => !method.GetCustomAttribute<BaseRouteAttribute>().Hidden)
                .ToList();
            }
            return MethodCache[moduleType];
        }

        /// <summary>
        /// Given a suitably-decorated endpoint method, returns the ParameterInfo objects
        /// representing its publicly-visible parameters.  Performs caching.
        /// </summary>
        public static IEnumerable<ParameterInfo> GetParameters(MethodInfo endpointMethod)
        {
            if (!ParameterCache.ContainsKey(endpointMethod)) {
                ParameterCache[endpointMethod] = endpointMethod.GetParameters()
                    // Only show parameters with attributes.
                    .Where(param => param.IsDefined(typeof(BaseParamAttribute)))
                    // Don't show "hidden" parameters
                    .Where(param => !(param.GetCustomAttribute(
                        typeof(BaseParamAttribute)
                    ) as BaseParamAttribute).Hidden)
                    .ToList();
            }
            return ParameterCache[endpointMethod];
        }
    }
}
