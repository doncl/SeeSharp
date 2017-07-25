namespace DataMigration
{
    /// <summary>
    /// IFeedManager describes the contract for an entity which, given an IFeedAccess instance
    /// can determine if feed work needs to be done, and move the files from CurrentToArchive,
    /// from Drop to Local, and download to the local file system.
    /// 
    /// IFeedManager works in tandem with IFeedAccess to manage this process.
    /// </summary>
    public interface IFeedManager
    {
        ///
        /// <summary>
        /// The local filesystem location where work will be performed.
        ///</summary>
        string WorkingDir { get; set; }
        
        /// <summary>
        /// Gets or sets the plan.  The DataSourcePlan describes the list of feed files and
        /// their base location.  The FeedManager will infer the canonical sub-locations (i.e.
        /// 'drop', 'current', and 'archive'.   
        /// </summary>
        DataSourcePlan Plan { get; set; }

        /// <summary>
        /// Gets or sets as IFeedAccess instance which is used to actually manipulate the feed 
        /// files.  
        /// </summary>
        /// <value>The FeedAccess instance.</value>
        IFeedAccessor FeedAccess { get; set; }

        /// <summary>
        /// Given a DataSourcePlan and an IFeedAccess instance, an IFeedManager is responsible 
        /// for determining if there is work to do.
        /// </summary>
        /// <returns><c>true</c>if we have work to do,<c>false</c> otherwise.</returns>
        bool NeedsToProcess ();

        /// <summary>
        /// Moves all feed files from the 'current' subfolder to the 'archive' location.
        /// <returns>The name of the dated subdirectory under archive.</returns>
        /// </summary>
        string MoveCurrentToArchive ();

        /// <summary>
        /// Moves all feed files from the 'drop' subfolder to the 'current' location.
        /// </summary>
        void MoveDropToCurrent ();

        /// <summary>
        /// Downloads the feed files in current to the local working directory to process.
        /// Alternatively, (if doDownload == false), it will simply locate the files already
        /// in the local working directory, and put appropriate FileInfo information in the 
        /// DataSourcePlan for all files. 
        /// </summary>
        /// <param name="doDownload">If set to <c>true</c> do download.</param>
        void DownloadCurrentToLocal (bool doDownload = true);
    }
}
