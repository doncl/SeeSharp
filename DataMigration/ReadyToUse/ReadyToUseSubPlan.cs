using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// These are the software equivalent of MRE's, i.e. 'Ready to Eat' packages of components
    /// that a datasource can consume for its use, rather than having to configure them.  The
    /// idea is to achieve convention-over-configuration by establishing a set of these things
    /// in the master plan, each with its own tag, and datasourceplans can just specify which
    /// one they want to use by specifying the tag, rather than declaring everything 
    /// explicitly.
    /// </summary>
    public class ReadyToUseSubPlan : IReadyToUseSubPlan
    {
        #region IReadyToUseSubPlan implementation
        /// <summary>
        /// The Feed stager in use.
        /// </summary>
        /// <value>The stager.</value>
        public IFeedStager Stager { get; set; }

        /// <summary>
        /// The Feed Manager in use.
        /// </summary>
        /// <value>The feed manager.</value>
        public IFeedManager FeedManager { get; set; }

        /// <summary>
        /// The feed Accessor in use.
        /// </summary>
        /// <value>The feed accessor.</value>
        public IFeedAccessor FeedAccessor { get; set; }

        /// <summary>
        /// The phase logger this subPlan uses.
        /// </summary>
        /// <value>The phase logger.</value>
        public IPhaseLogger PhaseLogger { get; set; }

        /// <summary>
        /// A description of the parser to use for each feedFile.  This gets reinstantiated
        /// for each file (since parsers are IDisposables), so we just store the descriptor
        /// here.
        /// </summary>
        public ParserDescriptor ParserDescriptor { get; set; }

        /// <summary>
        /// The collection of PostProcessors to use for this DataSource.
        /// </summary>
        public IEnumerable<PostProcessorDescriptor> PostProcessorDescriptors { get; set; }
        #endregion
    }
}
