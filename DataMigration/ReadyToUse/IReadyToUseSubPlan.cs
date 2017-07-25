using System.Collections;
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
    public interface IReadyToUseSubPlan
    {
        /// <summary>
        /// The Feed stager in use.
        /// </summary>
        IFeedStager Stager { get; set;}

        /// <summary>
        /// The Feed Manager in use.
        /// </summary>
        IFeedManager FeedManager {get; set;}

        /// <summary>
        /// The feed Accessor in use.
        /// </summary>
        IFeedAccessor FeedAccessor {get; set;}

        /// <summary>
        /// The phase logger this subPlan uses.
        /// </summary>
        IPhaseLogger PhaseLogger {get; set;}

        /// <summary>
        /// A description of the parser to use for each feedFile.  This gets reinstantiated
        /// for each file (since parsers are IDisposables), so we just store the descriptor
        /// here.
        /// </summary>
        ParserDescriptor ParserDescriptor { get; set; }

        /// <summary>
        /// The collection of postprocessors for use with processing this datasource.
        /// </summary>
        IEnumerable<PostProcessorDescriptor> PostProcessorDescriptors { get; set; }
    }
}
