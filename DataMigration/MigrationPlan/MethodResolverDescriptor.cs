using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// Describes the assembly name (as MethodResolvers are loaded by reflection) and tag for a 
    /// particular MethodResolver.   TransformMaps refer to transform methods with an 
    /// 'methodTag' attribute, which matches this Tag, so the RowProcessor knows which 
    /// MethodResolver to use for resolving the transformation method reference.
    /// </summary>
    [Serializable]
    [XmlRoot ("methodResolver")]
    public class MethodResolverDescriptor
    {
        [XmlElement ("tag")]
        public String Tag { get; set; }

        [XmlElement ("assembly")]
        public String Assembly { get; set; }
    }
}
