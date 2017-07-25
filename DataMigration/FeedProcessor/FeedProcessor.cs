using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DataMigration
{
    /// <summary>
    /// There's an instance of FeedProcessor for each DataSource in the Plan.  It uses the
    /// DataSourcePlan and the contained FeedFilePlans to figure out what files to process, 
    /// which parsers to use, which Stager to use, etc.
    /// 
    /// </summary>
    public class FeedProcessor : IFeedProcessor
    {
        // Define some string constants that are used in multiple places.
        public const string LoadNumberString = "LoadNum";
        public const string DataSourceCodeString = "DataSourceCode";
        public const string IdString = "Id";
        public const string RowNumStr = "RowNumber";
        public const string DestColumnStr = "DestColumn";
        public const string ReasonStr = "Reason";
        public const string ForeignIdStr = "ForeignId";
        public const string RowDataStr = "RowData";

        #region IFeedProcessor implementation

        /// <summary>
        /// The ready-to-use subplan is a collection of components that can be consumed; if
        /// specified, then the datasource can get away without literally specifying the
        /// stager, feedmanager, feedaccessor, and phaselogger explicitly.  
        /// </summary>
        /// <value>The ready to use sub plan.</value>
        public IReadyToUseSubPlan ReadyToUseSubPlan {get; set;}

        /// <summary>
        /// The phaseLogger is specified declaratively in the DataSourcePlan for this feed. 
        /// It's used to log the various stages/steps the feed goes through.
        /// </summary>
        public IPhaseLogger PhaseLogger { get; set; }
        
         /// <summary>
        /// The MonitoringPublisher is the facility for publishing data to be monitored by
        /// operations.
        /// </summary>
        public IPublishMonitoringData MonitoringPublisher {get; set;}

        /// <summary>
        /// The DataMigrationPlan consists of multiple DataSourcePlans, which correspond to 
        /// each DataSourceCode that can be processed. Each DataSourcePlan describes to the
        /// FeedProcessor the list of FeedFilePlans which it needs to process.
        /// </summary>
        public DataSourcePlan Plan { get; set; }

        // Each FeedProcessor instance will be loaded with a set of MethodResolvers sufficient
        // to resolve all the transform methods described by its Plan.
        public IDictionary<string, IMethodResolver> MethodResolvers { get; set; }

        // Each FeedProcessor instance will be loaded with a set of LitmusTestResolver objects 
        // sufficient to resolve all the litmusTest methods as specified in its Plan.
        public IDictionary<string, ILitmusTestResolver> LitmusTestResolvers {get; set;}

        // Each FeedProcessor instance will be loaded with a set of PostRowProcessor objects 
        // sufficient to resolve all the PostRowProcessor methods as specified in its Plan.
        public IDictionary<string, IPostRowProcessorResolver> 
                    PostRowProcessorResolvers {get; set;}

        // The FeedProcessor instance will be loaded with a set of Lookups sufficient to
        // resolve all the lookups described by the litmus tests in its FeedFilePlans.
        public IDictionary<string, ILookup> Lookups { get; set; }

        // The FeedProcessor instance will be loaded with a set of Existence objects 
        // sufficient to resolve all the existence objects needed by the Litmus test functions
        // specified in its FeedFilePlans.
        public IDictionary<string, IExistence> ExistenceObjects { get; set; }

        /// <summary>
        /// As the name implies, the RowProcessor processes each native row of parsed data,
        /// and turns it into normalized data that can be ingested into our staging location.
        /// </summary>
        /// <value>The row processor.</value>
        public IRowProcessor RowProcessor { get; set; }

        /// <summary>
        /// A feed processor has N post processors, which are very domain-specific.  Normally,
        /// there will just be one post-processor (e.g., pushing from staging table into 
        /// Camelot member table), but there could be more (i.e., pushing into Redis).
        /// </summary>
        /// <value>The post processors.</value>
        public IList<IPostProcessor> PostProcessors { get; set; }

        /// <summary>
        /// This entry point implements the first stage of processing for a datasource.  The 
        /// feed files have already been put in place (as a precondition), and this method will 
        /// then parse the feed files, transform them as described by their respective 
        /// transformMaps, and call the DataStager for this FeedProcessor to stage the 
        /// intermediate results (i.e. store them for further processing by the PostProcessing 
        /// entry point). 
        /// <returns>The loadnumber that was generated as part of the processing.  It is 
        /// returned so it can be passed on to the PostProcessors.</returns>
        /// </summary>
        public int Process()
        {
            var batchSize = int.Parse (
                ConfigurationManager.AppSettings ["XformRowsBatchSize"]);

            // The Plan has all the information about local files to process, plus the 
            // transformMaps, plus the parser for each file (they can be different), plus the 
            // table to copy it to, etc.

            var stager = GetFeedStager();
            stager.BatchSize = batchSize;

            var loadNumber = stager.GetLoadNumber ();
            var dataSourceCode = Plan.DataSourceCode;

            var existenceKeys = AddDataSourceSpecificExistences();
            var lookupKeys = AddDataSourceSpecificLookups();

            try {
                foreach (var plan in Plan.FeedFilePlans) {
                    stager.Location = plan.StagingLocation;
                    stager.BadRowsLocation = plan.BadRowsStagingLocation;
                    stager.WarnRowsLocation = plan.WarnRowsStagingLocation;

                    var dataTable = SetUpDataTable(plan);
                    var badRowsDataTable = SetupBadOrWarnRowsDataTable();
                    var warnRowsDataTable = SetupBadOrWarnRowsDataTable();

                    var stopWatch = Stopwatch.StartNew();

                    using (var parser = AcquireParser(plan)) {
                        parser.Open(plan.LocalFile.FullName);
                
                        var message = String.Format("Starting processing of file {0}",
                                                    plan.LocalFile.FullName);
                
                        PhaseLogger.Log(new PhaseLogEntry {
                            LogSource = MigrationEngine.LogSource,
                            DataSourceCode = dataSourceCode,
                            LoadNumber = loadNumber,
                            Phase = DataMigrationPhase.ProcessingRows,
                            Description = message
                        });
                
                        // Skip over any initial lines as defined by config.  Most commonly, 
                        // this is the header row.
                        for (var i = 0; i < plan.SkipLines.GetValueOrDefault(0); i++) {
                            parser.NextRow();
                        }
                
                        IList<string> row = null;
                
                        // process in batches, bulkloading each batch before continuing.
                        do {
                            for (var i = 0; i < batchSize; i++) {
                                row = parser.NextRow();
                
                                if (row == null) {
                                    break;
                                }
                
                                // Obtain the id that is designated by this feedplan as being 
                                // the thing that is most useful to put in the 'ForeignId' 
                                // column in the BadRows collection.
                                var foreignId = ProcessingHelper.GetForeignId(loadNumber, 
                                      dataSourceCode, plan, parser.CurrentRow, MethodResolvers, 
                                      ExistenceObjects, Lookups, row);
                
                
                                // Run the litmus tests.  If any fail, stop right now, and put
                                // the row in the BadRows table. 
                                var reason = ProcessLitmusTests(row, plan);
                                if (!String.IsNullOrEmpty(reason)) {

                                    AddToBadOrWarnRowsDataTable(badRowsDataTable,
                                        dataSourceCode, loadNumber, parser.CurrentRow,
                                        "LitmusTestFailure", reason, foreignId, row);

                                    continue;
                                }

                                var warnings = ProcessWarningTests(row, plan);
                                if (warnings.Count > 0) {
                                    AddToWarnRowsDataTable(warnRowsDataTable, dataSourceCode,
                                        loadNumber, parser.CurrentRow, "WarnTestFailure",
                                        warnings, foreignId, row);

                                    // Do not pass GO - this row still must be processed.
                                }

                                IDictionary<string, string> transformedRow = null;
                                try {
                                    // This is the actual work being done.
                                    transformedRow = RowProcessor.TransformRow(loadNumber,
                                        dataSourceCode, parser.CurrentRow, plan, row);
                
                                } catch (BadRowException brex) {
                                    // Add foreignId context to the exception, so auditors of 
                                    // the BadRows table can see what they want to see.
                                    brex.ForeignId = foreignId;
                                    AddToBadRowsDataTable(badRowsDataTable, dataSourceCode, 
                                        brex, row);
                
                                    continue; // Go get the next row.
                                } catch (Exception ex) {
                                    throw new Exception(String.Format("Process - error at row " 
                                        + " {0} of feed file {1}",
                                        parser.CurrentRow, plan.LocalFile.FullName), ex);
                                }

                                AssertPasswordSanity(transformedRow, parser.CurrentRow);
                                
                                // After EVERYTHING else is done, and we know the row is good,
                                // execute the PostRowProcessors.
                                ProcessPostRowProcessors(loadNumber,row,transformedRow,plan);

                                RowProcessor.AddRowToDataTable(plan, loadNumber,
                                    dataSourceCode, transformedRow, dataTable);
                            }

                            if (dataTable.Rows.Count > 0 || badRowsDataTable.Rows.Count > 0 ||
                                warnRowsDataTable.Rows.Count > 0) {
                                try {
                                    stager.BulkLoad(dataTable, badRowsDataTable,
                                        warnRowsDataTable);
                                }
                                catch (Exception ex) {
                                    throw new Exception(String.Format("Error staging load" +
                                        "number {0}, row {1}, error = {2}",
                                        loadNumber, parser.CurrentRow, ex.Message));
                                }
                                dataTable.Rows.Clear();
                                badRowsDataTable.Clear();
                            }
                        } while (row != null);
                    }
                
                    stopWatch.Stop();
                    var logMessage = String.Format("DataSourceCode {0}, file {1} took {2} " +
                                     "seconds to process and copy to staging.", 
                                         dataSourceCode, plan.FileName, 
                                         stopWatch.Elapsed.TotalSeconds);
                
                    PhaseLogger.Log(new PhaseLogEntry {
                        LogSource = MigrationEngine.LogSource,
                        DataSourceCode = Plan.DataSourceCode,
                        LoadNumber = loadNumber,
                        Phase = DataMigrationPhase.CompletedBulkCopy,
                        Description = logMessage
                    });
                }
            } catch (Exception ex) {
                throw new Exception(String.Format("Error preprocessing load {0} = {1}",
                                            loadNumber, ex.Message));
            } finally  {
                RemoveDataSourceSpecificExistences(existenceKeys);
                RemoveDataSourceSpecificLookups(lookupKeys);
            }
            return loadNumber;
        }

        /// <summary>
        /// Just a check for a mistake that has been made for FFT.  Makes sure that, if we have 
        /// calculated a PasswordImportState of "1", then there needs to be a PasswordHash.
        /// </summary>
        /// <param name="outRow">OutputRow we have built up.</param>
        /// <param name="lineNumber">The line number of the feed.</param>
        private static void AssertPasswordSanity(IDictionary<string, string> outRow, 
                                                int lineNumber)
        {
            if (!outRow.ContainsKey("PasswordImportState") ||
                outRow["PasswordImportState"] != "1") {
                return;
            }

            if (!outRow.ContainsKey("PasswordHash") ||
                String.IsNullOrEmpty(outRow["PasswordHash"]) ||
                !outRow.ContainsKey("PasswordHashSalt") ||
                String.IsNullOrEmpty(outRow["PasswordHashSalt"])) {

                throw new Exception(
                    String.Format(
                        "Something is wrong with this integration - line {0} - processed " +
                        "passwordImportState == 1, yet no PasswordHash - cannot continue - " +
                        "FIX THIS.", lineNumber));
            }
        }

        /// <summary>
        /// Helper which acquires the parser either from the description in the FeedFilePlan
        /// (it favors this one), or if that's missing, from the ReadyToUseSubPlan.
        /// </summary>
        /// <param name="plan">FeedFilePlan for this file.</param>
        /// <returns>An IParser instance.</returns>
        private IParser AcquireParser(FeedFilePlan plan)
        {
            var parserDescriptor = plan.Parser ?? ReadyToUseSubPlan.ParserDescriptor;
            if (null == parserDescriptor) {
                throw new Exception(String.Format("AcquireParser - no parser defined for" +
                                " file {0}, either in feed file plan or in readyToUsePlan",
                                plan.FileName));
            }

            var parser = (IParser)Activator.CreateInstance(
                            Utilities.GetTypeFromFqName(parserDescriptor.Assembly));

            parser.Properties = parserDescriptor.Properties;
            return parser;
        }

        /// <summary>
        /// Gets the feed stager from either the readyToUsePlan or instantiates it through
        /// reflection from the main Plan.
        /// </summary>
        /// <returns>The feed stager.</returns>
        private IFeedStager GetFeedStager()
        {
            if (Plan.FeedStager != null) {
                // Intanstiate the IDataStager object for this group of files.
                var type = Utilities.GetTypeFromFqName(Plan.FeedStager.Assembly);
                var stager = (IFeedStager)Activator.CreateInstance(type);
                var feedStagerDescriptor = Plan.FeedStager;
                if (feedStagerDescriptor.Properties != null &&
                    feedStagerDescriptor.Properties.Length > 0) {
                    stager.Properties = feedStagerDescriptor.Properties;
                }
                return stager;
            }
            if (ReadyToUseSubPlan != null && ReadyToUseSubPlan.Stager != null) {
                return ReadyToUseSubPlan.Stager;
            }
            throw new Exception(String.Format("No Feed Stager found for datasourcecode = {0}",
                                               Plan.DataSourceCode));
        }

        /// <summary>
        /// PostProcess is the final stage of processing, where business rules are applied
        // against the data placed in staging by the Process() endpoint, and resultant rows
        // inserted, modified, and/or deleted at the final location.
        /// <param name="loadNum">The load number to perform post processing on.</param>
        /// <param name="readyToUseSubPlan">A ReadyToUseSubPlan (can be null) that may
        /// have additional PostProcessors to be run first.</param>
        /// </summary>
        public void PostProcess (int loadNum, IReadyToUseSubPlan readyToUseSubPlan)
        {
            if (!Plan.PostProcess) {
                return;
            }

            AcquirePostPostProcessors (readyToUseSubPlan);
            RunPostProcessors(loadNum, PostProcessors);
        }
        #endregion

        private void RunPostProcessors(int loadNum, IEnumerable<IPostProcessor> postProcessors)
        {
            if (postProcessors == null) {
                return;
            }
            foreach (var postProcessor in postProcessors) {
                postProcessor.PostProcess(loadNum, Plan);
            }
        }


        /// <summary>
        /// Adds the data source specific existence Objects.  These are described in the 
        /// DataSourcePlan.  They will be instantiated, and then later removed from the global
        /// set of Existence objects when processing this datasource is complete.
        /// </summary>
        /// <returns>The data source specific existences.</returns>
        IEnumerable<string> AddDataSourceSpecificExistences()
        {
            var existenceKeys = new List<string>();
            if (Plan.Existences != null) {
                var datasourceSpecificExistences = MigrationEngine.AcquireExistenceObjects(
                                                       Plan.Existences);
                foreach (var existenceKeyValuePair in datasourceSpecificExistences) {
                    var key = existenceKeyValuePair.Key;
                    var value = existenceKeyValuePair.Value;

                    if (!ExistenceObjects.ContainsKey(key)) {
                        ExistenceObjects[key] = value;
                        existenceKeys.Add(key);
                    }
                }
            }
            return existenceKeys;
        }

        /// <summary>
        /// Adds the data source specific lookups.  These are also described in the 
        /// DataSourcePlan.  They will be instantiated, and then later removed from the global
        /// set of Lookup objects when processing this datasource is complete.
        /// </summary>
        /// <returns>The data source specific lookups.</returns>
        IEnumerable<string> AddDataSourceSpecificLookups()
        {
            var lookupKeys = new List<string>();
            if (Plan.Lookups != null) {
                var dataSourceSpecificLookups = MigrationEngine.AcquireLookups(Plan.Lookups);

                foreach (var lookupKeyValuePair in dataSourceSpecificLookups) {
                    var key = lookupKeyValuePair.Key;
                    var value = lookupKeyValuePair.Value;

                    if (!Lookups.ContainsKey(key)) {
                        Lookups[key] = value;
                        lookupKeys.Add(key);
                    }
                }
            }
            return lookupKeys;
        }

        /// <summary>
        /// Removes the data source specific lookups.
        /// </summary>
        /// <param name="lookupKeys">Lookup keys.</param>
        void RemoveDataSourceSpecificLookups(IEnumerable<string> lookupKeys)
        {
            foreach (var lookupKey in lookupKeys) {
                if (Lookups.ContainsKey(lookupKey)) {
                    Lookups.Remove(lookupKey);
                }
            }
        }

        /// <summary>
        /// Removes the data source specific existences.
        /// </summary>
        /// <param name="existenceKeys">Existence keys.</param>
        void RemoveDataSourceSpecificExistences(IEnumerable<string> existenceKeys)
        {
            foreach (var existenceKey in existenceKeys) {
                if (ExistenceObjects.ContainsKey(existenceKey)) {
                    ExistenceObjects.Remove(existenceKey);
                }
            }
        }

        /// <summary>
        /// Creates and sets up the DataTable with column information.
        /// </summary>
        /// <returns>A new DataTable, with Columns set up, ready to accept new rows.</returns>
        /// <param name="feedFilePlan">The Plan for this Feed File; it contains all the 
        /// TransformMaps and other information necessary to set up the column headers in this 
        /// DataTable.
        /// </param>
        private DataTable SetUpDataTable (FeedFilePlan feedFilePlan)
        {
            var dt = new DataTable ();

            dt.Columns.Add (IdString, typeof(Int32));
            dt.Columns.Add (LoadNumberString, typeof(Int32));
            dt.Columns.Add (DataSourceCodeString, typeof(string));
            foreach (var map in feedFilePlan.TransformMaps) {
                var t = typeof(String);  // default
                if (map.Type != null) {
                    switch (map.Type) {
                    case "int":
                        t = typeof(int);
                        break;
                    case "long":
                        t = typeof(long);
                        break;
                    case "bool":
                        t = typeof(bool);
                        break;
                    case "float":
                        t = typeof(float);
                        break;
                    case "double":
                        t = typeof(double);
                        break;
                    case "DateTime":
                        t = typeof(DateTime);
                        break;
                    }
                }
                dt.Columns.Add (map.DestCol, t);
            }
            return dt;
        }

        /// <summary>
        /// Instantiates and builds the column definitions for the bad rows DataTable.
        /// </summary>
        /// <returns>The newly created datatable.</returns>
        DataTable SetupBadOrWarnRowsDataTable ()
        {
            var dt = new DataTable ();
            
            dt.Columns.Add (LoadNumberString, typeof(Int32));
            dt.Columns.Add (DataSourceCodeString, typeof(string));
            dt.Columns.Add (RowNumStr, typeof(Int32));
            dt.Columns.Add (DestColumnStr, typeof(String));
            dt.Columns.Add (ReasonStr, typeof(String));
            dt.Columns.Add (ForeignIdStr, typeof(String));
            dt.Columns.Add (RowDataStr, typeof(string));
            
            return dt;
        }

        /// <summary>
        /// Rows are processed in batches, and good and bad rows collected.  This function
        /// add a single bad row to a datatable which is comprised of all the bad rows for a 
        /// single batch.
        /// </summary>
        /// <param name="badRowDataTable">The data table to add to.</param>
        /// <param name="dataSourceCode">The DataSourceCode.</param>
        /// <param name="brex">The BadRowException that has contextual information.</param>
        /// <param name="rowData">The raw parsed row data.</param>
        void AddToBadRowsDataTable (DataTable badRowDataTable, string dataSourceCode, 
                                    BadRowException brex, IList<string> rowData)
        {
            AddToBadOrWarnRowsDataTable(badRowDataTable, dataSourceCode, brex.LoadNumber, 
                                  brex.RowNumber, brex.DestColumn, brex.Reason, brex.ForeignId,
                                  rowData);
        }

        /// <summary>
        /// Helper function for adding rows to data table that takes the individual
        /// arguments.  This makes it possible to do this without a BadRowException being 
        /// present, as in the case of LitmusTests.
        /// </summary>
        /// <param name="dataTable">The data table to add to.</param>
        /// <param name="dataSourceCode">The DataSourceCode.</param>
        /// <param name="loadNumber">The load number of the data.</param>
        /// <param name="rowNumber">The row index.</param>
        /// <param name="destinationColumn">The name of the destination column.</param>
        /// <param name="reason">The reason it's a bad row.</param>
        /// <param name="foreignId">The foreign id for the row.</param>
        /// <param name="rowData">The raw parsed row data.</param>
        private void AddToBadOrWarnRowsDataTable(DataTable dataTable, 
            string dataSourceCode, int loadNumber, int rowNumber, string destinationColumn, 
            string reason, string foreignId, IList<string> rowData)
        {
            var dr = dataTable.NewRow();
            dr[LoadNumberString] = loadNumber;
            dr[DataSourceCodeString] = dataSourceCode;
            dr[RowNumStr] = rowNumber;
            dr[DestColumnStr] = destinationColumn;
            dr[ReasonStr] = reason ?? String.Empty;
            dr[ForeignIdStr] = foreignId ?? String.Empty;
            dr[RowDataStr] = FormXmlDocumentFromRowData(rowData);

            dataTable.Rows.Add(dr);
        }

        /// <summary>
        /// Helper function that adds all the warning rows for a given input row to the 
        /// WarnRows datatable.
        /// </summary>
        /// <param name="warnRowsDataTable">DataTable for WarnRows table.</param>
        /// <param name="dataSourceCode">The DataSourceCode.</param>
        /// <param name="loadNumber">The Load Number.</param>
        /// <param name="rowNumber">The row index.</param>
        /// <param name="destinationColumn">The name of the destination column.</param>
        /// <param name="warnings">The List of Warnings.</param>
        /// <param name="foreignId">The foreignId for the row.</param>
        /// <param name="rowData">The raw parsed row data.</param>
        private void AddToWarnRowsDataTable(DataTable warnRowsDataTable, string dataSourceCode,
                                    int loadNumber, int rowNumber, string destinationColumn,
                                    IList<string> warnings , string foreignId, 
                                    IList<string> rowData)
        {
            // Add one row for each warning.  This means a single input row can generate 
            // multiple warning rows (which is right and proper).
            foreach (var warning in warnings) {
                AddToBadOrWarnRowsDataTable(warnRowsDataTable, dataSourceCode, loadNumber, 
                    rowNumber, destinationColumn, warning, foreignId, rowData);
            }
        }

        /// <summary>
        /// Produces a simple xml object from the row Data.  The reason to do it this way is
        /// that the BadRows table has to support any feed, with any schema.  Instead of using
        /// a blob or nvarchar(max) column, the Xml data type is queryable.
        /// </summary>
        string FormXmlDocumentFromRowData (IList<string> rowData)
        {
            var builder = new StringBuilder ("<dataRow>");
            for (var i = 1; i <= rowData.Count; i++) {
                var dataValue = rowData[i - 1].Replace("]]>", String.Empty);
                builder.AppendFormat ("<dataValue_{0}><![CDATA[{1}]]></dataValue_{0}>",
                    i, dataValue);
            }
            builder.Append ("</dataRow>");
            return builder.ToString ();
        }

        /// <summary>
        /// The DataSourcePlan has a set of PostProcessors, identified by fully-qualified
        /// assembly name.   If there is a readyToUseSubPlan, and it has postprocessors, it
        /// will add those first.
        /// <param name="readyToUseSubPlan">A ReadyToUsePlan (possibly null) they specify
        /// processors to be fired first.</param>       
        /// </summary>
        private void AcquirePostPostProcessors (IReadyToUseSubPlan readyToUseSubPlan)
        {
            PostProcessors = new List<IPostProcessor> ();
            try {

                // Get the processors from the ReadyToUseSubPlan first.
                if (readyToUseSubPlan != null &&
                    readyToUseSubPlan.PostProcessorDescriptors != null) {
                    foreach (var postProcessorDescriptor in 
                        readyToUseSubPlan.PostProcessorDescriptors) {
                        InstantiatePostProcessor(postProcessorDescriptor);
                    }
                }

                // Now get the ones from the DataSourcePlan.
                if (Plan.PostProcessors != null) {
                    foreach (var postProcessorDescriptor in Plan.PostProcessors) {
                        InstantiatePostProcessor(postProcessorDescriptor);
                    }
                }
            } catch (Exception ex) {
                throw new Exception (
                    String.Format ("AcquirePostPostProcessors - error {0}", ex.Message), ex);
            }
        }

        /// <summary>
        /// Instantiates a single PostProcessor.
        /// </summary>
        /// <param name="postProcessorDescriptor"></param>
        /// <returns>Instantiated PostProcessor object.</returns>
        private IPostProcessor InstantiatePostProcessor(
                                    PostProcessorDescriptor postProcessorDescriptor)
        {
            var postProcessor = (IPostProcessor)
                Activator.CreateInstance(Type.GetType(postProcessorDescriptor.Assembly));

            // Propagate the plan-specified properties to the instance, and set the 
            // PhaseLogger to be the same as the enclosing FeedProcessor.
            postProcessor.Properties = postProcessorDescriptor.Properties;
            postProcessor.PhaseLogger = PhaseLogger;

            PostProcessors.Add(postProcessor);
            return postProcessor;
        }

        /// <summary>
        /// Processes the post-row processor methods. These are methods that optionally run
        /// subsequent to a row successfully processing, that generate some desired 
        /// side-effect.
        /// </summary>
        /// <param name="loadNumber">The load number.</param>
        /// <param name="parsedInputRow">The raw row.</param>
        /// <param name="transformedRow">The complete output row.</param>
        /// <param name="plan">A FeedFilePlan with PostRowProcessorDescriptors.</param>
        void ProcessPostRowProcessors(int loadNumber, IList<string> parsedInputRow, 
            IDictionary<string, string> transformedRow,FeedFilePlan plan) 
        {
            if (plan.PostRowProcessorDescriptors == null) {
                return;
            }
            foreach (var postRowProcessorDescriptor in plan.PostRowProcessorDescriptors) {
                InvokePostRowProcessor(loadNumber, parsedInputRow, transformedRow, 
                    postRowProcessorDescriptor);
            }
        }


        /// <summary>
        /// Processes the litmus tests.
        /// </summary>
        /// <returns>A reason for failing, on the first test that fails.</returns>
        /// <param name="row">The raw row.</param>
        /// <param name="plan">A FeedFilePlan with Litmus Test Descriptors.</param>
        string ProcessLitmusTests(IList<string> row, FeedFilePlan plan)
        {
            foreach (var litmusTestDescriptor in plan.LitmusTestDescriptors) {
                if (!PerformLitmusTest(row, litmusTestDescriptor)) {
                    return litmusTestDescriptor.Reason;
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// Processes the warning tests.  Warning Tests are litmus tests, the only difference
        /// being that the handling of a failure is different.   Warning tests cause the row
        /// to be copied to the WarnRows table, but we still process the row.
        /// </summary>
        /// <returns>A list of warnings</returns>
        /// <param name="row">The raw row.</param>
        /// <param name="plan">A FeedFilePlan with Warning Test Descriptors.</param>
        List<string> ProcessWarningTests(IList<string> row, FeedFilePlan plan)
        {
            var warnings = new List<String>();
            if (plan.WarningTestDescriptors != null) {
                foreach (var warnTestDesciptor in plan.WarningTestDescriptors) {
                    if (!PerformLitmusTest(row, warnTestDesciptor)) {
                        warnings.Add(warnTestDesciptor.Reason);
                    }
                }
            }
            return warnings;
        }

        /// <summary>
        /// This function is called for litmustest functions specified in the FeedFileMap.
        /// </summary>
        /// <param name="row">The raw data row.</param>
        /// <param name="map">An IMethodMap that describes how to perform the Litmus Test.
        /// </param>
        /// <returns>True, if the row passed the test, false otherwise.</returns>
        bool PerformLitmusTest(IList<string> row, IMethodMap map)

        {
            if (String.IsNullOrEmpty(map.MethodTag)) {
                throw new Exception(
                    String.Format("TransformRow - have method {0}, but no methodTag",
                        map.Method));
            }
            if (!LitmusTestResolvers.ContainsKey(map.MethodTag)) {
                throw new Exception(
                    String.Format("PerformLitmusTest - methodTag {0} for" +
                          " method {1} missing from map of LitmusTestResolvers - config error",
                          map.MethodTag, map.Method));
            }
            var args = ProcessingHelper.ParseMethodArguments(row, map, Lookups);
            var method = LitmusTestResolvers[map.MethodTag].ResolveLitmusTest(map.Method);
            try {
                return method(Lookups, ExistenceObjects, args);
            } catch (Exception ex) {
                throw new Exception(
                    String.Format("PerformLitmusTest - transformation " +
                    "function {0} incorrectly threw an error {1} - this is a " +
                    "fatal bug in the litmus test function code",
                        map.Method, ex.Message));
            }
        }

        /// <summary>
        /// This function is called for PostRowProcessor methods specified in the FeedFileMap.
        /// </summary>
        /// <param name="loadNumber"></param>
        /// <param name="row">The raw data row.</param>
        /// <param name="rowInProgress">The completed output row.</param>
        /// <param name="map">An IMethodMap that describes how to perform the PostRowProcessor.
        /// </param>
        void InvokePostRowProcessor(int loadNumber, IList<string> row, 
            IDictionary<string, string> rowInProgress, IMethodMap map)

        {
            if (String.IsNullOrEmpty(map.MethodTag)) {
                throw new Exception(
                    String.Format("InvokePostRowProcessor - have method {0}, but no methodTag",
                        map.Method));
            }
            if (!PostRowProcessorResolvers.ContainsKey(map.MethodTag)) {
                throw new Exception(
                    String.Format("InvokePostRowProcessor - methodTag {0} for" +
                          " method {1} missing from map of InvokePostRowProcessorResolvers - "
                        + "config error",
                          map.MethodTag, map.Method));
            }
            var args = ProcessingHelper.ParseMethodArguments(row, map, Lookups);
            var method = PostRowProcessorResolvers[map.MethodTag]
                                    .ResolvePostRowProcessor(map.Method);
            try {
                method(loadNumber, Lookups, ExistenceObjects, rowInProgress, args);
            } catch (Exception ex) {
                throw new Exception(
                    String.Format("InvokePostRowProcessor - " +
                    "method {0} incorrectly threw an error {1} - this is a fatal bug in the " +
                    "postRowProcessor method code",
                        map.Method, ex.Message));
            }
        }

    }
}
