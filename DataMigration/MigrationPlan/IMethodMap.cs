using System;

namespace DataMigration
{
    /// <summary>
    /// This is the fundamental interface that ISourceValueMap derives from.
    /// There is a need for LitmusTests, Transformation Functions, and ForeignId generation to
    /// share the same code, hence this interface hierarchy.  This is the more stripped-down
    /// interface, which is implemented by LitmusTestDescriptor, and which is sufficient for
    /// the code that parses out arguments and looks for Method names using tags, to do its
    /// work.  ISourceValueMap has more fields, and TransformationMaps and 
    /// BadRowsForeignIdDesciptors implement it.
    /// </summary>
    public interface IMethodMap
    {
       /// <summary>
        /// SrcColumns is a comma-separated list of integers which refer to input data columns,
        /// zero-relative.
        /// </summary>
        String SrcColumns { get; set; }

        /// <summary>
        /// Method is the name of the method that needs to be applied to the srcColumns values,
        /// to calculate a result.
        /// </summary>
        String Method { get; set; }
        
        /// <summary>
        /// MethodTag is a short string that is an identifier denoting which method resolver is
        /// used by this Method.  If Method is non-empty, then this needs to be set.
        /// </summary>
        String MethodTag { get; set; }        
                
        /// <summary>
        /// The Lookup property denotes which, if any, lookup object is to be used.
        /// </summary>
        String Lookup { get; set; }
    }
}
