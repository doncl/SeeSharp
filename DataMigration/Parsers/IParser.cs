using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// IParser's are a unifed way to physically access data feed.  Disparate physical formats
    /// require different IParser implementations.
    /// </summary>
    public interface IParser : IDisposable
    {
        /// <summary>
        /// Gets or sets the Properties for this Parser.  This is a set of key-value pairs, 
        /// described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation. 
        /// </summary>
        /// <value>The properties.</value>
        IEnumerable<DescriptorProperty> Properties { get; set; }

        /// <summary>
        /// Gets the current row, zero-relative.
        /// </summary>
        /// <value>The current row.</value>
        int CurrentRow { get; }

        /// <summary>
        /// Open the file the specified Path.  The caller is responsible for calling Dispose 
        /// once Open() has been called.
        /// </summary>
        /// <param name="filePath">File path.</param>
        void Open (string filePath);

        /// <summary>
        /// Returns the next row of data, and advances the CurrentRow.
        /// </summary>
        /// <returns>The row.</returns>
        IList<string> NextRow ();
    }
}
