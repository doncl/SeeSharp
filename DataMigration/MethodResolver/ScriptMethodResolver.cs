using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace DataMigration
{
    public class ScriptMethodResolver : IMethodResolver, ILitmusTestResolver,
                                        IPostRowProcessorResolver
    {
        static readonly ILog log = LogManager.GetLogger(typeof(ScriptMethodResolver));

        ReflectionResolverHelper reflectionResolverHelper = new ReflectionResolverHelper();

        #region IMethodResolver

        /// <summary>
        /// For a given TransformMethod name, returns the implementation.
        /// </summary>
        /// <param name="xformName">The name of the transform.</param>
        /// <returns>The delegate for the method.</returns>
        public TransformMethod ResolveTransformationMethod(string xformName)
        {
            if (!reflectionResolverHelper.TransformMethods.ContainsKey(xformName)) {
                throw new Exception(String.Format("ResolveTransformationMethod - no entry" +
                                                    " for xform {0}",
                                                    xformName));
            }
            return reflectionResolverHelper.TransformMethods[xformName];
        }
        #endregion

        #region ILitmusTestResolver

        /// <summary>
        /// For a given LitmusTest name, returns the implementation.
        /// </summary>
        /// <param name="litmusTestName">The name of the litmus test.</param>
        /// <returns>The delegate for the litmus test.</returns>
        public LitmusTest ResolveLitmusTest(string litmusTestName)
        {
            if (!reflectionResolverHelper.LitmusTests.ContainsKey(litmusTestName)) {
                throw new Exception(String.Format("ResolveLitmusTest - no entry for litmus" +
                                                   " test {0}",
                                    litmusTestName));
            }
            return reflectionResolverHelper.LitmusTests[litmusTestName];
        }
        #endregion

        #region IPostRowProcessorResolver
        /// <summary>
        /// For a given PostRowProcessor name, return the implementation.
        /// </summary>
        /// <param name="postRowProcessorName">The name of the postRowProcessor.</param>
        /// <returns>The delegate for the postRowProcessor.</returns>
        public PostRowProcessor ResolvePostRowProcessor(string postRowProcessorName)
        {
            if (!reflectionResolverHelper.PostRowProcessors.ContainsKey(postRowProcessorName)){
                throw new Exception(String.Format("ResolvePostRowProcessor - no entry for " +
                    "postRowProcessor {0}", postRowProcessorName));
            }
            return reflectionResolverHelper.PostRowProcessors[postRowProcessorName];
        }
        #endregion

        /// <summary>
        /// Given the path to the script file, compiles it into an in-memory assembly, and uses
        /// the RefelectionResolverHelper to grovel out the TransformMethods and LitmusTests.
        /// </summary>
        /// <param name="pathToFile">The full path to the script file.</param>
        public void BuildResolversFromScriptFile(String pathToFile)
        {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var parameters = new CompilerParameters {
                GenerateExecutable = false,
                GenerateInMemory = true,
                WarningLevel = 3,
                TreatWarningsAsErrors = false,
                CompilerOptions = "/debug+",
            };

            var assemblyNames = new HashSet<string>();

            IEnumerable<Assembly> assemblies;
            try {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var domainAssembly in assemblies) {
                    if (domainAssembly.FullName.StartsWith("Microsoft.GeneratedCode")) {
                        continue;
                    }
                    var name = domainAssembly.GetName().Name;

                    // Workaround for the fact that monodevelop has its own nunit built-in.
                    if (name.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) != -1) {
                        continue;
                    }
                    if (!assemblyNames.Contains(name)) {
                        parameters.ReferencedAssemblies.Add(domainAssembly.Location);
                        assemblyNames.Add(name);
                    }
                }
            } catch (Exception ex)  {
                var error = String.Format("BuildResolversFromScriptFile - error = {0}", 
                                        ex.Message);
                throw new Exception(error, ex);
            }

            var results = provider.CompileAssemblyFromFile(parameters, pathToFile);
            if (results.Errors.HasErrors) {
                log.ErrorFormat("Compilation errors for file {0}", pathToFile);
                foreach (var compilerError in results.Errors.OfType<CompilerError>()) {
                    log.ErrorFormat("Line {0}, error = {1}",
                        compilerError.Line, compilerError.ErrorText);
                }
                var errors = String.Join(",", results.Output);
                throw new Exception(String.Format("BuildResolversFromScriptFile - compile " +
                       "errors for file {0} = {1}", pathToFile, errors));
            }
            reflectionResolverHelper = new ReflectionResolverHelper();
            reflectionResolverHelper.Initialize(results.CompiledAssembly);
        }
    }
}

