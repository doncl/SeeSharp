using System;
using System.Collections.Generic;
using System.Data;

namespace DataMigration
{
    /// <summary>
    /// IRowProcessor describes the contract of an object that is responsible for transforming
    /// a raw row (as returned from a parser) to a canonical data format as described by the
    /// FeedFilePlan passed into it, AND it is responsible for adding a transformed row to
    /// a DataTable object.
    /// </summary>
    public interface IRowProcessor
    {
        // Each IRowProcessor instance will be loaded with a set of MethodResolvers sufficient
        // to resolve all the transform methods described by its Plan.
        IDictionary<String, IMethodResolver> MethodResolvers { get; set; }

        /// <summary>
        /// The RowProcessor instance will be loaded with a set of LitmusTestResolvers 
        /// sufficient  to resolve all the transform methods described by its Plan.
        /// </summary>
        IDictionary<String, ILitmusTestResolver> LitmusTestResolvers { get; set; }

        /// <summary>
        /// The RowProcessor instance will be loaded with a set of PostRowProcessorResolvers
        /// sufficient to resolve all the PostRowProcessor methods described by the Plan.
        /// </summary>
        IDictionary<String, IPostRowProcessorResolver> PostRowProcessorResolvers {get; set;}
        
        // Each IRowProcessor instance will be loaded with a set of Lookups sufficient to
        // resolve all the lookups described by its Plan.
        IDictionary<String, ILookup> Lookups { get; set; }

        // Each IRowProcessor instance will be loaded with a set of Existence objects 
        // sufficient to resolve all the existence objects needed by the Transform functions
        // specified in its Plan.
        IDictionary<String, IExistence> ExistenceObjects {get; set;}

        /// <summary>
        /// Transforms the row.   This method implements the guts of the actual transformation 
        /// that takes place for each destination row.   The action taken is driven by the
        /// TransformMap, which tells it to either copy a value from a source column, or input 
        /// 1-N source column values into a transformation method, and put the resultant value 
        /// in the dest, or just copy a constant scalar value into the dest.
        /// </summary>
        /// <returns>An IDictionary, where the keys are column names, and the values are the
        /// transformed values for this row.
        /// </returns>
        /// <param name="loadNum">Load number.</param>
        /// <param name="dataSourceCode">The DataSourceCode for this load.</param>
        /// <param name="rowNumber">Row number.</param>
        /// <param name="plan">This is the Plan for the row.  It describes the transform 
        /// mappings.</param>
        /// <param name="row">The array of strings returned for this row from the parser.
        /// </param>
        IDictionary<string, string> TransformRow (int loadNum, string dataSourceCode, 
                                         int rowNumber, FeedFilePlan plan, IList<string> row);

        /// <summary>
        /// Adds the transformed row to a DataTable object passed in.
        /// </summary>
        /// <param name="feedFilePlan">The Plan for this row.</param>
        /// <param name="loadNumber">Load number.</param>
        /// <param name="dataSourceCode">The DataSourceCode.</param>
        /// <param name="transformedRow">The previously transformed row.</param>
        /// <param name="dataTable">The DataTable to add the row data to.</param>
        void AddRowToDataTable (FeedFilePlan feedFilePlan, int loadNumber, 
                                string dataSourceCode, 
                                IDictionary<string, string> transformedRow, 
                                DataTable dataTable);
    }
}
