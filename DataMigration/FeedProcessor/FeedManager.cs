using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// FeedManager implements IFeedManager, an entity which, given an IFeedAccess instance can 
    /// determine if feed work needs to be done, and move the files from CurrentToArchive,
    /// from Drop to Local, and download to the local file system.
    /// 
    /// FeedManager works in tandem with IFeedAccess to manage this process.
    /// </summary>
    public class FeedManager : IFeedManager
    {
        static readonly ILog log = LogManager.GetLogger (typeof(FeedProcessor));

        #region IFeedManager implementation
        
        public string WorkingDir { get; set; }

        public DataSourcePlan Plan { get; set; }

        public IFeedAccessor FeedAccess { get; set; }

        /// <summary>
        /// Given a DataSourcePlan and an IFeedAccess instance, an IFeedManager is responsible 
        /// for determining if there is work to do.  This works on LastModifiedDates, comparing 
        /// what is in the drop to the current.
        /// </summary>
        /// <returns>true</returns>
        /// <c>false</c>
        public bool NeedsToProcess ()
        {
            var lastModifiedStates = GetLastModifiedStates ();
            if (lastModifiedStates.Select (kvp => kvp.Key)
                .Any (dateTime => dateTime == null)) {
                return false; // This means we're missing a source file.
            }

            // If we have all the drop files, and are missing one in the archive location, then 
            // we should process the feed.
            if (lastModifiedStates.Select (kvp => kvp.Value).
                            Any (dateTime => dateTime == null)) {
                return true;  
            }

            // We now know all files exist in drop and archive.  If any of the files in the 
            // drop directory are newer than the ones in the archive directory, then process 
            // the feed.
            return lastModifiedStates.Any (kvp => kvp.Key > kvp.Value);
        }

        /// <summary>
        /// Moves all feed files from the 'current' subdirectory to the 'archive' location.
        /// <returns>The name of the dated subdirectory under archive.</returns>
        /// </summary>
        public string MoveCurrentToArchive ()
        {
            // This is not an abort on error, because we're just archiving; maybe the current 
            // directory didn't have all the files for some reason.  In any case, it's not a 
            // showstopper.

            // We want to keep feeds around indefinitely, so we're adding a dated subdirectory
            // using the 's' specifier, which means the subdirectories will be sortable by
            // date, e.g. '2009-06-15T13.45.30'.
            var datedSubDirectory = DateTime.UtcNow.ToString("s").Replace(":", ".");
            var archiveSubDirectory = Utilities.UriCombine("archive", datedSubDirectory);
            MoveFeed ("current", archiveSubDirectory, false);
            return datedSubDirectory;
        }

        /// <summary>
        /// Moves all feed files from the 'drop' subfolder to the 'current' location.
        /// </summary>
        public void MoveDropToCurrent ()
        {
            // This is an abort on error because we can't proceed unless all feed files get 
            // moved to the 'current' subdirectory.
            MoveFeed ("drop", "current", true);
        }
        
        /// <summary>
        /// Downloads the feed files in current to the local working directory to process.
        /// Alternatively, (if doDownload == false), it will simply locate the files already in 
        /// the local working directory, and put appropriate FileInfo information
        /// in the DataSourcePlan for all files.
        /// </summary>
        /// <param name="doDownload">If set to <c>true</c> do download.</param>
        public void DownloadCurrentToLocal (bool doDownload = true)
        {
            var location = Plan.FilesLocation;
            var workDir = AssureWorkDirExists ();
                    
            foreach (var plan in Plan.FeedFilePlans) {
                var downloadFile = Path.Combine (workDir, plan.FileName);
                var remoteFile = Utilities.UriCombine (location, "current", plan.FileName);

                if (doDownload) {
                    FeedAccess.Copy (remoteFile, downloadFile);
                    log.InfoFormat ("DonwnloadCurrentToLocal - " +
                    "Successfully downloaded {0} to {1}", 
                        remoteFile, downloadFile);
                }

                plan.LocalFile = new FileInfo (downloadFile);
                if (!plan.LocalFile.Exists) {
                    throw new Exception (
                        String.Format ("DownloadCurrentToLocal - successfully downloaded " +
                        "{0} but it doesn't exist in filesystem", downloadFile));
                }
            }
        }
        
        #endregion

        /// <summary>
        /// Gets the last modifiedDate for each file in the current, and the drop.
        /// </summary>
        /// <returns>The last modified states.</returns>
        IList<KeyValuePair<DateTime?, DateTime?>>  
        GetLastModifiedStates ()
        {
            var fileLocation = Plan.FilesLocation;
            var lastModifiedStates = new List<KeyValuePair<DateTime?, DateTime?>> ();
            var fileNames = Plan.FeedFilePlans.Select (plan => plan.FileName);
            foreach (var fileName in fileNames) {

                var currState = GetLastModified (fileLocation, "current", fileName);
                var dropState = GetLastModified (fileLocation, "drop", fileName);

                lastModifiedStates.Add (
                            new KeyValuePair<DateTime?, DateTime?> (dropState, currState));
            }
            return lastModifiedStates;
        }

        /// <summary>
        /// Gets the last modified date for a single file.
        /// </summary>
        /// <returns>The last modified.</returns>
        /// <param name="fileLocation">File location, i.e. the base bucket.</param>
        /// <param name="subDirectory">Subdirectory the file is located in.</param>
        /// <param name="fileName">Filename.</param>
        private DateTime? GetLastModified (string fileLocation, string subDirectory,
                                            string fileName)
        {
            var fullUrl = Utilities.UriCombine (fileLocation, subDirectory, fileName);
            if (!FeedAccess.Exists (fullUrl)) {
                return null;
            }
            return FeedAccess.LastModified (fullUrl);
        }

        /// <summary>
        /// Moves all files in a particular feed from the specified old dir to new dir.
        /// </summary>
        /// <param name="oldDirectory">Old directory.</param>
        /// <param name="newDirectory">New directory.</param>
        /// <param name="abortOnErr">If set to <c>true</c>throw an exception on error.</param>
        void MoveFeed (string oldDirectory, string newDirectory, bool abortOnErr)
        {
            var location = Plan.FilesLocation;
            var files = Plan.FeedFilePlans.Select (plan => plan.FileName);
            foreach (var file in files) {
                var sourceUrl = Utilities.UriCombine (location, oldDirectory, file);
                var destinationUrl = Utilities.UriCombine (location, newDirectory, file);

                if (FeedAccess.Exists (sourceUrl)) {
                    try {
                        FeedAccess.Move (sourceUrl, destinationUrl);
                    } catch (Exception ex) {
                        var err = String.Format ("MoveFeed - error " +
                                  "moving {0} to {1}, " + "error = {2}", 
                                      sourceUrl, destinationUrl, ex.Message);

                        if (abortOnErr) {
                            throw new Exception (err, ex);
                        }
                        log.Error (err + ", continuing on...");
                    }
                } else {
                    log.WarnFormat ("MoveFeed - cannot move {0} to {1} because" +
                    "src File does not exist", sourceUrl, destinationUrl);
                }
            }
        }

        /// <summary>
        /// Assures the Working Directory exists.  It uses Environment.SpecialFolder.Personal, 
        /// and adds a subdir as described in the Plan.  
        /// </summary>
        /// <returns>The full path of the Working Directory.</returns>
        string AssureWorkDirExists ()
        {
            var path = String.Empty;
            try {
                var basePath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
                path = Path.Combine (basePath, WorkingDir);
                if (!Directory.Exists (path)) {
                    Directory.CreateDirectory (path);
                }
            } catch (Exception ex) {
                throw new Exception (String.Format (
                    "AssureWorkDirExists - error creating workdir at {0}, err = {1}",
                    path, ex.Message), ex);
            }
            return path;
        }
    }
}
