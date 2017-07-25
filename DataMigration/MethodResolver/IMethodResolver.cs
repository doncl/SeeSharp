using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// TransformMethod is a delegate that describes our transformation functions.
    /// </summary>
    /// <param name="loadNum">The load number.</param>
    /// <param name="lookups">The global collection of ILookup objects.</param>
    /// <param name="existenceObjects">The global collection of IExistence objects.</param>
    /// <param name="rowInProgress">The destination row we're building up. </param>
    /// <returns>A string for use in the destination column.</returns> ///
    public delegate string TransformMethod (int loadNum, IDictionary<String, ILookup> lookups, 
                                  IDictionary<string, IExistence> existenceObjects,
                                  IDictionary<string, string> rowInProgress, 
                                  IList<string> args);
    /// <summary>
    /// IMethodResolver has a single method, 'ResolveTransformationMethod'.   Given the name of 
    /// a method, it returns the actual delegate.
    /// </summary>
    public interface IMethodResolver : IResolver
    {
        TransformMethod ResolveTransformationMethod (string xformName);
    }
                            
}
