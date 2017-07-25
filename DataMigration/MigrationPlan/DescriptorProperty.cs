using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// This is a generalized key-value pair used for declaratively passing information to
    /// deserialized objects that make use of it.  It's contextual to the object its being
    /// applied to.
    /// </summary>
    [Serializable]
    [XmlRoot ("property")]
    public class DescriptorProperty
    {
        [XmlAttribute ("name")]
        public String Name { get; set; }

        [XmlAttribute ("value")]
        public String Value { get; set; }
    }
}
