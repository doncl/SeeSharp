using System;

namespace DataMigration
{
    /// <summary>
    /// This interface describes the contract for an object whose responsibilities entail the 
    /// movement and downloading of feed files.  It could point to some sort of remote store, 
    /// or locally (although in the latter case, the 'DownloadTo' method would simply be a 
    /// local file system copy). 
    /// </summary>
    public interface IFeedAccessor
    {
        /// <summary>
        /// Given a full path, this method determines whether or not a feed 'file' exists.
        /// </summary>
        /// <returns><c>true</c>, if the file exists, <c>false</c> otherwise.</returns>
        /// <param name="fullFilePath">Full file path</param>
        bool Exists (string fullFilePath);

        /// <summary>
        /// Given a file path, this method will return the last modified date or the file, or
        /// null if it doesn't exist.
        /// </summary>
        /// <returns>The last modified date</returns>
        /// <param name="fileName">Full path to file</param>
        DateTime? LastModified (string fileName);

        /// <summary>
        /// Moves a file to another location. 
        /// </summary>
        /// <param name="oldFile">Old file.</param>
        /// <param name="newfile">New file.</param>
        void Move (string oldFile, string newfile);

        /// <summary>
        /// Deletes the specified file at the full path.
        /// </summary>
        /// <param name="fullFilePath">The path to the file.</param>
        void Delete (String fullFilePath);

        /// <summary>
        /// Given a feed file path, and a local file path, downloads the file to the prescribed
        /// location.
        /// </summary>
        /// <param name="fullPath">Fully described Path.</param>
        /// <param name="localFileLocation">Local file location to download to.</param>
        void Copy (String fullPath, String localFileLocation);
    }
}
