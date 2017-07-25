using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace DataMigration
{
    /// <summary>
    /// The SqlDataSource class can use Sql data from a table or query, and present it to the
    /// system as an IParser-based data source.  It could be used for getting data from SQL
    /// Server into Redis, for example.
    ///
    /// Caveats:  It assumes there is an identity column on the table or at least some sort
    /// of id or other column that is monotonically increasing.  It may well work with data
    /// sources where there are 'holes' in the sequence of ('ids'), but this has not been 
    /// tested.  Take a look at DataMigrationPlan.xml, which shows the beginnings of an 
    /// example of usage for this class.
    /// </summary>
    public class SqlDataSource : IParser
    {
        string connectString;
        int minId;
        int maxId;
        int currentStart = 0;
        int currentEnd;
        int batchSize;
        string queryFormatString;
        int currentGlobalRowIndex = 0;
        List<List<string>> batchOfRows;
        int currentBatchRowIndex = 0;

        #region IParser implementation
        
        public IEnumerable<DescriptorProperty> Properties { get; set; }

        /// <summary>
        /// Gets the current row, zero-relative.
        /// This implementation keeps track of the current row with the currentGlobalRowIndex
        /// member.
        /// </summary>
        /// <value>The current row.</value>
        public int CurrentRow {
            get {
                return currentGlobalRowIndex;
            }
        }

        /// <summary>
        /// Open the file the specified Path. The caller is responsible for calling Dispose 
        /// once Open() has been called.
        ///
        ///  This implementation will check to see that all the properties have been set in 
        /// the plan so it can connect to SQL, and execute its queries.   It will then get the
        /// min and max ids for the set of rows by executing the queries specified in the plan.
        /// Then it will use those values to form the main query to query for the first batch
        /// of rows.
        /// </summary>
        /// <param name="filePath">In this implementation, the filePath argument is unused.
        /// </param>
        public void Open (string filePath)
        {
            ValidateProperties();
            GetMinAndMaxIds();
            currentStart = minId;
            currentEnd = currentStart + batchSize;
            QueryForBatchOfRows();
        }

        /// <summary>
        /// Returns the next row of data, and advances the CurrentRow.
        /// This implementation will just return the cached row of data, until it runs out of
        /// cached rows, then it will execute another query to get another batch of rows from
        /// Sql Server.
        /// </summary>
        /// <returns>The raw row data to be transformed.</returns>
        public IList<string> NextRow ()
        {
            // if we have progressed beyond the end of the batchOfRows cache, then we need to
            // query for a new batch. 
            if (currentBatchRowIndex >= batchOfRows.Count) {

                currentBatchRowIndex = 0;   // Reset this.

                // Get the new start, making sure to not go beyond the final row.
                currentStart = Math.Min(currentStart + batchOfRows.Count, maxId);

                // Likewise, get the new end of the batch, again ensuring we don't fall off the
                // end.
                currentEnd = Math.Min(currentStart + batchSize, maxId);
                
                // Eventually, currentStart == currentEnd == maxId.
                if (currentStart == currentEnd) {
                    return null;  // We're done.
                }
                QueryForBatchOfRows();
            }
            
            // grab the current row from the batch, and increment the batch and global row 
            // indices.  
            var row = batchOfRows[currentBatchRowIndex];
            currentBatchRowIndex++;
            currentGlobalRowIndex++;

            return row;
        }

        #endregion

        #region IDisposable implementation
        
        /// <summary>
        /// There are no native resources held between calls by this IParser implementation, 
        /// hence, Dispose() does nothing.
        /// </summary>
        public void Dispose ()
        {
        }

        #endregion

        /// <summary>
        ///  Gets the connection string for Sql Server access, and makes sure the query format
        /// string is available, as well as the batch size for queries.
        /// </summary>
        void ValidateProperties ()
        {
            GetConnectString();

            queryFormatString = GetRequiredProperty("queryFormatString");
            batchSize = int.Parse(GetRequiredProperty("batchSize"));
        }

        /// <summary>
        /// Helper function that looks in the properties set by the Plan for this parser.  This
        /// implementation uses these properties extensively.
        /// </summary>
        /// <returns>The required property value.</returns>
        /// <param name="key">The key for the property value.</param>
        string GetRequiredProperty (string key)
        {
            return Utilities.GetRequiredProperty(key, Properties);
        }

        /// <summary>
        /// Looks for a property in the plan, called 'connectionKey', and uses that key to find
        /// the connection string from the App.Config.
        /// </summary>
        void GetConnectString ()
        {
            var connectKey = GetRequiredProperty("connectionKey");

            connectString = ConfigurationManager.AppSettings [connectKey];
            if (String.IsNullOrEmpty (connectString)) {
                throw new Exception("GetConnectString - SqlDataSource needs a connection " +
                                    "string to do its work");
            }
        }

        /// <summary>
        /// Uses the minIdQuery and maxIdQuery set in the Plan properties to determine the 
        /// Id lower and upper bounds to be used  in calculating how to query for batches of 
        /// rows. 
        /// </summary>
        void GetMinAndMaxIds ()
        {
            var minIdQuery = GetRequiredProperty("minIdQuery");
            var maxIdQuery = GetRequiredProperty("maxIdQuery");
            
            minId = GetIntegerFromSql(minIdQuery, connectString);
            maxId = GetIntegerFromSql(maxIdQuery, connectString);
        }

        /// <summary>
        /// A helper function that returns a scalar integer from Sql Server.
        /// </summary>
        /// <returns>The integer value desired from sql.</returns>
        /// <param name="sqlCommandText">Text of the Sql query to execute.</param>
        /// <param name="connectionString">The connection string for database access.</param>
        static int GetIntegerFromSql(string sqlCommandText, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString)) {
                connection.Open();
                using (var cmd = new SqlCommand(sqlCommandText,connection)) {
                    return (int) cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Queries for a single batch of rows.   This is called whenever the consumer needs
        /// a row which hasn't been cached yet.   This will clear out the cached rows, and
        /// execute the query again, using new new currentStart and currentEnd values.
        ///
        /// The effect of this method is to repopulate the batchOfRows List.
        /// </summary>
        void QueryForBatchOfRows ()
        {
            // Create a new batch of rows if necessary; otherwise, clear the old one out.
            if (batchOfRows == null) {
                batchOfRows = new List<List<String>> ();
            } else {
                batchOfRows.Clear ();
            }

            var rowCount = 0;

            using (var connection = new SqlConnection (connectString)) {
                connection.Open ();

                // The commandString is a String.Format-style format string that comes directly
                // out of the Properties for this Parser (described in the Plan).  The ides is
                // that, given a start and end for the identity id columns for this table, or
                // view, or join, or whatever query is described, it selects out a batch's
                // worth of rows for the parser to cache.
                var commandString = String.Format (queryFormatString, currentStart, 
                                                   currentEnd);

                using (var command = new SqlCommand (commandString, connection)) {
                    var reader = command.ExecuteReader ();
                    var columnCount = reader.FieldCount;
                    var types = new List<Type> ();

                    // We're not making use of the field types right now, but might need to,
                    // for example, to turn tinyint 1's and 0's to "true" and "false".   
                    for (var i = 0; i < columnCount; i++) {
                        types.Add (reader.GetFieldType (i));
                    }

                    while (reader.Read ()) {
                        rowCount++;
                        var row = new List<string> ();
                        var values = new Object[reader.FieldCount];
                        reader.GetValues (values);

                        // It just stuffs all the row values from the IDataReader into the raw
                        // row, and adds to the batchOfRows cache.   Again, if mapping values
                        // from SqlDataTypes to C# types is needed, this would be a good spot.
                        for (var i = 0; i < reader.FieldCount; i++) {
                            var value = values [i];
                            row.Add (value.ToString ());
                        }
                        batchOfRows.Add (row);
                    }
                }
            }
        }
    }
}
