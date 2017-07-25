using System;

namespace DataMigration
{
    /// <summary>
    /// The ISourceValueMap interface is immplemented by a class when it wants to be processed
    /// by the RowProcessor concrete class.  It describes the way in which source values are
    /// processed by the RowProcessor, e.g., what literal value to use, or which source column
    /// to copy to the destination location, or which 1-N source columns to use as inputs to 
    /// a Transformation Function specified by the XForm and XFormTag properties.  Source 
    /// values can also be indirected through an ILookup object, specified in this interface
    /// by the Lookup's name.
    /// </summary>
    public interface ISourceValueMap : IMethodMap
    {
        /// <summary>
        /// Destination column is the name of the column in the staging destination table that 
        /// this map refers to. 
        ///</summary>
        String DestCol { get; set; }
        
        /// <summary>
        /// When this is set, the intent is just to hardcopy a literal value from the 
        /// map right into the result.
        /// </summary>
        String Value { get; set; }
    }
}
