using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// Lookups are described with the assembly name (since they are loaded via reflection),
    /// and an array of key-value properties.
    /// </summary>
    [Serializable]
    [XmlRoot ("lookup")]
    public class LookupDescriptor : IComponentDescriptor
    {
        [XmlElement ("assembly")]
        public String Assembly { get; set; }
        
        /// <summary>
        /// Lookups are referenced in TransformMappings by their names.
        /// </summary>
        /// <value>The name.</value>
        [XmlElement("name")]
        public String Name { get; set; }

        /// Aan array of key-value properties, which are just strings that mean something to 
        /// the lookup in question.   It allows the author of the Plan to declaratively provide 
        /// knowledge to the lookup.        
        [XmlArray ("properties")]
        [XmlArrayItem ("property")]
        public DescriptorProperty[] Properties { get; set; }
    }
}
