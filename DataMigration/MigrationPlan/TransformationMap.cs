using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// TransformMapping is a class that describes the computation we perform to produce a 
    /// single destination row value (i.e. the value that goes in column N of the destination).
    /// </summary>
    [Serializable]
    [XmlRoot ("mapping")]
    public class TransformMapping : ISourceValueMap
    {
        /// <summary>
        /// SrcColumns is a comma-separated list of integers which refer to input data columns,
        /// zero-relative.  It's normally a comma-separated string   
        /// </summary>
        [XmlAttribute ("srcColumns")]
        public String SrcColumns { get; set; }
        
        /// <summary>
        /// Method is the name of the method that needs to be applied to the srcColumns values,
        /// and the result place in the DestCol.
        /// </summary>
        [XmlAttribute ("method")]
        public String Method { get; set; }

        /// <summary>
        /// MethodTag is a short string that is an identifier denoting which method resolver is
        /// used by this Method.  If Method is non-empty, then this needs to be set.
        /// </summary>
        [XmlAttribute ("methodTag")]
        public String MethodTag { get; set; }
        
        /// <summary>
        /// The lookup denotes which lookup object, if any, is to be used.
        /// </summary>
        /// <value>The lookup tag.</value>
        [XmlAttribute ("lookup")]
        public String Lookup { get; set; }

        /// <summary>
        /// Destination column is the name of the column in the staging destination table that 
        /// this map refers to. 
        ///</summary>
        [XmlAttribute ("destCol")]
        public String DestCol { get; set; }

        /// <summary>
        /// When this is set, the intent is just to hardcopy a literal value from the 
        /// TransformMap right into the destination table.
        /// </summary>
        [XmlAttribute ("value")]
        public String Value { get; set; }

        /// <summary>
        ///  The Plan author should specify the type of data, int, long, bool, or float.  If 
        /// left empty, the system assumes a string type.
        /// </summary>
        [XmlAttribute ("type")]
        public String Type { get; set; }
    }
}
