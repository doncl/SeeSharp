using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LitS3;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// This is a location to put utility functions that don't exist in Mono/.NET, or in our
    /// source code.   If a method becomes globally useful, it should be promoted to a shared
    /// location for multiple projects to use.   It's very MS Sql Server specific, although 
    /// there is some code in here for non-Sql Server things. 
    /// </summary>
    static public class Utilities
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Utilities));

        const int maxPasswordLength = 20;

        /// <summary>
        /// Tells if an IDataRecord has a column, avoiding exception being thrown when 
        /// looking for it.
        /// </summary>
        /// <param name="dr">IDataRecord</param>
        /// <param name="columnName">Name of column to look for.</param>
        /// <returns>True if column exists, false otherwise.</returns>
        public static bool HasColumn(IDataRecord dr, string columnName)
        {
            for (int i=0; i < dr.FieldCount; i++) {
                if (dr.GetName(i).Equals(columnName, 
                    StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Given a fully assembly-qualified typename, returns a Mono Type.
        /// </summary>
        /// <returns>The desired Mono type.</returns>
        /// <param name="fullyQualifiedTypeName">Fully assembly-qualified type name.</param>
        public static Type GetTypeFromFqName(string fullyQualifiedTypeName)
        {
            var type = Type.GetType(fullyQualifiedTypeName);
            if (type == null)
            {
                throw new Exception(
                    String.Format("Error creating type {0}", fullyQualifiedTypeName));
            }
            return type;
        }


        /// <summary>
        /// Given a series of uri segments, this method will return a complete Uri.
        /// </summary>
        /// <returns>The desired Uri.</returns>
        /// <param name="paths">Segments of the Uri desired.</param>
        public static string UriCombine(params string[] paths)
        {
            var result = String.Empty;

            if (paths != null && paths.Length > 0) {
                foreach (var p in paths) {
                    string temp = p;

                    if (temp.StartsWith("//"))
                        temp = temp.Substring(1, temp.Length - 1);
                    else if (temp.EndsWith ("//"))
                        temp = temp.Substring(0, temp.Length - 1);

                    if (temp.StartsWith("/") && !String.IsNullOrEmpty(result))
                        result += temp.Substring(1, temp.Length - 1);
                    else {
                        result += temp;
                    }

                    if (!result.EndsWith("/")) {
                        result += "/";
                    }
                }

                if (result.EndsWith("/"))
                    result = result.Substring (0, result.Length - 1);
            }

            return result;
        }

        /// <summary>
        /// Helper function that looks in a properties collection for values, throwing an 
        /// exception if not found.
        /// <returns>The required property value.</returns>
        /// <param name="key">The key for the property value.</param>
        /// <param name="properties">The list of properties to look in.</param>
        /// </summary>
        public static string GetRequiredProperty (string key, 
                                                  IEnumerable<DescriptorProperty> properties)
        {
            return GetProperty(key, properties, true);
        }

        /// <summary>
        /// Helper function that looks in a properties collection for values, throwing an 
        /// exception if not found, although this can be suppressed.  It also will throw an 
        /// exception if the property is not parseable as a bool.
        /// <returns>The boolean property value, if found, and parseable, null if not found and
        /// the 'required' parameter is false, or throws an exception in remaining cases.
        /// </returns>
        /// <param name="key">The key for the property value.</param>
        /// <param name="properties">The list of properties to look in.</param>
        /// <param name="required">Is the property required?  Defaults to true.</param>
        /// </summary>
        public static bool? GetBoolProperty(string key, 
                                    IEnumerable<DescriptorProperty> properties,
                                    bool required = true)
        {
            var boolString = GetProperty(key, properties, required);
            if (String.IsNullOrEmpty(boolString)) {
                return null;
            }
            bool boolValue;
            if (!Boolean.TryParse(boolString, out boolValue)) {
                throw new Exception(String.Format("GetRequiredBoolProperty - value for key " +
                                    "\"{0}\" = \"{1}\", not parseable as bool",
                                    key, boolString));                
            }
            return boolValue;
        }

        /// <summary>
        /// This helper is for getting boolean properties that are required to be in the Plan.
        /// </summary>
        /// <param name="key">The key for the property value.</param>
        /// <param name="properties">The list of properties to look in.</param>
        /// <returns></returns>
        public static bool GetRequiredBoolProperty(string key,
                                            IEnumerable<DescriptorProperty> properties)
        {
            // It's OK to use the .Value operator here; this will properly throw a usable 
            // exception if the property is missing in the plan, and otherwise there will be a
            // valid boolean value.
            return GetBoolProperty(key, properties).Value;
        }

        /// <summary>
        /// Helper function that looks in a properties collection for values that are expected
        /// to be parseable to an int.  Will throw an exception if required == true and the
        /// property is not found, or if it is found, but is not parseable as an integer.
        /// </summary>
        /// <param name="key">Key for the integer property to look for.</param>
        /// <param name="properties">The properties collection.</param>
        /// <param name="required">If true, will throw exception if the property is missing.
        /// </param>
        /// <returns>The parsed int value, or null.</returns>
        public static int? GetIntProperty(string key,
                                    IEnumerable<DescriptorProperty> properties, 
                                    bool required = true)
        {
            var intString = GetProperty(key, properties, required);
            if (String.IsNullOrEmpty(intString)) {
                return null;
            }
            int intValue;
            if (!Int32.TryParse(intString, out intValue)) {
                throw new Exception(String.Format("GetIntProperty - {0} property value is not " 
                                                 + "parseable as int", key ));
            }
            return intValue;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <returns>The property.</returns>
        /// <param name="key">Key.</param>
        /// <param name="properties">Properties.</param>
        /// <param name="required">If set to <c>true</c> required.  This function will throw an
        /// exception if 'required' is true and the property doesn't exist or has a blank or
        /// null value.</param>
        public static string GetProperty(string key, 
                                 IEnumerable<DescriptorProperty> properties, bool required)
        {
            if (properties == null) {
                throw new Exception("GetProperty a- properties block is needed to " +
                "function.");
            }
            var property = properties.SingleOrDefault(p => p.Name.Equals(key));
            if (property == null) {
                if (required) {
                    throw new Exception(
                        String.Format("GetRequiredProperty - missing {0} property",
                            key));
                }
                return null;
            }

            if (String.IsNullOrWhiteSpace (property.Value)) {
                if (required) {
                    throw new Exception(String.Format("GetProperty - {0} property " +
                                                    "value is empty", key));
                }
                return null;
            }

            return property.Value;
        }

        /// <summary>
        /// Given a key from the properties for the PostProcessorDescriptor that describes this
        /// PostProcessor, this will return a matching string in App.Config.
        /// </summary>
        /// <param name="keyInProperties"></param>
        /// <param name="properties">The collection of properties to search through. </param>
        /// <returns></returns>
        public static string GetConfigString(string keyInProperties, 
            IEnumerable<DescriptorProperty> properties)
        {
            var connectionKey = Utilities.GetRequiredProperty(keyInProperties, properties);
            try {
                return ConfigurationManager.AppSettings[connectionKey];
            } catch (Exception ex) {
                throw new Exception(
                    String.Format("GetConfigString - missing App.Config app setting " +
                        "\"{0}\"", keyInProperties), ex);
            }
        }
        
        /// <summary>
        /// Instantiates and returns a LitS3 S3 service object.
        /// </summary>
        /// <returns>The LitS3 S3Service.</returns>
        public static S3Service GetS3Service()
        {
            var appSettings = ConfigurationManager.AppSettings;
            var accessKey = appSettings["AWSAccessKey"];
            var secretAccessKey = appSettings["AWSSecretAccessKey"];
            var service = new S3Service{
                AccessKeyID = accessKey,
                SecretAccessKey = secretAccessKey,
                UseSsl = true,
                UseSubdomains = true
            };
            service.BeforeAuthorize += (sender, e) => {
                e.Request.ServicePoint.ConnectionLimit = int.MaxValue;
            };

            return service;
        }

        /// <summary>
        /// A helper function to encapsulate the creation of integer parameters to stored
        /// procedures.
        /// </summary>
        /// <returns>The SqlParameter object.</returns>
        /// <param name="command">A SqlCommand object to create the parameter for.</param>
        /// <param name="direction">Is the parameter In, Out, In/Out, or Return Value?</param>
        /// <param name="name">Name of the parameter.</param>
        /// <param name="value">The integer value to set.</param>
        public static SqlParameter CreateIntParameter(SqlCommand command,
                    ParameterDirection direction, String name, int value)
        {
            var parameter = command.Parameters.Add(name, SqlDbType.Int);
            parameter.Direction = direction;
            parameter.Value = value;
            return parameter;
        }

        /// <summary>
        /// Creates an nvarchar variable length string SqlParameter object.
        /// </summary>
        /// <returns>The created parameter.</returns>
        /// <param name="command">A SqlCommand object to create the parameter for.</param>
        /// <param name="direction">Is the parameter In, Out, In/Out, or Return Value?</param>
        /// <param name="name">Name of the parameter.</param>
        /// <param name="value">The string value to set.</param>
        /// <param name="count">The length of the string</param>
        public static SqlParameter CreateNVarCharParameter(SqlCommand command,
                    ParameterDirection direction, String name, String value, int count)
        {
            var parameter = command.Parameters.Add(name, SqlDbType.NVarChar, count);
            parameter.Direction = direction;
            parameter.Value = value;
            return parameter;
        }

        /// <summary>
        /// Enables the or disable sql trigger.  This is 'tui_member', on the 'member' table.
        /// It enforces historical business logic that is different than what we are permitting
        /// for the user migration for NAMG.   We disable it, run the post-process, then 
        /// re-enable it. 
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="triggerName">Trigger name.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="enableOrDisable">If set to <c>true</c> enable or disable.</param>
        public static void EnableOrDisableSqlTrigger(string connectionString, 
                                    string triggerName, string tableName, bool enableOrDisable)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    if (DoesTriggerExist(triggerName, connection))
                    {

                        var commandString = String.Format(enableOrDisable ?
                            "ALTER TABLE {0} ENABLE TRIGGER {1}" :
                            "ALTER TABLE {0} DISABLE TRIGGER {1}", tableName, triggerName);

                        using (var command = new SqlCommand(commandString, connection))
                        {
                            command.ExecuteNonQuery();
                            log.DebugFormat("Successfully executed {0}, commandString",
                                            commandString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Error {0} sql trigger {1} on table {2}" +
                        " error = {3}",
                        enableOrDisable ? "enabling" : "disabling",
                        triggerName, tableName, ex.Message), ex);
            }
        }

        /// <summary>
        /// A helper function that piggybacks on an existing open Sql connection, and let us
        /// know whether a trigger exists or not.
        /// </summary>
        /// <returns><c>true</c>, if trigger exists, <c>false</c> otherwise.</returns>
        /// <param name="triggerName">Trigger name.</param>
        /// <param name="connection">Connection.</param>
        static bool DoesTriggerExist(string triggerName, SqlConnection connection)
        {
            var commandString = String.Format("if exists (select * from sys.triggers where " +
                                        "name = '{0}') select 1 else select 0",
                                    triggerName);

            using (var command = new SqlCommand(commandString, connection))
            {
                var answer = (int)command.ExecuteScalar();
                return answer == 1;
            }
        }

        /// Gets a string from an IDataRecord safely, mapping DBNull.Value to null.
        /// </summary>
        /// <param name="record">The data record from a query.</param>
        /// <param name="name">The string value, or null.</param>
        /// <returns></returns>
        public static String GetDbString(IDataRecord record, String name)
        {
            var objectValue = GetDbValue(record, name);
            return objectValue == null ? null : (String)objectValue;
        }

        /// <summary>
        /// Gets a value type from an IDataRecord safely, mapping DBNull.Value to null.
        /// </summary>
        /// <param name="record">The data record from a query.</param>
        /// <param name="name">The column name.</param>
        /// <returns>The  value, or null.</returns>
        public static T? GetDbNullableT<T>(IDataRecord record, String name) 
            where T : struct
        {
            var objectValue = GetDbValue(record, name);
            return objectValue == null ? (T?) null : (T) objectValue;
        }

        /// <summary>
        /// Helper function; turns DBNull.Value into null.
        /// </summary>
        /// <param name="record">Data Record returned from query.</param>
        /// <param name="name">The name of the column.</param>
        /// <returns></returns>
        private static object GetDbValue(IDataRecord record, string name)
        {
            var objectValue = record[name];
            return DBNull.Value == objectValue ? null : objectValue;
        }

        /// <summary>
        /// Writes the bad rows feed file.
        /// </summary>
        /// <returns>The bad rows feed file name.</returns>
        /// <param name="loadNumber">The Loadnumber.</param>
        /// <param name="phase">The Phase to use for Phase Logging.</param>
        /// <param name="dataSourcePlan">DataSourcePlan for this processor.</param>        
        /// <param name="description">Description to use for phase logging.</param>
        /// <param name="badRowsFileName">Name of badRows Feed File.</param>        
        /// <param name="workingDirectory">Directory to write file in.</param>
        /// <param name="connectionString">Db connection string.</param>
        /// <param name="logQuantum">How many rows to do before logging.</param>
        /// <param name="phaseLogger">Phase Logger for logging to Sql</param>
        /// <param name="logSource">logSource integer that uniquely indentifies who's doing
        /// the phase logging.</param>
        public static string WriteBadRowsFeedFile(int loadNumber, DataMigrationPhase phase,
            DataSourcePlan dataSourcePlan, string description, string badRowsFileName,
            string workingDirectory, string connectionString, int logQuantum,
            IPhaseLogger phaseLogger, int logSource) 
        {
            return WriteFeedFileFromDatabaseQuery(
                    String.Format("select * from dbo.BadRows where loadnum = {0}", loadNumber),
                    badRowsFileName,
                    "LoadNumber\tDataSourceCode\tRowNumber\tDestinationColumn\tReason\t" +
                    "ForeignId\tRowData\t",
                    (reader, textWriter) => {
                        var dataSourceCode = (string)reader["DataSourceCode"];
                        var rowNumber = (int)reader["RowNumber"];
                        var destinationColumn = (string)reader["DestColumn"];
                        var reason = (string)reader["Reason"];
                        var foreignId = (string)reader["ForeignId"];

                        // BadRows generated in the PostProcessing stage do not have access to
                        // the original rowdata.
                        string rowData;
                        if (DBNull.Value == reader["RowData"]) {
                            rowData = "null";
                        } else {
                            rowData = (string)reader["RowData"];
                        }

                        textWriter.Write("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                            loadNumber, dataSourceCode, rowNumber, destinationColumn,
                            reason, foreignId, rowData);

                        textWriter.WriteLine();
                    },
                    loadNumber,
                    phase,
                    dataSourcePlan,
                    description,
                    workingDirectory,
                    connectionString,
                    logQuantum,
                    phaseLogger,
                    logSource
             );
        }

        /// <summary>
        /// Helper function that factors out the common code for issuing a database query and
        /// writing a log file to the local file system.
        /// </summary>
        /// <returns>The full filepath for the local file produced.</returns>
        /// <param name="sqlQuery">Text of the sql query to issue.</param>
        /// <param name="fileName">The local filename to use.</param>
        /// <param name="tabDelimitedHeaderText">A header row for the file.</param>
        /// <param name="rowWriter">A delegate that reads from the SqlDataReader and
        ///     writes to the TextWriter passed in.  It knows what arguments to pull off the 
        ///     DataReader and how they should be written to the TextWriter.</param>
        /// <param name="loadNumber">The Loadnumber.</param>
        /// <param name="phase">The Phase to use for Phase Logging.</param>
        /// <param name="dataSourcePlan">DataSourcePlan for this processor.</param>        
        /// <param name="description">Description to use for phase logging.</param>
        /// <param name="workingDirectory">Directory to write file in.</param>
        /// <param name="connectionString">Db connection string.</param>
        /// <param name="logQuantum">How many rows to do before logging.</param>
        /// <param name="phaseLogger">Phase Logger for logging to Sql</param>
        /// <param name="logSource">logSource integer that uniquely indentifies who's doing
        /// the phase logging.</param>
        public static string WriteFeedFileFromDatabaseQuery(string sqlQuery, string fileName,
                    string tabDelimitedHeaderText, Action<SqlDataReader, TextWriter> rowWriter,
                    int loadNumber, DataMigrationPhase phase, DataSourcePlan dataSourcePlan,
                    string description, string workingDirectory, string connectionString,
                    int logQuantum, IPhaseLogger phaseLogger,
                    int logSource) 
        {
            var stopWatch = Stopwatch.StartNew();
            var recordCount = 0;
            var fullFileName = Path.Combine(workingDirectory, fileName);

            try {
                // Open up a text file for reading.
                using (var textWriter = File.CreateText(fullFileName)) {
                    // Write the column name.
                    textWriter.Write(tabDelimitedHeaderText);
                    textWriter.WriteLine();

                    using (var connection = new SqlConnection(connectionString)) {
                        connection.Open();

                        using (var command = new SqlCommand(sqlQuery, connection)) {
                            var reader = command.ExecuteReader();
                            while (reader.Read()) {
                                rowWriter(reader, textWriter);
                                recordCount++;

                                if ((recordCount % logQuantum) == 0) {
                                    log.DebugFormat("WriteFeedFileFromDatabaseQuery - rows " +
                                    "written = {0}", recordCount);
                                }
                            }
                        }
                    }
                }
                phaseLogger.Log(new PhaseLogEntry {
                    LogSource = logSource,
                    DataSourceCode = dataSourcePlan.DataSourceCode,
                    LoadNumber = loadNumber,
                    Phase = phase,
                    NumberOfRecords = recordCount,
                    Description = description
                });
            } catch (Exception ex) {
                throw new Exception(
                    String.Format("WriteFeedFileFromDatabaseQuery - error = {0}",
                                    ex.Message), ex);
            }
            stopWatch.Stop();
            log.DebugFormat("WriteFeedFileFromDictionary - wrote {0} rows in {1} seconds",
                recordCount, stopWatch.Elapsed.TotalSeconds);

            return fullFileName;
        }

        /// <summary>
        /// Copies to s3, using the LitS3 library, and the internal S3Parts class.
        /// </summary>
        /// <param name="s3Location">Full S3 url where we want this to live.</param>
        /// <param name="localFilePath">Local file path to the file to upload.</param>
        /// <param name="baseFileName">The name of the file without path.</param>
        /// <param name="loadNumber">The Load Number.</param>
        /// <param name="phase">The phase to use for phase logging.</param>
        /// <param name="dataSourcePlan">DataSourcePlan for this processor.</param>
        /// <param name="description">Description to use for phase logging.</param>
        /// <param name="phaseLogger">PhaseLogger to use to talk about progress.</param>
        /// <param name="logSource">logSource to use when logging.</param>
        public static void CopyToS3(string s3Location, string localFilePath, 
            string baseFileName, int loadNumber, DataMigrationPhase phase, 
            DataSourcePlan dataSourcePlan, string description, IPhaseLogger phaseLogger,
            int logSource) 
        {
            log.DebugFormat("CopyToS3 - attempting upload from {0} to {1}", baseFileName,
                            s3Location);

            var stopWatch = Stopwatch.StartNew();

            var s3DestinationFile = Utilities.UriCombine(s3Location, baseFileName);
            var s3Parts = S3Parts.FromUrl(s3DestinationFile);
            var s3Service = Utilities.GetS3Service();
            EventHandler<S3ProgressEventArgs> progressHandler = (source, eventArguments) => {
                var percentDone = eventArguments.ProgressPercentage;
                if ((percentDone % 10) == 0) {  // Throttle the log messages.
                    log.DebugFormat("Uploading {0}, progress = {1}%", baseFileName,
                                    percentDone);
                }
            };

            s3Service.AddObjectProgress += progressHandler;

            try {
                s3Service.AddObject(localFilePath, s3Parts.Bucket, s3Parts.File);
                phaseLogger.Log(new PhaseLogEntry {
                    LogSource = logSource,
                    DataSourceCode = dataSourcePlan.DataSourceCode,
                    LoadNumber = loadNumber,
                    Phase = phase,
                    Description = description
                });
            } catch (Exception ex) {
                throw new Exception(String.Format("CopyToS3 - error uploading file {0} = {1}",
                    localFilePath, ex.Message), ex);
            } finally {
                s3Service.AddObjectProgress -= progressHandler;
            }
            stopWatch.Stop();
            log.DebugFormat("CopyToS3 - uploaded file {0} in {1} seconds.",
                baseFileName, stopWatch.Elapsed.TotalSeconds);
        }

        /// <summary>
        /// This function makes every effort to remove the temporary working directory, but
        /// on error will just log a warning and continue on.
        /// </summary>
        public static void DeleteWorkingDirectory(string workingDirectory) 
        {
            try {
                if (String.IsNullOrWhiteSpace(workingDirectory)) {
                    return;
                }
                var directoryInfo = new DirectoryInfo(workingDirectory);
                if (!directoryInfo.Exists) {
                    return;
                }
                Directory.Delete(workingDirectory);
            } catch (Exception ex) {
                // Log a warning and continue.  This is not a fatal error.
                log.WarnFormat("DeleteWorkingDirectory - error deleting {0} = {1}",
                                workingDirectory, ex.Message);
            }
        }

        /// <summary>
        /// This function makes every effort to remove the local feed file, but  on error will 
        /// just log a warning and continue on.
        /// </summary>
        /// <param name="feedFileFullPath">Full path to the file to delete.</param>
        public static void DeleteLocalFeedFile(string feedFileFullPath) 
        {
            try {
                if (String.IsNullOrWhiteSpace(feedFileFullPath)) {
                    return;
                }
                var fileInfo = new FileInfo(feedFileFullPath);
                if (!fileInfo.Exists) {
                    return;
                }
                File.Delete(feedFileFullPath);
            } catch (Exception ex) {
                // Just log a warning and continue; this is not a fatal error.
                log.WarnFormat("DeleteLocalFeedFile - error deleting file {0} = {1}",
                            feedFileFullPath, ex.Message);
            }
        }
    }
}
