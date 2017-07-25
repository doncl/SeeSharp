using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// A base class for lookups that can use a C# Dictionary object for their implementation.
    /// Child classes usually just implement Init() to populate the Dictionary.
    /// </summary>
    public abstract class AbstractBaseLookup : ILookup
    {
        /// <summary>
        /// The lookup values used to implement ILookup.
        /// </summary>
        protected Dictionary<string, string> LookupValues = 
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        #region ILookup implementation
        virtual public IEnumerable<DescriptorProperty> Properties { get; set; }

        /// <summary>
        /// This is essentially a two-phase ctor. The sequence of construction is, instantiate
        /// the lookup, then set its properties, then call Init() so it can migrate the 
        /// property values to their implementation-specific roles.
        /// In this abstract class, it is unimplemented, and must be provided by child classes.
        /// </summary>
        virtual public void Init()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the value associated with this key.
        /// </summary>
        /// <returns>The value.</returns>
        /// <param name="key">Key.</param>
        public string LookupValue(string key)
        {
            if (!LookupValues.ContainsKey(key)) {
                throw new BadRowException(
                    String.Format("LookupValue - key {0} not found in lookup",
                        key));
            }
            return LookupValues[key];
        }

        /// <summary>
        /// Returns true if the Lookup contains the key, false otherwise.
        /// </summary>
        public bool ContainsKey(string key)
        {
            return LookupValues.ContainsKey(key);
        }

        #endregion
    }
}
