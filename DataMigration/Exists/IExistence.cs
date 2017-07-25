using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// IExistence is used to model a data structure such that the caller can determine if 
    /// entries exist, in a performant way. The canonical usage of this would be a list of 
    /// items that starts with a list of things from Sql, and then gets added to as we go.
    /// </summary>
    public interface IExistence
    {
        /// <summary>
        /// Gets or sets the Properties for this IExistence object.  This is a set of key-value 
        /// pairs, described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation. 
        /// </summary>
        /// <value>The properties.</value>
        IEnumerable<DescriptorProperty> Properties { get; set; }
        
        /// <summary>
        /// This is essentially a two-phase ctor.  The sequence of construction is, instantiate
        /// the object, then set its properties, then call Init() so it can migrate the 
        /// property values to their implementation-specific roles.
        /// </summary>
        void Init();

        /// <summary>
        /// Adds a single value to the IExistence collection.
        /// </summary>
        /// <param name="value">Value.</param>
        void Add(string value);

        /// <summary>
        /// Determines if an external value exists in the collection.
        /// </summary>
        /// <returns>True if the value exists, false otherwise.</returns>
        /// <param name="value">Value to check existence for.</param>
        bool Exists(string value);
    }
}
