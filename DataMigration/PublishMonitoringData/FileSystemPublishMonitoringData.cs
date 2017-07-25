using System;
using System.IO;
using System.Linq;
using log4net;

namespace DataMigration
{
    public class FileSystemPublishMonitoringData : IPublishMonitoringData
    {
        static readonly ILog log = 
            LogManager.GetLogger(typeof(FileSystemPublishMonitoringData));
        
        // Write separator lines out at 95 characters.  If it's good enough for our source 
        // code, then it's good enough for the monitoring status file.
        const int nominalLineLength = 95;
        const string monitoringFileName = "dataMigrationRun";
        
        #region IPublishMonitoringData implementation
        
        /// <summary>
        /// Create a file called 'success', with all the relevant information.  It will put one
        /// file in a directory called 'latest', overwriting.
        /// </summary>
        /// <param name="plan">Plan.</param>
        /// <param name="logEntries">Log entries.</param>
        public void PublishSuccess(DataSourcePlan plan,  
                                   IOrderedEnumerable<PhaseLogEntry> logEntries)
        {
            WriteLatestAndHistoricalFiles(plan, true, logEntries, null);
        }
        
        /// <summary>
        /// Publishs a file called 'failure' with all the relevant information.  It will put
        /// one in a directory called 'latest', removing whatever is there ('success' or 
        /// 'failure') and 
        /// </summary>
        /// <param name="plan">Plan.</param>
        /// <param name="logEntries">Log entries.</param>
        /// <param name="exception">Exception.</param>
        public void PublishFailure(DataSourcePlan plan, 
                            IOrderedEnumerable<PhaseLogEntry> logEntries, Exception exception)
        {
            WriteLatestAndHistoricalFiles(plan, false, logEntries, exception);
        }
        #endregion
        
        /// <summary>
        /// Writes the monitoring report file to both the 'latest', and to a historical 
        /// directory, which is named in a sortable data format.  Thus, there's a complete 
        /// record of runs, whether they succeeded or failed, and what happened in either case.
        /// </summary>
        /// <param name="plan">DataSourcePlan.</param>
        /// <param name="success">If true, writes 'true' in file, else 'false'.
        /// </param>
        /// <param name="logEntries">List of Log entries.</param>
        /// <param name="exception">Exception (can be null).</param>
        void WriteLatestAndHistoricalFiles(DataSourcePlan plan, bool success,
                                           IOrderedEnumerable<PhaseLogEntry> logEntries,
                                           Exception exception)
        {
            try {
                string latestPath;
                string historicalPath;
            
                GetLatestAndHistoricalPaths(plan, out latestPath, out historicalPath);

            
                latestPath = Path.Combine(latestPath, monitoringFileName);
                historicalPath = Path.Combine(historicalPath, monitoringFileName);

                // Write the file out in 'latest', and then copy it to the dated historical 
                // directory.   
                WriteMonitoringFile(plan, latestPath, success, logEntries, exception);
                File.Copy(latestPath, historicalPath);
            }
            catch (Exception ex) {
                log.ErrorFormat("WriteLatestAndHistoricalFiles - error {0}, stack = {1}", 
                    ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Gets the latest and historical paths.  This function simply returns the paths for
        /// calling code to use for generating 'latest' and 'historical' files.
        /// </summary>
        /// <param name="plan">The DataSourcePlan.</param>
        /// <param name="pathForLatest">The directory path for the 'latest' file.</param>
        /// <param name="pathForHistorical">The directory path for the 'historical' file.
        /// </param>
        void GetLatestAndHistoricalPaths(DataSourcePlan plan, out string pathForLatest, 
                                                                out string pathForHistorical)
        {
            var date = DateTime.UtcNow.ToString("s").Replace(":", ".");
            var current = Directory.GetCurrentDirectory();
            pathForLatest = Path.Combine(current, plan.DataSourceCode, "latest");
            pathForHistorical = Path.Combine(current, plan.DataSourceCode, date);
            Directory.CreateDirectory(pathForLatest);
            Directory.CreateDirectory(pathForHistorical);
        }
        
        /// <summary>
        /// Writes the monitoring status file out to disk.
        /// </summary>
        /// <param name="plan">The DataSourcePlan.</param>
        /// <param name="monitoredFilePath">The full path to the file to write.</param>
        /// <param name="success">Whether we're in a success or failure state.</param>
        /// <param name="logEntries">The list of Log entries.</param>
        /// <param name="exception">An exception, which can be null.  If non-null, the function
        /// will write out the exception message and stacktrace.</param>
        void WriteMonitoringFile(DataSourcePlan plan, string monitoredFilePath, bool success,
                         IOrderedEnumerable<PhaseLogEntry> logEntries,
                         Exception exception)
        {
            using (var textWriter = File.CreateText(monitoredFilePath)) {
                // The first line (true or false) is the main purpose of the file, and is there
                // for operations to key off.
                textWriter.Write("{0}", success ? "true": "false");
                textWriter.WriteLine();
                
                textWriter.Write("Date (UTC): {0}", DateTime.UtcNow);
                textWriter.WriteLine();
                
                textWriter.Write("DataSourceCode: {0}", plan.DataSourceCode);
                textWriter.WriteLine();
                
                if (exception != null) {
                    WriteSeparatorLine(textWriter);
                    textWriter.Write("EXCEPTION INFORMATION");
                    textWriter.WriteLine();
                    textWriter.Write("Exception type: {0}", exception.GetType().FullName);
                    textWriter.WriteLine();
                    textWriter.Write("Exception message: {0}", exception.Message);
                    textWriter.WriteLine();
                    textWriter.Write("Exception stacktrace: {0}", exception.StackTrace);
                    textWriter.WriteLine();
                }
                
                textWriter.WriteLine();
                WriteSeparatorLine(textWriter);
                
                textWriter.WriteLine();
                textWriter.Write("PHASE LOG ENTRIES");
                textWriter.WriteLine();
                
                foreach (var logEntry in logEntries) {
                    textWriter.Write("LogSource: {0}\tPhase: {1}\tNumberOfRecords: {2}\t" + 
                        "Description:{3}", 
                        logEntry.LogSource, logEntry.PhaseNumber, 
                        logEntry.NumberOfRecords.GetValueOrDefault(-1), 
                        String.IsNullOrEmpty(logEntry.Description) ? 
                                "(none)" : logEntry.Description);
                                
                    textWriter.WriteLine();
                }
            }
        }
        
        /// <summary>
        /// Writes out a separator line to the file.
        /// </summary>
        /// <param name="writer">Stream writer object.</param>
        static void WriteSeparatorLine(StreamWriter writer)
        {
            for (var i = 0; i < nominalLineLength; i++) {
                writer.Write("=");
            }
            writer.WriteLine();
        }
    }
}
