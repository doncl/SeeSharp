using System;
using System.Collections.Generic;
using System.Reflection;

namespace DataMigration
{
    /// <summary>
    /// This class, given an in memory assembly, will grovel out the TransformMethods and 
    /// LitmusTests, exposing them both as dictionaries for the benefit of the consumer 
    /// (typically an IMethodResolver implementation).
    /// </summary>
    public class ReflectionResolverHelper
    {
        public IDictionary<string, TransformMethod> TransformMethods { get; set; }
        public IDictionary<string, LitmusTest> LitmusTests { get; set; }
        public IDictionary<string, PostRowProcessor> PostRowProcessors {get; set;}

        /// <summary>
        /// This method does the actual work of groveling out the TransformMethods and 
        /// LitmusTests.   
        /// </summary>
        /// <param name="assembly">The in-memory assembly to inspect.</param>
        public void Initialize(Assembly assembly)
        {
            TransformMethods =
                new Dictionary<string, TransformMethod>(StringComparer.OrdinalIgnoreCase);
            LitmusTests = new Dictionary<string, LitmusTest>(StringComparer.OrdinalIgnoreCase);
            PostRowProcessors = 
                new Dictionary<string, PostRowProcessor>( StringComparer.OrdinalIgnoreCase);

            try {
                var types = assembly.GetTypes();
                foreach (var type in types) {
                    var methodInfos = type.GetMethods(BindingFlags.Public | 
                                                      BindingFlags.Static);

                    foreach (var methodInfo in methodInfos) {
                        if (methodInfo.IsGenericMethodDefinition) {
                            continue;
                        }
                        var transformMethod = MakeDelegateT<TransformMethod>(methodInfo);
                        if (null != transformMethod) {
                            TransformMethods.Add(methodInfo.Name, transformMethod);
                        }
                        else {
                            var litmusTest = MakeDelegateT<LitmusTest>(methodInfo);
                            if (null != litmusTest) {
                                LitmusTests.Add(methodInfo.Name, litmusTest);
                            } else {
                                var postRowProcessor = 
                                    MakeDelegateT<PostRowProcessor>(methodInfo);
                                if (null != postRowProcessor) {
                                    PostRowProcessors.Add(methodInfo.Name, postRowProcessor);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw new Exception(String.Format("ReflectionResolverHelper.Initialize - " +
                        "error = {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// Attempts to make a TransformMethod or LitmusTest from a methodInfo.
        /// </summary>
        /// <returns>An instantiated delegate, redy for use, or null.</returns>
        /// <param name="methodInfo">MethodInfo to create the delegate from.</param>
        /// <typeparam name="T">Either TransformMethod or LitmusTest.</typeparam>
        private static T MakeDelegateT<T>(MethodInfo methodInfo) where T : class
        {
            try {
                var tDelegate = Delegate.CreateDelegate(typeof(T), methodInfo, false);
                if (tDelegate == null) {
                    return null;
                }
                var tInstance = tDelegate as T;
                return tInstance;  // returns null if tInstance can't be cast to a T.
            }
            catch (Exception ex) {
                throw new Exception(String.Format("Error Making delegate from methodinfo {0}" +
                    " error = {1}, stack = {2}", methodInfo.Name, ex.Message, ex.StackTrace));
            }
        }
    }
}
