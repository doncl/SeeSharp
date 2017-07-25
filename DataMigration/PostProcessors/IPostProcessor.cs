using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// IPostProcessor defines the contract for an object that knows how to postprocess
    /// staged feeds.
    /// </summary>
    public interface IPostProcessor
    {
        /// <summary>
        /// Gets or sets the Properties for this PostProcessor.  This is a set of key-value 
        /// pairs, described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation. 
        /// </summary>
        /// <value>The properties.</value>
        IEnumerable<DescriptorProperty> Properties { get; set; }
        
        /// <summary>
        /// This is set by the Feed processor, and is a way for any C# code in the 
        /// postprocessor to use the same logging mechanism as the FeedProcessor is using.
        /// </summary>
        IPhaseLogger PhaseLogger {get; set;}
        
        /// <summary>
        /// The main entry point for this postprocessor.
        /// </summary>
        /// <param name="loadNumber">Load number.</param>
        /// <param name="dataSourcePlan">The DataSourcePlan for this datasource.</param>
        void PostProcess(int loadNumber, DataSourcePlan dataSourcePlan);
    }
}
