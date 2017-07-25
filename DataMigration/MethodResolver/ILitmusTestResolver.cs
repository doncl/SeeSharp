using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// LitmusTest is a delegate that describes our Litmus Test functions. LitmusTests are 
    /// similar to Transformation Functions, but different in that they are applied before the 
    /// rowProcessor works on the row.  If the row fails, then processing ceases on that row, 
    /// and it's added to the badrows collection without incurring the overhead of a 
    /// BadRowException throw and catch.
    /// </summary>
    /// <param name="lookups">Global ILookup objects..</param>
    /// <param name="existenceObjects">Global IExistence objects.</param>
    /// <param name="arguments">Arguments to the function.</param>
    /// <returns>True if the row is ok for further processing, false otherwise.</returns>
    public delegate bool LitmusTest (IDictionary<String, ILookup> lookups, 
                                     IDictionary<string, IExistence> existenceObjects,
                                     IList<string> arguments);
    /// <summary>
    /// ILitmusTestResolver has a single method, 'ResolveLitmusTest'.   Given the name of a 
    /// litmusTest, it returns the actual delegate.
    /// </summary>
    public interface ILitmusTestResolver : IResolver
    {
        LitmusTest ResolveLitmusTest (string litmusTestName);
    }
}
