using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace DataMigration
{
    /// <summary>
    /// This copies the BadRows for a given integration loadnumber up to S3.
    /// </summary>
    public class BadRowsOutboundFeed : IPostProcessor
    {
        public IEnumerable<DescriptorProperty> Properties {get; set;}
        public IPhaseLogger PhaseLogger {get; set;}

        // We'll use '3' for logging phases.
        const int logSource = 3;

        // SQL connection string.
        string connectionString;

        // We will write out log messages ever N rows, as expressed by 'logQuantum'.
        const int logQuantum = 10000;

        // This is where we'll place the new feed files, before pushing them up to S3.
        string workingDirectory;

        // This is the name of the bad rows file we're exporting.
        const string badRowsFileName = "BadRows.txt";
        
        public void PostProcess(int loadNumber, DataSourcePlan dataSourcePlan) 
        {
            connectionString = Utilities.GetConfigString("connectionKey", Properties);

            workingDirectory = Path.GetTempPath();
            var guidString = Guid.NewGuid().ToString();
            guidString = guidString.Substring(guidString.Length - 12);
            workingDirectory = Path.Combine(workingDirectory, guidString);

            Directory.CreateDirectory(workingDirectory);
            var utcNow = DateTime.UtcNow;

            var badRowsFile = Utilities.WriteBadRowsFeedFile(loadNumber,
                                            DataMigrationPhase.OutboundBadRowsFileWritten,
                                            dataSourcePlan, "Bad Rows Feed File written",
                                            badRowsFileName, workingDirectory,
                                            connectionString, logQuantum, PhaseLogger,
                                            logSource);

            try {
                var s3Location = Utilities.UriCombine(dataSourcePlan.FilesLocation,
                            "outbound", utcNow.ToString("s"), loadNumber.ToString());

                Utilities.CopyToS3(s3Location, badRowsFile, badRowsFileName, loadNumber,
                    DataMigrationPhase.OutboundBadRowsFileUploaded, dataSourcePlan,
                    "Bad Rows Feed File uploaded to S3",
                    PhaseLogger,
                    logSource);
            } finally {
                Utilities.DeleteLocalFeedFile(badRowsFile);
                Utilities.DeleteWorkingDirectory(workingDirectory);
            }
        }
    }
}

