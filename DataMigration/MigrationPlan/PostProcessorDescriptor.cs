using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// PostProcessors are described with the assembly name (since they are loaded via 
    /// reflection), and an array of key-value properties.
    /// </summary>
    [Serializable]
    [XmlRoot ("postProcessor")]
    public class PostProcessorDescriptor
    {
        /// <summary>
        /// The fully-qualified name of the assembly.
        /// </summary>
        [XmlElement ("assembly")]
        public String Assembly { get; set; }

        /// An array of key-value properties, which are just strings that mean something to the 
        /// PostProcessor.  It allows the author of the Plan to declaratively provide knowledge 
        /// to the PostProcessor.
        [XmlArray ("properties")]
        [XmlArrayItem ("property")]
        public DescriptorProperty[] Properties { get; set; }
    }
}
