using log4net;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace DataMigration
{
    /// <summary>
    /// SqlQueryDrivenLookup is a very simple class that knows how to take the text of a sql
    /// query from its descriptor in the plan, execute it, and perform key-value lookups using
    /// the values obtained via the query.  It is not for use with stored procedures.
    /// </summary>
    public class SqlQueryDrivenLookup : AbstractBaseLookup
    {
        static readonly ILog log = LogManager.GetLogger(typeof(SqlQueryDrivenLookup));
        string connectionString;
        string queryText;
        string valueName;
        string keyName;

        const int loggingModulo = 10000;
        
        #region ILookup implementation
        
        /// <summary>
        /// This is essentially a two-phase ctor. The sequence of construction is, instantiate
        /// the lookup, then set its properties, then call Init() so it can migrate the 
        /// property values to their implementation-specific roles.
        /// In this case, Init() will get the connectionKey, and then the connectionString from
        /// the properties, and the query string, and use that to execute a query.
        /// </summary>
        public override void Init()
        {
            var connectionKey = GetRequiredProperty("connectionKey");
            connectionString = ConfigurationManager.AppSettings[connectionKey];
            queryText = GetRequiredProperty("queryText");
            valueName = GetRequiredProperty("valueName");
            keyName = GetRequiredProperty("keyName");

            var row = 0;

            log.DebugFormat("Processing Sql lookup query {0}", queryText);
            
            try {
                using (var connection = new SqlConnection(connectionString)) {
                    connection.Open();
                    using (var command = new SqlCommand(queryText, connection)) {
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                row++;
                                var valueObject = reader[valueName];
                                var value = valueObject == DBNull.Value ? 
                                           String.Empty : valueObject.ToString();

                                var keyObject = reader[keyName];
                                var key = keyObject == DBNull.Value ?
                                           String.Empty : keyObject.ToString();

                                if (String.IsNullOrWhiteSpace(key)) {
                                    continue;  // not adding empty keys.
                                }

                                if (LookupValues.ContainsKey(key)) {
                                    log.WarnFormat("Key: \"{0}\" appears more than once, " +
                                                    "ignoring", key);
                                    continue;
                                }

                                try {
                                    LookupValues.Add(key.Trim(), value.Trim());
                                }
                                catch (Exception ex) {
                                    throw new Exception(String.Format("Init - error adding " +
                                        "key {0} = {1}", key, ex.Message), ex);
                                }

                                if (row % loggingModulo == 0) {
                                    log.DebugFormat("Loaded {0} rows from Sql", row);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                throw new Exception(String.Format("Init - Error = {0}", ex.Message), ex);
            }
        }

        #endregion

        /// <summary>
        /// Helper function that looks in the properties set by the Plan for this lookup.  This
        /// implementation uses these properties extensively.
        /// </summary>
        /// <returns>The required property value.</returns>
        /// <param name="key">The key for the property value.</param>
        string GetRequiredProperty (string key)
        {
            return Utilities.GetRequiredProperty(key, Properties);
        }
    }
}
