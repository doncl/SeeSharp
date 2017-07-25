using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// The SqlPhaseLogger class performs phase logging on behalf of the C# code.   It writes
    /// to a table, specified in the PhaseLoggerDescriptor in the Plan. 
    /// </summary>
    public class SqlPhaseLogger : IPhaseLogger
    {
        static readonly ILog log = LogManager.GetLogger(typeof(SqlPhaseLogger));

        IEnumerable<DescriptorProperty> properties;

        string tableName;
        
        //  We keep a cached list of the entries, so they can be retrieved later for monitoring
        //  purposes.  There isn't that much phase logging going on; it's not a terribly
        // memory-intensive process.
        List<PhaseLogEntry> entries = new List<PhaseLogEntry>();

        const string sqlCommandFormat = "Insert into {0}(LogSource, DataSourceCode, " +
            "LoadNumber, Phase, NumberOfRecords, Description)" + 
            "VALUES({1}, '{2}', {3}, {4}, {5}, '{6}')";
 
        #region IPhaseLogger implementation

        public string ConnectionString {get; set;}

        /// <summary>
        /// Gets or sets the Properties for this PhaseLogger.  This is a set of key-value 
        /// pairs, described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation. 
        /// </summary>
        /// <value>The properties.</value>
        public IEnumerable<DescriptorProperty> Properties 
        {
            get {
                return properties;
            }
            set {
                properties = value;
                if (string.IsNullOrEmpty(ConnectionString)) {
                    var connectKey = Utilities.GetRequiredProperty("connectionKey", properties);
                    ConnectionString = ConfigurationManager.AppSettings[connectKey];
                }
                tableName = Utilities.GetRequiredProperty("tableName", properties);
            }
        }
        
        /// <summary>
        /// Logs the StageLogEntry to the implementation-specific persistence mechanism.
        /// In this implementation, it logs to a Sql Table (DataMigrationPhaseLog) in the cpms
        /// database in Sql Server.
        /// </summary>
        /// <param name="entry">A StageLogEntry with all the information needed to produce
        /// a useful stage log entry.</param>
        public void Log(PhaseLogEntry entry)
        {
            entries.Add(entry);
            var sqlCommand = String.Format(sqlCommandFormat,
                tableName,
                entry.LogSource,
                String.IsNullOrEmpty(entry.DataSourceCode) ? "null" : entry.DataSourceCode,
                entry.LoadNumber.HasValue ? entry.LoadNumber.ToString() : "null",
                entry.PhaseNumber,
                entry.NumberOfRecords.HasValue ?
                    entry.NumberOfRecords.Value.ToString() : "null",
                entry.Description.Replace("'", "''")); // Use two single quotes.

            // It is useful to redundantly use the log4net log.
            log.Debug(entry.Description);
            
            try {
                using (var connection = new SqlConnection(ConnectionString)) {
                    connection.Open();

                    using (var command = new SqlCommand(sqlCommand, connection)) {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) {
                // We're not going to fail the load for this, but we want it to be clear.
                log.ErrorFormat("Log - error attempting to log staging message = {0}",
                                ex.Message);
            }
        }
        
        /// <summary>
        /// Returns the collected entries for an entire load.  We throw away the ones with
        /// null loadnumbers, because we can't order them, and because they're not very
        /// interesting anyway, for this purpose.   Then we order by logsource, and then 
        /// phase number, which gives an approximate logical order.
        /// </summary>
        /// <returns>The entries.</returns>
        /// <param name="loadNumber">The load number.</param>
        public IOrderedEnumerable<PhaseLogEntry> GetLoadEntries(int loadNumber)
        {
            AttemptToLoadTheEntriesStoresInSql(loadNumber);
            return entries.Where(entry => entry.LoadNumber.HasValue && 
                                 entry.LoadNumber == loadNumber)
                    .OrderBy(entry => entry.LogSource)
                    .ThenBy(entry => entry.PhaseNumber);
        }
        #endregion
        
        /// <summary>
        /// Calls into Sql to get the actual list of Phase Log Entries.  The cached set that
        /// we have is just an in-memory backup strategy, in case this fails.  It only has the
        /// phase log entries that were done from the C#, not the ones done by T-SQL based
        /// Post-Processing stages.   If this function is successful, it replaces the cached
        /// list of entries, so the entries can be more complete.
        /// </summary>
        /// <param name="loadNumber">Load number.</param>
        void AttemptToLoadTheEntriesStoresInSql(int loadNumber)
        {
            if (loadNumber == -1) {
                // Probably an error got caught before a loadnumber could be propagated back
                // to the MonitoringPublisher.   This is fine, the PhaseLog entries are a nice
                //-to-have, but not essential.
                return;
            }
            var sqlCommand = String.Format("select * from DataMigrationPhaseLog where " + 
                                        "loadnumber= {0} order by RecordDate", loadNumber);
            try {
                using (var connection = new SqlConnection(ConnectionString)) {
                    connection.Open();

                    using (var command = new SqlCommand(sqlCommand, connection)) {
                        var entriesFromSql = new List<PhaseLogEntry>();
                        var reader = command.ExecuteReader();
                        while (reader.Read()) {
                            var entry = new PhaseLogEntry();
                            entry.LogSource = (int) reader["LogSource"];
                            entry.DataSourceCode = (string) reader["DataSourceCode"];
                            entry.LoadNumber = loadNumber;
                            entry.PhaseNumber = GetNullableNumericField(reader, "Phase");
                            entry.NumberOfRecords = GetNullableNumericField(reader, 
                                                                        "NumberOfRecords");
                            entry.Description = (string) reader["Description"];
                            
                            entriesFromSql.Add(entry);
                        }
                        // We succeeded, so replace the cached list of entries with ones from
                        // Sql, which are more complete.
                        entries = entriesFromSql;
                    }
                }
            }
            catch (Exception ex) {
                // We're not going to fail the load for this, but we want it to be clear.
                log.ErrorFormat("Log - error attempting to log staging message = {0}",
                                ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a nullable numeric field from an IDataReader.
        /// </summary>
        /// <returns>The number, if not null, -1 otherwise.</returns>
        /// <param name="reader">Reader.</param>
        /// <param name="fieldName">Field name.</param>
        int GetNullableNumericField(IDataReader reader, string fieldName)
        {
            var numberObject = reader[fieldName];
            return numberObject != DBNull.Value ? (int) numberObject : -1;
        }
    }
}
