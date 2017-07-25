using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// ILookup is a generalized abstract lookup mechanism.  It can be implemented in a 
    /// multitude of ways, e.g. text file on disk, explicit values in the DataSourcePlan,
    /// as a raw sql query, or a stored procedure, etc.  It's purpose is generally to provide
    /// as set of domain-value lookups for the RowProcessor.
    /// </summary>
    public interface ILookup
    {
        /// <summary>
        /// Gets or sets the Properties for this Lookup.  This is a set of key-value pairs, 
        /// described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation. 
        /// </summary>
        /// <value>The properties.</value>
        IEnumerable<DescriptorProperty> Properties { get; set; }
        
        /// <summary>
        /// This is essentially a two-phase ctor.  The sequence of construction is, instantiate
        /// the lookup, then set its properties, then call Init() so it can migrate the 
        /// property values to their implementation-specific roles.
        /// </summary>
        void Init();

        /// <summary>
        /// Returns the value associated with this key.
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="key">Key.</param>
        string LookupValue (string key);
        
        /// <summary>
        /// Returns true if the Lookup contains the key, false otherwise.
        /// </summary>
        bool ContainsKey(string key);
    }
}
