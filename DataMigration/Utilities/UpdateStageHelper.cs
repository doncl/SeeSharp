using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace DataMigration
{
    /// <summary>
    /// This class is for managing updating the staging table with member and memberToProduct
    /// ids in an efficient manner.
    /// </summary>
    public class UpdateStageHelper
    {
        static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(typeof(UpdateStageHelper));

        /// <summary>
        /// Helper that manages the arithmetic for walking through a finite list, batch by
        /// batch, using them all, without going off the end.   It's written this way, as a 
        /// public static method so it's easily unit testable.
        /// </summary>
        /// <param name="loadNumber">The load number.</param>
        /// <param name="items">The collection of items.</param>
        /// <param name="batchSize">The size of the batch.</param>
        /// <param name="action">A delegate which processes each batch.</param>
        /// <returns>Number of successfully updated records.</returns>
        static public int WalkThroughListBatchByBatch<T>(int loadNumber,
            IList<T> items, int batchSize,
            Func<int, IList<T>, int, bool> action) where T : struct
        {
            var successfullyUpdated = 0;

            for (var i = 0; i < items.Count; i += batchSize) {
                var start = i;

                if (start >= items.Count) {
                    break;
                }
                var end = Math.Min(start + batchSize - 1, items.Count - 1);
                if (start > end) {
                    break;
                }

                var batchOfItems = items
                    .Where((item, index) => index >= start && index <= end).ToList();

                if (action(loadNumber, batchOfItems, batchSize)) {
                    successfullyUpdated += batchSize;
                }
            }
            return successfullyUpdated;
        }

        /// <summary>
        /// Updates a single batch of ids in staging table.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="commandText">Fully formed update command.</param>
        /// <param name="attempt">Attempt number.</param>
        /// <returns>true, on success, false on failure.</returns>
        public static bool UpdateBatchHelper(string connectionString, string commandText, 
                                            int attempt)
        {
            try { 
                using (var connection = new SqlConnection(connectionString)) {
                    connection.Open();
                    using (var command = new SqlCommand(commandText, connection)) {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) {
                log.ErrorFormat("Error attempting to update staging table with success, " +
                                 "attempt number {0}, error = {1}",
                                 attempt, ex.Message);

                return false;
            }
            return true;
        }

    }
}

