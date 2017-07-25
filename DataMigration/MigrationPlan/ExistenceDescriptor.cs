using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// Existence objects are described by their assembly, name, and a collection of key-value
    /// pair properties.
    /// </summary>
    [Serializable]
    [XmlRoot ("existence")]
    public class ExistenceDescriptor : IComponentDescriptor
    {
        /// <summary>
        /// Existence objects are described with the assembly name (since they are loaded via 
        ///reflection), and an array of key-value properties.
        /// </summary>
        [XmlElement ("assembly")]
        public String Assembly { get; set; }
        
        /// <summary>
        /// Existences are referenced in Transform Functions by their names.
        /// </summary>
        /// <value>The name.</value>
        [XmlElement("name")]
        public String Name { get; set; }

        /// An array of key-value properties, which are just strings that mean something to 
        /// the existence object in question.  It allows the author of the Plan to 
        /// declaratively provide knowledge to the existence object, like (for Sql-based ones)
        /// connection string, and the query to use for populating them, etc.
        [XmlArray ("properties")]
        [XmlArrayItem ("property")]
        public DescriptorProperty[] Properties { get; set; }
    }
}
