using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// IFeedProcess describes the contract for an entity that can Process a DataSource.  Each 
    /// DataSource will map to a FeedProcessor instance, and this is described by the 
    /// MigrationPlan.  Each FeedProcessor instance gets its own DataSourcePlan, which 
    /// describes the set and location of feed files its responsible, and with instructions
    /// for processing them. 
    /// </summary>
    public interface IFeedProcessor
    {
        /// <summary>
        /// The ready-to-use subplan is a collection of components that can be consumed; if
        /// specified, then the datasource can get away without literally specifying the
        /// stager, feedmanager, feedaccessor, and phaselogger explicitly.  
        /// </summary>
        /// <value>The ready to use sub plan.</value>
        IReadyToUseSubPlan ReadyToUseSubPlan {get; set;}

        /// <summary>
        /// The PhaseLogger is a device for the FeedProcessor to use to log actions taken.
        /// </summary>
        IPhaseLogger PhaseLogger { get; set; }
        
        /// <summary>
        /// The MonitoringPublisher is the facility for publishing data to be monitored by
        /// operations.
        /// </summary>
        IPublishMonitoringData MonitoringPublisher {get; set;}

        /// <summary>
        /// The DataMigrationPlan consists of multiple DataSourcePlans, which correspond to 
        /// each DataSourceCode that can be processed.  Each DataSourcePlan describes to the
        /// FeedProcessor the list of FeedFilePlans which it needs to process.
        /// </summary>
        DataSourcePlan Plan { get; set; }

        // Each IFeedProcessor instance will be loaded with a set of MethodResolvers sufficient
        // to resolve all the transform methods described by its Plan.
        IDictionary<string, IMethodResolver> MethodResolvers { get; set; }
        
        // Each IFeedProcessor instance will be loaded with a set of LitmusTestResolver objects 
        // sufficient to resolve all the litmusTest methods as specified in its Plan.
        IDictionary<string, ILitmusTestResolver> LitmusTestResolvers {get; set;}

        // The IFeedProcessor instance will be loaded with a set of Lookups sufficient to
        // resolve all the lookups described by the litmus tests in its FeedFilePlans.
        IDictionary<string, ILookup> Lookups { get; set; }

        // The FeedProcessor instance will be loaded with a set of Existence objects 
        // sufficient to resolve all the existence objects needed by the Litmus test functions
        // specified in its FeedFilePlans.
        IDictionary<string, IExistence> ExistenceObjects { get; set; }

        /// <summary>
        /// The RowProcessor is used by the FeedProcessor to transform each raw row coming
        /// back from the parser into the desired destination format.
        /// </summary>
        IRowProcessor RowProcessor { get; set; }

        /// <summary>
        /// A feed processor has N post processors, which are very domain-specific.  Normally,
        /// there will just be one post-processor (e.g., pushing from staging table into 
        /// Camelot member table), but there could be more (i.e., pushing into Redis).
        /// </summary>
        /// <value>The post processors.</value>
        IList<IPostProcessor> PostProcessors { get; set; }
        
        ///<summary>
        /// Process is the first stage of processing, where feed files are loaded, parsed,
        /// transformed, and staged.
        ///</summary>
        /// <returns>A load number that identifies the staged data.</returns>
        int Process ();
        
        /// <summary>
        /// PostProcess is the final stage of processing, where business rules are applied
        // against the data placed in staging by the Process() endpoint, and resultant rows
        // inserted, modified, and/or deleted at the final location.
        /// <param name="loadNum">The load number to perform post processing on.</param>
        /// <param name="readyToUseSubPlan">A ReadyToUseSubPlan (can be null) that may
        /// have additional PostProcessors to be run first.</param>
        /// </summary>
        void PostProcess (int loadNum, IReadyToUseSubPlan readyToUseSubPlan);
    }
}
