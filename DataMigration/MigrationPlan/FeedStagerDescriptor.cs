using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// Describes the Assembly (since the FeedStager is loaded by reflection), and a set of
    /// properties, i.e. key-value pairs that are meaningful to this Stager.
    /// </summary>
    [Serializable]
    [XmlRoot ("dataStager")]
    public class FeedStagerDescriptor : IComponentDescriptor
    {
        [XmlElement ("assembly")]
        public String Assembly { get; set; }

        [XmlElement("name")]
        public String Name {get; set;}

        [XmlArray ("properties")]
        [XmlArrayItem ("property")]
        public DescriptorProperty[] Properties { get; set; }
    }
}
