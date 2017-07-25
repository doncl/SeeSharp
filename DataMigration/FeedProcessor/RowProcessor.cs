using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DataMigration
{
    /// <summary>
    /// A RowProcessor object is responsible for transforming a single row of raw data as
    /// returned from a parser, into something suitable for inserting into our staging 
    /// location.  It also is responsible for adding one of these transformed rows to a 
    /// DataTable.
    /// </summary>
    public class RowProcessor : IRowProcessor
    {
        #region IRowProcessor implementation
        
        /// <summary>
        /// The RowProcessor instance will be loaded with a set of MethodResolvers sufficient
        /// to resolve all the transform methods described by its Plan.
        /// </summary>
        public IDictionary<String, IMethodResolver> MethodResolvers { get; set; }

        /// <summary>
        /// The RowProcessor instance will be loaded with a set of LitmusTestResolvers 
        /// sufficient  to resolve all the transform methods described by its Plan.
        /// </summary>
        public IDictionary<String, ILitmusTestResolver> LitmusTestResolvers { get; set; }

        /// <summary>
        /// The RowProcessor instance will be loaded with a set of PostRowProcessorResolvers
        /// sufficient  to resolve all the PostRowProcessor methods described by its Plan.
        /// </summary>
        public IDictionary<String, IPostRowProcessorResolver> 
        PostRowProcessorResolvers {get; set;}

        /// <summary>
        /// The RowProcessor instance will be loaded with a set of Lookups sufficient to
        /// resolve all the lookups described by its Plan.
        /// </summary>
        public IDictionary<String, ILookup> Lookups { get; set; }

        /// <summary>
        /// The RowProcessor instance will be loaded with a set of Existence objects 
        /// sufficient to resolve all the existence objects needed by the Transform functions
        /// specified in its Plan.
        /// </summary>
        public IDictionary<String, IExistence> ExistenceObjects {get; set;}

        /// <summary>
        /// Transforms the row.   This method implements the guts of the actual transformation 
        /// that takes place for each destination row.   The action taken is driven by the
        /// TransformMap, which tells it to either copy a value from a source column, or
        /// input 1-N source column values into a transformation method, and put the resultant
        /// value in the dest, or just copy a constant scalar value into the dest.
        /// </summary>
        /// <returns>An IDictionary, where the keys are column names, and the values are the
        /// transformed values for this row.
        /// </returns>
        /// <param name="loadNumber">Load number.</param>
        /// <param name="dataSourceCode">The DataSourceCode</param>
        /// <param name="rowNumber">Row number.</param>
        /// <param name="plan">This is the Plan for the row.  It describes the transform 
        /// mappings.</param>
        /// <param name="row">The array of strings returned for this row from the parser.
        /// </param>
        public IDictionary<string, string> TransformRow (int loadNumber, string dataSourceCode, 
                                                        int rowNumber, FeedFilePlan plan, 
                                                        IList<string> row)
        {
            var values = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);

            var destinationColumn = String.Empty;
            try {
                foreach (var map in plan.TransformMaps) {
                    destinationColumn = map.DestCol;
                    var destVal = ProcessingHelper.ComputeDestinationValue (loadNumber, 
                                                        rowNumber, row, MethodResolvers, 
                                                        ExistenceObjects, Lookups, map, 
                                                        values);

                    values.Add (destinationColumn, destVal);
                }
            } catch (BadRowException badRowException) {

                // Add the ForeignId to the badRowException, so that auditors of the BadRows
                // datastore can relate this bad row back to what (for example) account it was.
                badRowException.DestColumn = destinationColumn;
                badRowException.LoadNumber = loadNumber;
                throw;
            }
            return values;
        }

        /// <summary>
        /// Adds the transformed row to a DataTable object passed in.
        /// </summary>
        /// <param name="feedFilePlan">The Plan for this row.</param>
        /// <param name="loadNumber">Load number.</param>
        /// <param name="dataSourceCode">The DataSourceCode.</param>
        /// <param name="transformedRow">The previously transformed row.</param>
        /// <param name="dataTable">The DataTable to add the row data to.</param>
        public void AddRowToDataTable (FeedFilePlan feedFilePlan, int loadNumber, 
                        string dataSourceCode, IDictionary<string, string> transformedRow, 
                        DataTable dataTable)
        {
            var dataRow = dataTable.NewRow ();
            dataRow [FeedProcessor.LoadNumberString] = loadNumber;
            dataRow [FeedProcessor.DataSourceCodeString] = dataSourceCode;

            if (transformedRow.Count != feedFilePlan.TransformMaps.Length) {
                throw new Exception (
                    String.Format ("AddRowToDataTable - mismatched mappings " +
                    "count {0} to row count {1}",
                        feedFilePlan.TransformMaps.Length,
                        transformedRow.Count));
            }

            foreach (var map in feedFilePlan.TransformMaps) {
                var val = transformedRow[map.DestCol];
                val = val == null ? null : val.Trim();
                if (String.IsNullOrEmpty (val) || 
                    val.Equals("NULL", StringComparison.OrdinalIgnoreCase)) {
                    dataRow [map.DestCol] = DBNull.Value;
                    continue;
                }
                switch (map.Type) {
                case "int":
                    int iVal;
                    if (!int.TryParse (val, out iVal)) {
                        throw new Exception (
                            String.Format ("Value {0} not parseable as int",
                                val));
                    }
                    dataRow [map.DestCol] = iVal;
                    break;
                case "datetime":
                    DateTime dateTime;
                    if (!DateTime.TryParse(val, out dateTime)) {
                        throw new Exception(
                            String.Format("Value {0} not parseable as datetime",
                                val));
                    }
                    break;
                case "long":
                    long lVal;
                    if (!long.TryParse (val, out lVal)) {
                        throw new Exception (
                            String.Format ("Value {0} not parseable as long",
                                val));
                    }
                    dataRow [map.DestCol] = lVal;
                    break;
                case "bool":
                    dataRow [map.DestCol] = ParseBooleanValue(val);
                    break;
                case "float":
                    float fVal;
                    if (!float.TryParse (val, out fVal)) {
                        throw new Exception (
                            String.Format ("Value {0} not parseable as float",
                                val));
                    }
                    dataRow [map.DestCol] = fVal;
                    break;
                case "double":
                    double dVal;
                    if (!double.TryParse (val, out dVal)) {
                        throw new Exception (
                            String.Format ("Value {0} not parseable as double",
                                val));
                    }
                    dataRow [map.DestCol] = dVal;
                    break;
                default:    
                    dataRow [map.DestCol] = val;  // string
                    break;
                }
            }
            dataTable.Rows.Add (dataRow);
        }

        static string[] boolValueSynonyms = {"Yes", "No", "y", "n", "0", "1"};

        /// <summary>
        /// Helper function that tries to figure out all the ways someone might express a bool
        /// in a feed cell.
        /// </summary>
        /// <param name="val">String value for boolean.</param>
        /// <returns>True or False, if able to parse it; exception otherwise.</returns>
        private static bool ParseBooleanValue(string val)
        {
            var errMsg = String.Format("Value {0} not parseable as bool", val);

            bool boolVal;
            if (bool.TryParse(val, out boolVal)) {
                return boolVal;
            }

            if (!boolValueSynonyms.Contains(val, StringComparer.OrdinalIgnoreCase)) 
                throw new Exception(errMsg);

            return val.StartsWith("Y", StringComparison.OrdinalIgnoreCase) || 
                   val.Equals("1");
        }

        #endregion
        
    }
}
