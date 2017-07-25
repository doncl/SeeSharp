using System.Collections.Generic;
using System.Linq;

namespace DataMigration
{
    /// <summary>
    /// IStageLogger provides stage logging services to the C# code here.  The RowProcessor
    /// and Migration Engine both log entries.  It can be implemented in any sort of logging
    /// persistence mechanism, but the goal is for it to be a place that PostProcessors can
    /// also log to, so that you get a unified coherent log of the entire process, from start
    /// to end.
    /// </summary>
    public interface IPhaseLogger
    {
        /// <summary>
        /// Gets or sets the connection string for the logger.  Usually derived via properties,
        /// but can be set directly.
        /// </summary>
        /// <value>The connection string.</value>
        string ConnectionString { get; set; }
        /// <summary>
        /// Gets or sets the Properties for this PhaseLogger.  This is a set of key-value 
        /// pairs, described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation. 
        /// </summary>
        /// <value>The properties.</value>
        IEnumerable<DescriptorProperty> Properties { get; set; }    
        
        /// <summary>
        /// Logs the StageLogEntry to the implementation-specific persistence mechanism.
        /// </summary>
        /// <param name="entry">A StageLogEntry with all the information needed to produce
        /// a useful stage log entry.</param>
        void Log(PhaseLogEntry entry);
        
        /// <summary>
        /// Returns the collected entries for an entire load.
        /// </summary>
        /// <returns>The entries.</returns>
        /// <param name="loadNumber">The load number.</param>
        IOrderedEnumerable<PhaseLogEntry> GetLoadEntries(int loadNumber);
    }
}
