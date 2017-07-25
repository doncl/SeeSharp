using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// PostRowProcessor is a delegate that describes methods that get called subsequent to the
    /// successfull processing of a row; they provide some desired side-effect.
    /// </summary>
    /// <param name="loadNum">The load number.</param>
    /// <param name="lookups">The global collection of ILookup objects.</param>
    /// <param name="existenceObjects">The global collection of IExistence objects.</param>
    /// <param name="rowInProgress">The destination row we're building up. </param>
    /// <returns>A string for use in the destination column.</returns> ///
    public delegate void PostRowProcessor(int loadnum, IDictionary<String, ILookup> lookups,
        IDictionary<string, IExistence> existenceObjects,
        IDictionary<string, string> rowInProgress, 
        IList<string> args);

    /// <summary>
    /// IPostRowProcessorResolver has a single method, 'ResolvePostRowProcessor'.   Given the 
    /// name of a PostRowProcessor, it returns the actual delegate.
    /// </summary>
    public interface IPostRowProcessorResolver : IResolver
    {
        PostRowProcessor ResolvePostRowProcessor (string postRowProcessorName);
    }
}
