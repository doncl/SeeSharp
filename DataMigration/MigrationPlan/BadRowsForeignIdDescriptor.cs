using System;
using System.Xml.Serialization;

namespace DataMigration 
{
    /// <summary>
    ///  The BadRowsForeignIdDescriptor tells the row processor how to acquire a foreign Id.
    /// </summary>
    [Serializable]
    [XmlRoot ("badRowsForeignId")]
    public class BadRowsForeignIdDescriptor : ISourceValueMap
    {
        /// <summary>
        /// SrcColumns is a comma-separated list of integers which refer to input data columns,
        /// zero-relative.
        /// </summary>
        [XmlAttribute ("srcColumns")]
        public String SrcColumns { get; set; }

        /// <summary>
        /// Method is the name of the method that needs to be applied to the srcColumns values,
        /// to generate the foreign id.
        /// </summary>
        [XmlAttribute ("method")]
        public String Method { get; set; }        
   
        /// <summary>
        /// methodTag is a short string that is an identifier denoting which method resolver is
        /// used by this method.  If method is non-empty, then this needs to be set.
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
        /// this descriptor will need to use to generate the foreign id.
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
        /// The ID length is constrained; the default implementation stores rows in a 
        /// BadRows table, which has a finite length for the foreign id.  If we're unable to
        /// obtain the foreign id, we'll store the error in the foreignId column of the 
        /// BadRows table, and we need to not exceed its length.
        /// </summary>
        /// <value>The length.</value>
        [XmlAttribute("length")]
        public int Length {get; set;}
    }
}
