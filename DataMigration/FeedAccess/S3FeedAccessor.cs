using System;
using System.Configuration;
using LitS3;
using System.Text.RegularExpressions;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// S3FeedAccess implements IFeedAccess, and manages file movement for feeds deposited on 
    /// S3 locations, with methods for moving, copying and deleting feeds, as well as 
    /// facilitating downloading to the local file system. 
    /// </summary>
    public class S3FeedAccessor : IFeedAccessor
    {
        readonly static ILog log = LogManager.GetLogger (typeof(S3FeedAccessor));

        public String AccessKey { get; set; }

        public String SecretAccessKey { get; set; }

        public S3FeedAccessor()
        {
            var appSettings = ConfigurationManager.AppSettings;
            AccessKey = appSettings ["AWSAccessKey"];
            SecretAccessKey = appSettings ["AWSSecretAccessKey"];
        }

        #region IFeedAccess implementation

        /// <summary>
        /// Given an S3 url, this method ascertains the existence (or lack thereof) of a file
        /// in S3.
        /// </summary>
        /// <returns><c>true</c>, if the file exists, <c>false</c> otherwise.</returns>
        /// <param name="fullS3Url">Full s3 URL.</param>
        public bool Exists (string fullS3Url)
        {
            var s3Service = Utilities.GetS3Service ();
            var parts = S3Parts.FromUrl (fullS3Url);

            var exists = false;

            s3Service.ForEachObject (parts.Bucket, parts.File, 
                delegate(ListEntry le) {
                    exists = true;
                });

            return exists;
        }

        /// <summary>
        /// Given an S3 Url, this method will return the last modified date or the file, or
        /// null if it doesn't exist.
        /// </summary>
        /// <returns>The last modified date</returns>
        /// <param name="fullS3Url">Full s3 URL.</param>
        public DateTime? LastModified (string fullS3Url)
        {
            var s3Service = Utilities.GetS3Service ();
            var parts = S3Parts.FromUrl (fullS3Url);

            if (!Exists (fullS3Url)) {
                return null;
            }

            var req = new GetObjectRequest (s3Service, parts.Bucket, parts.File, true);
            try {
                using (var response = req.GetResponse ()) {
                    return response.LastModified;
                }
            } catch (Exception ex) {
                log.DebugFormat (
                    "LastModified - error when querying file {0}, err = {1}, " +
                    "returning null",
                    fullS3Url, ex.Message);

                return null;
            }
        }

        /// <summary>
        /// Moves one S3Url to another S3Url in the same bucket. The latter S3Url just 
        /// describes a full path to a 'directory' (admittedly, this is a little bit of a
        /// fictional abstraction in the case of S3, but this is the abstraction the interface 
        /// presents to the caller).
        /// </summary>
        /// <param name="oldFile">Old file.</param>
        /// <param name="newFile">New file.</param>
        public void Move (string oldFile, string newFile)
        {
            if (!Exists (oldFile)) {
                throw new ArgumentException (
                    String.Format ("MoveTo - cannot move object {0} to {1}, " +
                    "because source file does not exist",
                        oldFile, newFile));
            }

            var sourceParts = S3Parts.FromUrl (oldFile);
            var destinationParts = S3Parts.FromUrl (newFile);

            if (!sourceParts.Bucket.Equals (destinationParts.Bucket, 
                StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException (
                    String.Format (
                        "MoveTo - cannot move object {0} to {1}, they " +
                        "have different buckets {2} and {3}", 
                        oldFile, newFile, sourceParts.Bucket, destinationParts.Bucket));
            }

            var s3 = Utilities.GetS3Service ();

            try {
                s3.CopyObject (sourceParts.Bucket, sourceParts.File, destinationParts.File);
            } catch (Exception ex) {
                throw new Exception (
                    String.Format (
                        "MoveTo - error copying file {0} to {1}, err = {2}",
                        oldFile, newFile, ex.Message), ex);
            }

            try {
                s3.DeleteObject (sourceParts.Bucket, sourceParts.File);

                log.DebugFormat ("MoveTo - successfully moved file {0} to {1}", 
                    oldFile, newFile);

            } catch (Exception ex) {
                throw new Exception (
                    String.Format ("MoveTo - error deleting source file {0}", 
                        oldFile), ex);
            }
        }

        /// <summary>
        /// Delete the file described by the specified fullS3Url.
        /// </summary>
        /// <param name="fullS3Url">Full s3 URL.</param>
        public void Delete (string fullS3Url)
        {
            var parts = S3Parts.FromUrl (fullS3Url);
            var s3 = Utilities.GetS3Service ();

            try {
                log.DebugFormat ("Delete - attempting to delete file {0}", fullS3Url);
                s3.DeleteObject (parts.Bucket, parts.File);
                log.DebugFormat ("Delete - successfully deleted file {0}", fullS3Url);

            } catch (Exception ex) {
                throw new Exception (
                    String.Format ("Delete - error deleting file {0} = {1}",
                        fullS3Url, ex.Message), ex);
            }
        }
        
        /// <summary>
        /// Given an S3 Url, and a local file path, downloads the file to the prescribed
        /// location.
        /// </summary>
        /// <param name="fullS3Url">Fully described S3 Url.</param>
        /// <param name="localFileLocation">Local file location to download to.</param>
        public void Copy (String fullS3Url, String localFileLocation)
        {
            var exists = Exists (fullS3Url);
            if (!exists) {
                throw new ArgumentException (
                    String.Format ("Copy - cannot copy obj {0} " +
                    "because it doesn't exist",
                        fullS3Url));
            }
            var s3Service = Utilities.GetS3Service ();
            var parts = S3Parts.FromUrl (fullS3Url);

            try {
                log.DebugFormat (
                    "Copy - attempting to copy file {0} to {1}", 
                    fullS3Url, localFileLocation);

                s3Service.GetObject (parts.Bucket, parts.File, localFileLocation);

                log.DebugFormat (
                    "Copy - successfully copy file {0} to {1}",
                    fullS3Url, localFileLocation);

            } catch (Exception ex) {
                throw new Exception (
                    String.Format (
                        "Copy - error copying {0} to {1}, err = {2}",
                        fullS3Url, localFileLocation, ex.Message));
            }
        }
        
        #endregion
    }
}
