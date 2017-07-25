using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// Parsers are described with the assembly name (since they are loaded via reflection),
    /// and an array of key-value properties, which are just strings that mean something to the 
    /// parser in question.   It allows the author of the Plan to declaratively provide 
    /// knowledge to the Parser.
    /// </summary>
    [Serializable]
    [XmlRoot ("parser")]
    public class ParserDescriptor : IComponentDescriptor
    {
        /// <summary>
        /// Assembly-qualified name.
        /// </summary>
        /// <value>The assembly.</value>
        [XmlElement ("assembly")]
        public String Assembly { get; set; }

        /// <summary>
        /// A descriptive name for the component.
        /// </summary>
        /// <value>The name.</value>
        [XmlElement("name")]
        public String Name {get; set;}

        /// <summary>
        /// Static properties declared in the plan for this instance of the component.
        /// </summary>
        /// <value>The properties.</value>
        [XmlArray ("properties")]
        [XmlArrayItem ("property")]
        public DescriptorProperty[] Properties { get; set; }
    }
}
