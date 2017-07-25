using System;
using System.IO;

namespace DataMigration
{
    /// <summary>
    /// This is used for non-file-based data sources, it does no actual work, but just allows
    /// the MigrationEngine to go directly to using the Parser to do work.
    /// </summary>
    public class NonFileBasedFeedManager : IFeedManager
    {

        #region IFeedManager implementation

        /// <summary>
        /// Given a DataSourcePlan and an IFeedAccess instance, an IFeedManager is responsible 
        /// for determining if there is work to do.
        /// In this case, we simply want to always return true, because there are no physical
        /// files to manipulate, and we always want to do the work.
        /// </summary>
        /// <c>false</c>
        public bool NeedsToProcess()
        {
            return true;
        }

        /// <summary>
        /// In this implementation, there are no files to move, hence it does nothing.
        /// <returns>Normally, The name of the dated subdirectory under archive.
        /// In this case, it just returns String.Empty;
        /// </returns>       
        /// </summary>
        public string MoveCurrentToArchive()
        {
            return String.Empty;
        }

        /// <summary>
        /// This implementation does nothing, since there are no files to move.
        /// </summary>
        public void MoveDropToCurrent()
        {
        }

        // The FeedProcessor needs a FileInfo, which it will use to pass a file name to the
        // parser.
        public void DownloadCurrentToLocal(bool doDownload = true)
        {
            foreach (var plan in Plan.FeedFilePlans) {
                plan.LocalFile = new FileInfo("NonexistentFile");
            }
        }

        /// <summary>
        /// This property is meaningless in the context of this implementation.
        /// </summary>
        /// <value>The working dir.</value>
        public string WorkingDir { get; set; }

        /// <summary>
        /// This implementation will make no usage of the DataSourcePlan
        /// </summary>
        /// <value>The DataSourcePlan.</value>
        public DataSourcePlan Plan { get; set; }

        /// <summary>
        /// This implementation will make no usage of the FeedAccess instance; it is always
        /// null.
        /// </summary>
        /// <value>The FeedAccess instance.</value>
        public IFeedAccessor FeedAccess { get; set; }

        #endregion

    }
}
