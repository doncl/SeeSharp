using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// This describes code that runs after a row successfully processes; it generates some 
    /// situationally desired side-effect.
    /// </summary>
    [Serializable]
    [XmlRoot("postRowProcessor")]    
    public class PostRowProcessorDescriptor : IMethodMap
    {
        #region IMethodMap implementation

        [XmlAttribute("srcColumns")]
        public string SrcColumns { get; set;}

        [XmlAttribute("method")]
        public string Method { get; set;}

        [XmlAttribute("methodTag")]
        public string MethodTag { get; set; }

        [XmlAttribute("lookup")]
        public string Lookup { get; set;}
        #endregion
    }
}

