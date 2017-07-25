using System;

namespace DataMigration
{
    /// <summary>
    /// DataMigrationPhases are essentially numeric ids that reflect the lifecycle phases of a
    /// feed's processing.  Values 1-1000 are reserved for the 1st stage of processing, which 
    /// is done by this C# code, and values above 1000 are reserved for use by the 
    /// postprocessors, which have to have the self-contained knowledge as to how to write to
    /// the datastore in question.
    /// </summary>
    public enum DataMigrationPhase 
    {
        AcquiringResources = 1,
        ManagingExternalFeeds = 10,
        ProcessingRows = 20,
        CompletedBulkCopy = 30,
        PostProcessorUsersInserted = 1000,
        PostProcessorUserStagingUpdated = 2000,
        PostProcessorSubscriptionsInserted = 3000,
        PostProcessorSubscriptionStagingUpdated = 4000,
        PostProcessorSubscriptionFailuresStagingUpdated = 5000,
        OutboundImportStatusFileWritten = 30000,
        OutboundBadRowsFileWritten = 30010,
        OutboundPhaseLogFileWritten = 30020,
        OutboundImportStatusFileUploaded = 30100,
        OutboundBadRowsFileUploaded = 30200,
        OutboundPhaseLogFileUploaded = 30300,
   }
    
    /// <summary>
    /// PhaseLogEntry is a class that encapsulates all the data we're writing in Phase Logging
    /// PhaseLogging is an activity that is done by the DataMigration tool to cope with the 
    /// fact that phases of the process may be done by C#, but other phases (e.g. Sql
    /// Server - based PostProcessing modules) that will not be able to write to log4net.
    /// This will be consumed by IPhaseLogWriter implementations as a unified way to show what
    /// happened on a given run of the DataMigration tool.  
    /// </summary>
    public class PhaseLogEntry
    {
        // DataSourceCodes, by definition, are 30 characters max.
        const int maxmimumDataSourceCodeCharacterLength = 30;
        
        string dataSourceCode;

        /// <summary>
        /// Gets or sets the log source.  The MigrationEngine is 1, and PostProcessors can use
        /// anything else. 
        /// </summary>
        /// <value>The log source.</value>
        public int LogSource { get; set; }
        
        public String DataSourceCode {
            get {
                return dataSourceCode;
            }
            set {
                if (!String.IsNullOrEmpty(value) &&
                    value.Length > maxmimumDataSourceCodeCharacterLength) {
                    throw new ArgumentException(
                        String.Format("Value {0} exceeds DataSourceCode maximum Length of {1}",
                            value, maxmimumDataSourceCodeCharacterLength));
                }
                dataSourceCode = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the load number.  We permit it to be null, so this class can be used
        /// with phases of the DataMigration process before a loadnumber is obtained.
        /// </summary>
        /// <value>The load number.</value>
        public int? LoadNumber {get; set;}
        
        /// <summary>
        /// The Sql post-processing stored procedures have license to log phases which don't
        /// exist as one of our hardcoded enumerations (The enum really only exists as 
        /// syntactic sugar to document the C# part of the process).  We keep the two fields
        /// in parallel so we can load something into the object when querying sql for it.
        /// </summary>
        public int PhaseNumber {get; set;}
        DataMigrationPhase phase;
        
        /// <summary>
        /// The DataMigrationPhase describes which phase of the process logged the entry.
        /// </summary>
        /// <value>The phase.</value>
        public DataMigrationPhase Phase {
            get {
                return phase;
            } 
            set {
                PhaseNumber = (int) value;
                phase = value;
            }
        }
        
        /// <summary>
        /// When possible, logging entities will attempt to describe how many records were 
        /// affected by the phase, but it is nullable, as this will not always make sense, in
        /// context.
        /// </summary>
        /// <value>The number of records.</value>
        public int? NumberOfRecords {get; set;}
        
        /// <summary>
        /// A textual description of the logged event.
        /// </summary>
        /// <value>The description.</value>
        public String Description {get; set;}
    }
}
