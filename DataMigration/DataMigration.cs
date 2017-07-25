using System;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// The principal class for the DataMigration tool.  It contains the Main() function, and
    /// processes command line arguments.  It deserializes the DataMigrationPlan object,
    /// instantiates a MigrationEngine, and calls its Transform() method.
    /// </summary>
    public class DataMigration
    {
        static readonly ILog log = LogManager.GetLogger (typeof(DataMigration));
        static bool noDownload;
        static String singleDataSourceCode;

        // This is used for debugging, to run the postprocess only, when data is already 
        // in staging.
        private static int? postProcessOnlyLoadNum; 

        /// <summary>
        /// The entry point for the DataMigration application
        /// </summary>
        /// <param name="args">The command-line arguments.  Currently, 'nodownload', 
        /// 'postprocessload', and 'singledatasource' are the only 
        /// supported arguments.   The command line processing is very crude, and could stand
        /// to be beefed up.
        /// </param>
        static public void Main (string[] args)
        {
           try {
                var fileInfo = new FileInfo (
                           Path.Combine (Environment.CurrentDirectory, "log4net.config"));

                log4net.Config.XmlConfigurator.Configure (fileInfo);

                ProcessCommandLineArgs (args);

                var plan = DataMigrationPlan.GetPlan ();
                var engine = new MigrationEngine {
                    MigrationPlan = plan,
                    SingleDataSourceCode = singleDataSourceCode,
                    NoDownload = noDownload,
                    PostProcessOnlyLoadNum = postProcessOnlyLoadNum,
                };

                engine.Transform ();

            } catch (Exception ex) {
                log.ErrorFormat ("Error doing UserMigration = {0}, stack = {1}", 
                    ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Processes the command line arguments.  Right now, the only argument actually in use 
        /// is 'nodownloads'.  'singledatasource' and 'notasks' are for future use; 
        /// 'singledatasource' will tell the DataMigration engine to just process a single 
        /// datasourcecode.  'notasks' will tell it to supress the usage of the Task Processing 
        /// Library, and just process feeds sequentially (None of this task logic is 
        /// implemented right now; it processes everything sequentially).  
        /// </summary>
        /// <param name="args">Arguments.</param>
        static private void ProcessCommandLineArgs (IEnumerable<string> args)
        {
            foreach (var arg in args) {
                if (arg.StartsWith ("singledatasource", StringComparison.OrdinalIgnoreCase)) {
                    singleDataSourceCode = arg.Split ('=') [1].Trim();
                } else if (arg.StartsWith ("nodownload", 
                               StringComparison.OrdinalIgnoreCase)) {
                    noDownload = true;
                } else if (arg.StartsWith("postprocessload",
                               StringComparison.OrdinalIgnoreCase)) {
                     postProcessOnlyLoadNum = int.Parse(arg.Split('=')[1]);
                }
            }
        }
    }
}
