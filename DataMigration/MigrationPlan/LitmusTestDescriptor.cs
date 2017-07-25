using System;
using System.Xml.Serialization;

namespace DataMigration
{
    [Serializable]
    [XmlRoot("litmusTest")]
    public class LitmusTestDescriptor : IMethodMap
    {
        /// <summary>
        /// SrcColumns is a comma-separated list of integers which refer to input data columns,
        /// zero-relative.
        /// </summary>
        [XmlAttribute ("srcColumns")]
        public String SrcColumns { get; set; }        
        
        /// <summary>
        /// The lookup denotes which lookup object, if any, is to be used.
        /// </summary>
        /// <value>The lookup tag.</value>
        [XmlAttribute ("lookup")]
        public String Lookup { get; set; }        
        
        /// <summary>
        /// Method is the name of the method that needs to be applied to the srcColumns values,
        /// to generate the foreign id.
        /// </summary>
        [XmlAttribute ("method")]
        public String Method { get; set; }        
   
        /// <summary>
        /// MethodTag is a short string that is an identifier denoting which method resolver is
        /// used by this method.  If Method is non-empty, then this needs to be set.
        /// </summary>
        [XmlAttribute ("methodTag")]
        public String MethodTag { get; set; }

        /// <summary>
        /// This is the criterion for the test.  If the row fails the test, this describes what
        /// the test was looking for, so it can go in the BadRowsTable as a reason.
        /// </summary>
        /// <value>The reason.</value>
        [XmlAttribute("reason")]
        public String Reason {get; set;}
    }
}
