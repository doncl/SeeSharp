using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataMigration;
using NUnit.Framework;
using log4net;

namespace DataMigrationTests
{
    /// <summary>
    /// Feed manager tests.   This test verifies the Feed Manager is capable of doing its work, 
    /// moving feed files around and downloading them, when given a FeedAccess object.
    /// </summary>
    [TestFixture]
    public class FeedManagerTests
    {
        static public string BaseDir = Path.GetTempPath ();
        string workingDirectory;
        List<string> feedFiles;
        readonly TestFeedAccess feedAccess = new TestFeedAccess ();

        /// <summary>
        /// Sets up the TestFeedAccess object with its directory paths, and instantiates the 
        /// workingDirectory member.
        /// </summary>
        [TestFixtureSetUp]
        public void SetupForAllTests ()
        {
            feedAccess.DropDir = CreateFeedDir (BaseDir, "drop");
            feedAccess.CurrDir = CreateFeedDir (BaseDir, "current");
            feedAccess.ArchiveDir = CreateFeedDir (BaseDir, "archive");

            workingDirectory = Path.Combine (Path.GetTempPath (), "workingDirectory");
        }

        private string CreateFeedDir (string baseDir, string suffix)
        {
            var fullDirName = Path.Combine (baseDir, suffix);
            Directory.CreateDirectory (fullDirName);
            return fullDirName;
        }

        /// <summary>
        /// Deletes all the temporary created directories and their contents. 
        /// </summary>
        [TestFixtureTearDown]
        public void TearDownAllTests ()
        {
            feedAccess.Cleanup ();
            Directory.Delete (workingDirectory, true);
        }

        /// <summary>
        /// This test verifies the feed manager is capable of using an IFeedAccess object to
        /// determine correctly that work is available, and can move the feed files correctly
        /// from drop to current to archive, and can download them locally for working on.
        /// </summary>
        [Test]
        public void TestFeedManager ()
        {
            feedFiles = new List<String> {
                "feedFile1.txt",
                "feedFile2.txt",
            };

            CreateDropAndCurrFiles (feedFiles);

            // Make FeedFilePlan objects.
            var feedPlan = feedFiles.Select (s => new FeedFilePlan{ FileName = s }).ToArray ();

            // Make the overall plan.
            var plan = new DataSourcePlan {
                FilesLocation = BaseDir,
                FeedFilePlans = feedPlan,                    
            };

            var feedManager = new FeedManager {
                FeedAccess = feedAccess,
                WorkingDir = workingDirectory,
                Plan = plan,
            };

            // Verify files are in drop and current as we expect, and none in archive.
            VerifyInitialState (BaseDir, feedFiles);

            // Verify that the feed manager believes we have work to do.
            Assert.IsTrue (feedManager.NeedsToProcess ());

            // Move the current to archive.
            var datedSubDirectory = feedManager.MoveCurrentToArchive ();
            var fullPathArchive = Path.Combine(feedAccess.ArchiveDir, datedSubDirectory);

            // Verify files exist in the archive, and not in the current.
            VerifyFeedFileExistenceOrNonExistence(fullPathArchive, feedFiles, true);
            VerifyFeedFileExistenceOrNonExistence (feedAccess.CurrDir, feedFiles, false);

            // Move the drop to the current.
            feedManager.MoveDropToCurrent ();

            // Verify that there are no files in the drop, and there are files in current
            // and archive.
            VerifyFeedFileExistenceOrNonExistence (feedAccess.DropDir, feedFiles, false);
            VerifyFeedFileExistenceOrNonExistence (feedAccess.CurrDir, feedFiles, true);
            VerifyFeedFileExistenceOrNonExistence (fullPathArchive, feedFiles, true);

            // Do the 'download', which in this context is simply a local file copy.
            feedManager.DownloadCurrentToLocal (true);

            // Verify the 'downloaded' files exists.
            VerifyFeedFileExistenceOrNonExistence (Path.Combine (workingDirectory), feedFiles,
                true);
        }

        /// <summary>
        /// Verify, before we start up, that we have set the stage correctly.  There should be 
        /// feed files in the drop and current, and none in the archive.
        /// </summary>
        /// <param name="baseDir">Base directory.</param>
        /// <param name="feedFileNames">Feed file names.</param>
        void VerifyInitialState (string baseDir, List<string> feedFileNames)
        {
            VerifyFeedFileExistenceOrNonExistence (feedAccess.DropDir, feedFileNames, true);
            VerifyFeedFileExistenceOrNonExistence (feedAccess.CurrDir, feedFileNames, true);
            VerifyFeedFileExistenceOrNonExistence (feedAccess.ArchiveDir, feedFileNames, false);
        }

        /// <summary>
        /// Verifies the feed file existence or non existence.
        /// </summary>
        /// <param name="baseDir">Base directory.</param>
        /// <param name="fileNames">List of file names.</param>
        /// <param name="exists">If true, enforce existence, otherwise, nonexistence.</param>
        void VerifyFeedFileExistenceOrNonExistence (string baseDir, IList<string> fileNames,
                                                    bool exists)
        {
            foreach (var feedFile in fileNames) {
                var fullPath = Path.Combine (baseDir, feedFile);
                if (exists) {
                    Assert.IsTrue(File.Exists(fullPath));
                } else {
                    Assert.IsFalse(File.Exists(fullPath));
                }
            }
        }

        /// <summary>
        /// Creates the drop and curr feed files.
        /// </summary>
        /// <param name="feedFileNames">Feed file names.</param>
        void CreateDropAndCurrFiles (IList<string> feedFileNames)
        {
            foreach (var feedFile in feedFileNames) {
                CreateTestFeedFile (Path.Combine (feedAccess.CurrDir, feedFile));
            }

            // sleep for a few ms. so that drop files will be newer than curr files.
            System.Threading.Thread.Sleep (1000);

            foreach (var feedFile in feedFileNames) {
                CreateTestFeedFile (Path.Combine (feedAccess.DropDir, feedFile));
            }
        }

        /// <summary>
        /// Creates the test feed file.
        /// </summary>
        /// <param name="fullPath">Full path of file to create.</param>
        private void CreateTestFeedFile (string fullPath)
        {
            using (var streamWriter = File.CreateText (fullPath)) {
                streamWriter.WriteLine ("A line of text");
            }
        }
    }

    /// <summary>
    /// Scaffolding/mock class that implements IFeedAccess so that FeedManager can be exercised 
    /// by unit tests.   It just uses local File IO methods to meet its IFeedAccess contract.
    /// </summary>
    sealed class TestFeedAccess : IFeedAccessor
    {
        static ILog log = LogManager.GetLogger (typeof(TestFeedAccess));
        // Drop, current and archive dir strings are set here, because its the responsibility
        // of this class to delete these.
        public string DropDir { get; set; }

        public string CurrDir { get; set; }

        public string ArchiveDir { get; set; }

        #region IFeedAccess implementation

        /// <summary>
        /// Returns true if file exists, otherwise false.
        /// 
        public bool Exists (string fileName)
        {
            return File.Exists (fileName);
        }

        /// <summary>
        /// Returns the last modified time of the file.
        /// </summary>
        public DateTime? LastModified (string fileName)
        {
            var fi = new FileInfo (fileName);
            return fi.LastWriteTime;
        }

        /// <summary>
        /// Given a full path to a file, and a new directory location, moves the file, removing 
        /// it from the source location.
        /// </summary>
        public void Move (string oldFile, string newDir)
        {
            var fullPath = newDir;
            newDir = Path.GetDirectoryName(newDir);
            if (!Directory.Exists(newDir)) {
                Directory.CreateDirectory(newDir);
            }
            File.Move (oldFile, fullPath);
        }

        /// <summary>
        /// Delete the specified file.
        /// </summary>
        public void Delete (string fileName)
        {
            File.Delete (fileName);
        }

        /// <summary>
        /// Downloads the file specified at fullPath to the location specified.
        /// </summary>
        /// <param name="fullPath">Full path to original file.</param>
        /// <param name="localFileLocation">Local file location.</param>
        public void Copy (string fullPath, string localFileLocation)
        {
            File.Copy (fullPath, localFileLocation);
        }

        #endregion

        /// <summary>
        /// Remove the archive, current, and drop directories, and all their contents.
        /// </summary>
        [TestFixtureTearDown]
        public void Cleanup ()
        {
            CleanupDir (ArchiveDir);
            CleanupDir (CurrDir);
            CleanupDir (DropDir);
        }

        /// <summary>
        /// Removes a directory and contents.  It creates a log entry on failure, but continues
        /// on, since this is reasonably benign.
        /// </summary>
        /// <param name="dir">Directory to cleanup.</param>
        private void CleanupDir (string dir)
        {
            try {
                Directory.Delete (dir, true);
            } catch (Exception ex) {
                log.ErrorFormat ("CleanupDir - error deleting dir {0}, err = {1}, stack= {2}" +
                ", continuing on...",
                    dir, ex.Message, ex.StackTrace);
            }
        }
    }
}
