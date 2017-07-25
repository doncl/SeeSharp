using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// A base class for existence objects that can use a C# HashSet<string> for their 
    /// implementation.  Child classes usually just implement Init() to populate the 
    /// HashSet, or they can leave it empty; plan-specific existence objects can use this 
    /// mechanism to implement a first-writer-wins de-duping strategry on feed data items.
    /// </summary>
    public abstract class AbstractBaseExistence : IExistence
    {
        /// <summary>
        /// The Values HashSet is used to implement this Exists object.
        /// </summary>
        protected HashSet<string> Values = 
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        #region IExistence implementation

        /// <summary>
        /// Gets or sets the Properties for this IExistence object. This is a set of key-value 
        /// pairs, described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation.
        /// </summary>
        /// <value>The properties.</value>
        public IEnumerable<DescriptorProperty> Properties { get; set; }

        /// <summary>
        /// This is essentially a two-phase ctor. The sequence of construction is, instantiate
        /// the object, then set its properties, then call Init() so it can migrate the 
        /// property values to their implementation-specific roles.  In this abstract class,
        /// this method is not implemented (by design).  It's up to concrete child classes to
        /// implement this, depending on their context.
        /// </summary>
        public virtual void Init()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a single value to the IExistence collection.
        /// </summary>
        /// <param name="value">Value.</param>
        public void Add(string value)
        {
            Values.Add(value);
        }

        /// <summary>
        /// Determines if an external value exists in the collection.
        /// </summary>
        /// <returns>True if the value exists, false otherwise.</returns>
        /// <param name="value">Value to check existence for.</param>
        public bool Exists(string value)
        {
            return Values.Contains(value);
        }
        #endregion
    }
}
