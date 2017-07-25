using log4net;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace DataMigration
{
    /// <summary>
    /// SqlQueryDrivenExistence - this class can produce an IExistence implementation, given
    /// a query to Sql Server.
    /// </summary>
    public class SqlQueryDrivenExistence : AbstractBaseExistence
    {
        static readonly ILog log = LogManager.GetLogger(typeof(SqlQueryDrivenExistence));
        string connectionString;
        string queryText;
        string itemName;
        const int loggingModulo = 10000;

        #region IExistence implementation
        /// <summary>
        /// This is essentially a two-phase ctor. The sequence of construction is, instantiate
        /// the object, then set its properties, then call Init() so it can migrate the 
        /// property values to their implementation-specific roles.
        /// This implementation will query sql to get values to populate in the internal Values
        /// HashSet.
        /// In this case, Init() will get the connectionKey, and then the connectionString from
        /// the properties, and the query string, and use that to execute a query.
        /// </summary>
        public override void Init()
        {
            var connectionKey = GetRequiredProperty("connectionKey");
            connectionString = ConfigurationManager.AppSettings[connectionKey];
            queryText = GetRequiredProperty("queryText");
            itemName = GetRequiredProperty("itemName");

            log.DebugFormat("Processing Sql lookup query {0}", queryText);
            var row = 0;

            try {
                using (var connection = new SqlConnection(connectionString)) {
                    connection.Open();
                    using (var command = new SqlCommand(queryText, connection)) {
                        using (var reader = command.ExecuteReader()) {
                            while (reader.Read()) {
                                row++;
                                var itemObject = reader[itemName];
                                var item = itemObject == DBNull.Value ? 
                                           String.Empty : itemObject.ToString();

                                Values.Add(item.Trim());

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
        /// Helper function that looks in the properties set by the Plan for this IExistence.  
        /// This implementation uses these properties extensively.
        /// </summary>
        /// <returns>The required property value.</returns>
        /// <param name="key">The key for the property value.</param>
        string GetRequiredProperty (string key)
        {
            return Utilities.GetRequiredProperty(key, Properties);
        }
    }
}
