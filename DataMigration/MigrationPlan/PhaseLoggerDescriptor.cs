using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// Describes the assembly name (as PhaseLoggers are loaded by reflection) for a particular 
    /// PhaseLogger, and properties to load it with on instantiation.
    /// </summary>
    [Serializable]
    [XmlRoot ("phaseLogger")]
    public class PhaseLoggerDescriptor : IComponentDescriptor
    {
        /// <summary>
        /// The fully assembly-qualified name of the class that implements this.
        /// </summary>
        /// <value>The assembly.</value>
        [XmlElement ("assembly")]
        public String Assembly { get; set; }

        /// <summary>
        /// The name for this phase logger in the plan.
        /// </summary>
        [XmlElement("name")]
        public String Name { get; set; }
        
        /// <summary>
        /// Arbitrary static properties for this phase logger in the plan.
        /// </summary>
        [XmlArray ("properties")]
        [XmlArrayItem ("property")]
        public DescriptorProperty[] Properties { get; set; }
    }
}