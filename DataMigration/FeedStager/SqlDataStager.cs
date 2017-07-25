using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// SqlFeedStager has the responsiblity
    /// for staging a DataTable input to a staging SQL table, as described in the Plan.  In 
    /// addition to the Plan, which drive the Properties of this object, it uses the App.Config 
    /// to get a Sql connection string. 
    /// </summary>
    public class SqlDataStager : IFeedStager
    {
        static readonly ILog log = LogManager.GetLogger(typeof(SqlDataStager));
        const string TdsVersionForSqlServerEnvironmentVariable = "TDSVER";

        bool useBulkCopy;
        
        // This is property-driven, if true, uses Threading.Task for inserting rows one at a time.
        // This only comes into play if useBulkCopy is false.
        bool useTaskLibrary;  
        
        // If useTaskLibrary is on, sometime it crushes the Sql Server, or the net connection.
        // Using a delay can help.  This is driven from properties in the Descriptor, but
        // it defaults here.
        int taskDelayMilliseconds = 10;
        
        readonly string connectionString;

        public SqlDataStager()
        {
            connectionString = ConfigurationManager.AppSettings["ConnectionString"];
        }

        #region IFeedStager implementation

        /// <summary>
        /// Gets or sets the Properties for this FeedStager.  This is a set of key-value pairs, 
        /// described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation.
        /// </summary>
        /// <value>The properties.</value>
        public IEnumerable<DescriptorProperty> Properties { get; set; }

        public string Location { get; set; }

        public string BadRowsLocation { get; set; }

        public string WarnRowsLocation { get; set; }

        /// <summary>
        /// This is the number of rows at a time the FeedStager will need to process. This
        /// should be set before calling BulkLoad(), and the DataTable passed to BulkLoad()
        /// should have this many rows in it.
        /// </summary>
        /// <value>The size of the batch.</value>
        public int BatchSize { get; set; }

        public int SecondsTimeout { get; set; }

        /// <summary>
        /// Gets the load number. This is used by the DataProcessor when building the DataTable 
        /// that gets passed to this FeedStager in BulkLoad, and is used to disambiguate one 
        /// load from another in the staged results.
        /// </summary>
        /// <returns>The load number.</returns>
        public int GetLoadNumber()
        {
            try {
                using (var conn = new SqlConnection(connectionString)) {
                    using (var cmd = new SqlCommand()) {
                        cmd.CommandText = "up_DM_GetLoadNum";
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Connection = conn;
                        conn.Open();
                        var loadNum = (decimal)cmd.ExecuteScalar();
                        return Decimal.ToInt32(loadNum);
                    }
                }
            } catch (Exception ex) {
                throw new Exception(String.Format("GetLoadNumber - error {0}", ex.Message),
                    ex);
            }
        }

        /// <summary>
        /// This entry point loads the data in the datatable argument into staging.  Depending 
        /// on the Properties set for this object (which are driven from the Plan) it will 
        /// either use SqlBulkCopy, or insert rows one at a time into Sql.
        /// </summary>
        /// <param name="dataTable">The DataTable with a batch of Data to insert</param>
        /// <param name="badRowsDataTable">The DataTable with a batch of Bad Rows to insert.
        /// </param>
        /// <param name="warnRowsDataTable">The DataTable with a batch of warning rows to 
        /// insert.</param>
        public void BulkLoad(DataTable dataTable, DataTable badRowsDataTable, 
                            DataTable warnRowsDataTable)
        {
            if (String.IsNullOrEmpty(connectionString) || String.IsNullOrEmpty(Location)) {
                throw new InvalidOperationException(
                    String.Format("Cannot perform BulkLoad until Location property set"));
            }

            var useBulkCopyString = Utilities.GetProperty("useBulkCopy", Properties, false);
            if (!bool.TryParse(useBulkCopyString, out useBulkCopy)) {
                useBulkCopy = false;
            }

            // Disabling bulkCopy on mono for now; it works with NAMG, but there's a problem
            // with freebcp bulk copying to the StageLoadMemberApi table; this is low priority
            // to fix, since the row-by-row insert works fine.
            var runtimeType = Type.GetType ("Mono.Runtime");
            if (runtimeType != null) {   
                useBulkCopy = false;
            }

            if (useBulkCopy) {
                DoBulkCopy(dataTable, Location); 
                DoBulkCopy(badRowsDataTable, BadRowsLocation);
                DoBulkCopy(warnRowsDataTable, WarnRowsLocation);
            } else {
                // Grovel through the Properties collection, looking for properties that 
                // qualify how the Row By Row insertion is to be done.
                
                // Check and see if we're to use the TPL.
                var useTaskLibraryString = 
                    Utilities.GetProperty("useTaskLibrary", Properties, false);
                    
                if (!String.IsNullOrEmpty(useTaskLibraryString) &&
                    !bool.TryParse(useTaskLibraryString, out useTaskLibrary)) {
                    useTaskLibrary = false;
                }
                
                //  Check to see if we want to add a millisecond-based delay on each task.
                //  This is sometimes necessary on slow connections or the Database is slow.
                var taskDelayMillisecondsString = 
                    Utilities.GetProperty("taskDelayMilliseconds", Properties, false);
                    
                if (!String.IsNullOrEmpty(taskDelayMillisecondsString)) {
                    int.TryParse(taskDelayMillisecondsString, out taskDelayMilliseconds);
                }
                       
                InsertRowByRow(dataTable, Location);
                InsertRowByRow(badRowsDataTable, BadRowsLocation);
            }
        }

        #endregion

        /// <summary>
        /// Inserts an entire datatabe, one row at a time.  This is a fallback method; driven
        /// by the Plan; we're using it until we can get a version of Mono with the SqlBulkCopy 
        /// fixed.
        /// </summary>
        /// <param name="dataTable"> The DataTable with the data to insert.</param>
        /// <param name="tableName"> The name of the Sql table to insert to. </param>
        void InsertRowByRow(DataTable dataTable, string tableName)
        {
            if (dataTable.Rows.Count == 0) {
                return;
            }

            var rowNum = 0;

            var colNames = new List<String>();

            // Due to some vagary of the DataColumnCollection class, you can't use the 'var' 
            // implicit typing construct here; it won't compile.  It's not an IEnumerable 
            // either, so you can't Select to get the column names out.

            var firstBuilder = new StringBuilder("insert into " + tableName + "(");
            foreach (DataColumn col in dataTable.Columns) {
                if (col.ColumnName.Equals(FeedProcessor.IdString,
                        StringComparison.OrdinalIgnoreCase)) {
                    continue;   // Do not add the Id column to the insert statement.
                }
                colNames.Add(col.ColumnName);
            }

            firstBuilder.Append(String.Join(",", colNames) + ") VALUES(");
            var commandPrefix = firstBuilder.ToString();
            
            if (useTaskLibrary) {
                try {
                    var taskArray = new Task[dataTable.Rows.Count];
                    
                    foreach (DataRow dr in dataTable.Rows) {
                        var cmdStr = FormSqlCmdString(commandPrefix, dataTable, dr);
                        taskArray[rowNum] = Task.Factory.StartNew(() => 
                                                         TaskWorker(cmdStr, true));
                        rowNum++;
                    }
                    Task.WaitAll(taskArray);
                } catch (AggregateException aggregateException) {
                    var flattenedException = aggregateException.Flatten();
                    var message = String.Join(",", flattenedException.InnerExceptions.
                                        Select(innerEx => innerEx.Message).ToArray());
            
                    throw new Exception(String.Format("InsertRowByRow -aggregateExceptions " +
                                                        "caught, errors = {0}",
                                                        message), flattenedException);
                }
            } else {  // We're not using multiple tasks to do this.
                foreach (DataRow dr in dataTable.Rows) {
                    var cmdStr = FormSqlCmdString(commandPrefix, dataTable, dr);
                    TaskWorker(cmdStr);
                    rowNum++;
                }
            }
            log.DebugFormat("Successfully inserted {0} rows to staging table {1}",
                rowNum, tableName);
        }

        /// <summary>
        /// This is just a delegate for the TPL to use.   When we're not using the TPL, it's
        /// called sequentially in a loop.
        /// </summary>
        /// <param name="commandString">Sql command string.</param>
        /// <param name="delay">If set to <c>true</c> delay by 'taskDelayMilliseconds'.</param>
        void TaskWorker(String commandString, bool delay = false)
        {
            using (var conn = new SqlConnection(connectionString)) {
                conn.Open();

                //log.Debug (cmdStr);
                ExecuteSingleInsert(commandString, conn);
            }
            if (delay && taskDelayMilliseconds > 0) {
                System.Threading.Thread.Sleep(taskDelayMilliseconds);
            }
        }

        /// <summary>
        /// Uses SqlBulkCopy (bcp) to blow an entire datatable (a single batch of data rows)
        /// into the Sql table.
        /// </summary>
        /// <param name="dataTable">The Datatable to copy into staging.</param>
        /// <param name="tableName">The SQL table that holds the staging data.</param>
        void DoBulkCopy(DataTable dataTable, string tableName)
        {
            if (dataTable.Rows.Count == 0 || String.IsNullOrWhiteSpace(tableName)) {
                return;
            }

            // This is a hack, but I believe it is an acceptable hack to workaround the bug
            // in Mono's implementation of SqlBulkCopy until they get around to fixing it; I 
            // submitted it to Mono's Bugzilla database April 4, 2014.
            var runtimeType = Type.GetType ("Mono.Runtime");
            if (runtimeType == null) {   
                // We're running on Windows, so we can use the SqlBulkCopy class.
                try {
                    using (var conn = new SqlConnection(connectionString)) {
                        conn.Open();
                        using (var bc = new SqlBulkCopy(conn) {
                            BatchSize = BatchSize,
                            BulkCopyTimeout = SecondsTimeout,
                            DestinationTableName = tableName
                        }) {
                            bc.WriteToServer(dataTable);
                            log.DebugFormat("Successfully bulk copied {0} rows to {1}",
                                dataTable.Rows.Count, tableName);
                        }
                    }
                } catch (Exception ex) {
                    throw new Exception(String.Format("BulkLoad - error = {0}", ex.Message), 
                        ex);
                }
            } else { // we're on Mono - use freebcp.
                var bcpFileFullPath = WriteOutBcpFile(dataTable);
                UseFreeBcpToBulkCopyIntoSql(bcpFileFullPath, tableName);

                try {
                    if (File.Exists(bcpFileFullPath)) {
                        File.Delete(bcpFileFullPath);
                    }
                } catch (Exception ex) {
                    log.WarnFormat("Error deleting bcp file, continuing on, error = {0}",
                        ex.Message);
                }
            }
        }

        /// <summary>
        /// Builds the Sql text command to insert a single row into the staging db table.
        /// </summary>
        /// <returns>The sql cmd string.</returns>
        /// <param name="cmdStart">This string has the first reusable part of the insert 
        /// command, i.e. 'Insert Into foo(col1, col2, etc)VALUES('.  It's this method's 
        /// responsibility to add the actual scalar values, and close the parentheses.
        /// </param>
        /// <param name="dt">The DataTable which describes the columns.</param>
        /// <param name="dr">The DataRow with the values.</param>
        static string FormSqlCmdString(string cmdStart, DataTable dt, DataRow dr)
        {
            var sb = new StringBuilder(cmdStart);
            var valList = new List<string>();
            foreach (DataColumn col in dt.Columns) {
                if (col.ColumnName.Equals(FeedProcessor.IdString,
                        StringComparison.OrdinalIgnoreCase)) {
                    continue;   // Do not add the Id column to the insert statement.
                }
                var oVal = dr[col.ColumnName];
                var isNull = oVal == DBNull.Value;
                var sVal = isNull ? "null" : oVal.ToString();
                if (col.DataType == typeof(String) || col.DataType == typeof(DateTime)) {
                    if (!isNull) {
                        sVal = sVal.Replace("'", "''");
                        valList.Add("'" + sVal + "'");
                    } else {
                        valList.Add("null");
                    }
                } else {
                    if (col.DataType == typeof(bool)) {
                        sVal = String.Equals(sVal, "True", 
                            StringComparison.OrdinalIgnoreCase) ? "1" : "0";
                    }
                    valList.Add(sVal);
                }
            }
            sb.Append(String.Join(",", valList) + ")");
            return sb.ToString();
        }

        /// <summary>
        /// Given a fully formed sql command, and an open connection, executes the insert.
        /// </summary>
        /// <param name="command">The insertion command.</param>
        /// <param name="conn">An open Sql Connection.</param>
        static void ExecuteSingleInsert(string command, SqlConnection conn)
        {
            try {
                using (var cmd = new SqlCommand(command, conn)) {
                    cmd.ExecuteNonQuery();
                }
            } catch (Exception ex) {
                throw new Exception(
                    String.Format(
                        "ExecuteSingleInsert - error executing statement {0} err = {1}",
                        command, ex.Message),
                    ex);
            }
        }

        /// <summary>
        /// On Linux/Mono, we need to write out a bcp file to disk, then spawn freebcp to run 
        /// it.  This function just writes it to a tempfileName.  
        /// </summary>
        /// <returns>The temporary bcp file written to disk.  Callers are responsible for
        /// cleaning this file up (i.e. deleting it when done).</returns>
        /// <param name="dataTable">The datatable with the data to write to the file.</param>
        string WriteOutBcpFile(DataTable dataTable)
        {
            var bcpFileFullPath = Path.GetTempFileName();

            try {
                using (var bcpFileWriter = File.CreateText(bcpFileFullPath)) {
                    foreach (DataRow dataRow in dataTable.Rows) {

                        var rowText = new List<string>();

                        // Can't use 'var' syntax here, the DataColumn objects come out as 
                        // typeless objects that way.
                        foreach (DataColumn column in dataTable.Columns) {
                            var dataObject = dataRow[column.ColumnName];
                            if (column.ColumnName.Equals("id", 
                                StringComparison.OrdinalIgnoreCase)) {
                                rowText.Add("1");
                            } else if (DBNull.Value == dataObject) {
                                rowText.Add(String.Empty);
                            } else {
                                if (column.DataType == typeof(bool)) {
                                    rowText.Add((bool) dataObject ? "1" : "0");
                                } else if (column.DataType == typeof(DateTime)) {
                                    rowText.Add(((DateTime) dataObject)
                                        .ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                }
                                else {
                                    rowText.Add(dataObject.ToString());
                                }
                            }
                        }
                        // Write out the row, tab-separated, and then write a new line.
                        bcpFileWriter.Write(String.Join("\t", rowText.ToArray()));
                        bcpFileWriter.Write("\t");
                        bcpFileWriter.WriteLine();  
                    }
                }
            } catch (Exception ex) {
                throw new Exception(String.Format("WriteOutBcpFile - error writing file {0}" +
                                    " error = {1}",
                                    bcpFileFullPath, ex.Message), ex);
            }
            return bcpFileFullPath;
        }

        /// <summary>
        /// Uses freebcp, a utility from freetds package, to copy from a bcp file we've put
        /// on the file system into Sql Server.
        /// </summary>
        /// <param name="bcpFileFullPath">Bcp file full path.</param>
        /// <param name="tableName">Table name to copy to.</param>
        void UseFreeBcpToBulkCopyIntoSql(string bcpFileFullPath, string tableName)
        {
            SetTdsVersionEnvironmentVariable();

            var tokens = connectionString.Split(';');
            var databaseName = ExtractConnectionStringValue(tokens, "database");
            var databaseTableName = databaseName + ".dbo." + tableName;
            var serverName = ExtractConnectionStringValue(tokens, "server");

            // This is on Linux that this code runs, so we can assume Sql Server Authentication
            // rather than Trusted Authentication, so we need a username and password from the
            // connection string.
            var userName = ExtractConnectionStringValue(tokens, "user");
            var password = ExtractConnectionStringValue(tokens, "password");

            var process = new Process
            {
                StartInfo =
                {
                    FileName = "freebcp",
                    Arguments = String.Format("{0} in {1} -S {2} -U {3} -P {4} -c",
                        databaseTableName, bcpFileFullPath, serverName, userName, password),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.OutputDataReceived += StdoutHandler;
            process.ErrorDataReceived += StderrHandler;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.HasExited) {
                process.WaitForExit(1000);
            }

            // Need to make this additional call to make sure stdout and stderr are flushed.
            process.WaitForExit();

            if (0 != process.ExitCode) {
                throw new Exception(String.Format("UseFreeBcpToBulkCopyIntoSql - freebcp " +
                        "process returned exit code {0}", process.ExitCode));
            }
        }

        /// <summary>
        /// Print stdout to the debug log.
        /// </summary>
        private void StdoutHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;
            var message = e.Data as string;
            if (String.IsNullOrEmpty(message))
                return;

            log.Debug(message);
        }

        /// <summary>
        /// Prints stderr to the debug log.
        /// </summary>
        private void StderrHandler(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            var error = e.Data as string;
            if (String.IsNullOrEmpty(error))
                return;

            log.Error(error);
        }

        /// <summary>
        /// Extracts a value from the connection string.   We parse the connection string 
        /// instead of providing properties for the SqlDataStager in the plan because we want
        /// to allow the same plan to work on different configs, when the database name is 
        /// different (e.g. 'cpms_test2', instead of just 'cpms'), and using different logons
        /// and so forth.  This function only gets called when we're on Mono/Linux, and so
        /// we have to use Sql Server Authentication (rather than Trusted), and therefore the
        /// relevant information is in the connection string.
        /// </summary>
        /// <returns>A value from the connection string.</returns>
        /// <param name="tokens">The connection string, broken into tokens.</param>
        /// <param name="key">The key we're looking for.</param>
        string ExtractConnectionStringValue(string[] tokens, string key)
        {
            var matchingToken = tokens.SingleOrDefault(token => token.StartsWith(key, 
                StringComparison.OrdinalIgnoreCase));

            if (String.IsNullOrEmpty(matchingToken)) {
                throw new Exception(String.Format("ExtractConnectionStringValue-cannot parse" +
                    " out {0} value from connectionString = '{1}', this is needed for freebcp", 
                    key, connectionString));
            }
            if (!matchingToken.Contains("=")) {
                throw new Exception(String.Format("ExtractConnectionStringValue - found {0} " +
                    "token '{0}' but no '=', so don't know what to make of it", 
                    matchingToken));
            }
            tokens = matchingToken.Split('=');
            return tokens[1];
        }

        /// <summary>
        /// OpenBcp requires us to set an enviroment variable to talk to SQL Server.  This is 
        /// the equivalent of doing 'export TDSVER=7.0' from the bash shell.   We're driving
        /// the value from the properties for the SqlDataStager in the Plan, instead of hard-
        /// coding the '7.0' value.
        /// </summary>
        void SetTdsVersionEnvironmentVariable()
        {
            var versionString = Utilities.GetRequiredProperty("TdsVersionForSqlServer", 
                                            Properties);
            // It appears to be necessary to let freebcp know that it's Sql Server it's talking
            // to.
            Environment.SetEnvironmentVariable(TdsVersionForSqlServerEnvironmentVariable, 
                                                                        versionString);
        }
    }
}
