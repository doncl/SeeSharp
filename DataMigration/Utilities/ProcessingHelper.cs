using System;
using System.Collections.Generic;

namespace DataMigration
{
    /// <summary>
    /// Processing helper class.  It turns out there is quite a bit of shared logic between the
    /// RowProcessor and FeedProcessor classes.   This class provides services to both.
    /// </summary>
    static public class ProcessingHelper
    {
        const int maxForeignKeyLen = 100;
        const string truncatedMessage = "...(TRUNCATED)...";

        /// <summary>
        /// ProcessLookups examines the map to determine if lookups are being specified.  If 
        /// not, it just returns the values.  If so, it uses the ILookup object specifed, using
        /// the source values as inputs to the lookup, and returning the indirected values.
        /// </summary>
        /// <param name="rowValues">The parsed raw input row.</param>
        /// <param name="map">The map that specifies the lookup to use.</param>
        /// <param name="lookups">The collection of lookup objects.</param>
        /// <returns>A list of indirected value, or (if no lookup), the original values.
        /// </returns>
        static public IList<string> ProcessLookups(IList<string> rowValues, 
                                    IMethodMap map, IDictionary<string, ILookup> lookups)
        {
            if (!String.IsNullOrEmpty(map.Lookup)) {
                if (!lookups.ContainsKey(map.Lookup)) {
                    throw new Exception(
                        String.Format("ProcessLookups - no lookup with name {0}",
                            map.Lookup));
                }

                // Find the appropriate lookup object.
                var lookup = lookups[map.Lookup];
                var lookedUpVals = new List<string>();
                foreach (var rowVal in rowValues) {
                    // Use the value as a key into a lookup, and use the looked-up value.
                    lookedUpVals.Add(lookup.LookupValue(rowVal));
                }
                return lookedUpVals;
            }
            return rowValues;
        }

        /// <summary>
        /// For a given sourcevaluemap, the optional 'srcColumns' attribute describes a comma-
        /// separated list of integer column indices.  This method will look up and return 
        /// those values as a list of strings.
        /// </summary>
        /// <returns>The list of string arguments to the transform method</returns>
        /// <param name="row">Input row data from the parser.</param>
        /// <param name="map">The map that describes this transform.</param>
        /// <param name="lookups">List of lookups.</param>
        static public IList<string> ParseMethodArguments(IList<string> row,
                                    IMethodMap map, IDictionary<string, ILookup> lookups)
        {
            // Parse out list of args for Method.
            IList<string> args = new List<string>();
            if (!String.IsNullOrEmpty(map.SrcColumns)) {
                var tokens = map.SrcColumns.Split(',');
                foreach (var token in tokens) {
                    int iCol;
                    if (!Int32.TryParse(token, out iCol)) {
                        throw new Exception(
                            String.Format("TransformRow - token {0} not parseable as int",
                                token));
                    }
                    if (iCol >= row.Count) {
                        throw new Exception(String.Format("TransformRow - parsed " +
                                          "column arg {0} from config, but data row from " +
                                          "parser is only {1} columns wide", iCol, row.Count));
                    }
                    args.Add(row[iCol].Trim());
                }
            }
            // Process any optional lookups.
            args = ProcessLookups(args, map, lookups);

            // If we've gotten to this point and no arguments have been generated, generate
            // a single empty string.   It can be used by methods like DefaultToUSCountryId()
            // as a sentinal that there's nothing in the feed for a given type of data.
            if (args.Count == 0) {
                args.Add(String.Empty);
            }
            return args;
        }

        /// <summary>
        /// This function is called to calculate destination values.
        /// </summary>
        /// <param name="loadNumber">The LoadNumber.</param>
        /// <param name="rowNumber">The RowNumber.</param>
        /// <param name="row">The raw parsed row data.</param>
        /// <param name="methodResolvers">Method Resolver collection.</param>
        /// <param name="existenceObjects">Existence Objects collection.</param>" 
        /// <param name="lookups">Lookup objects collection.<param name="">/ 
        /// <param name="map">The map that describes how to compute this value.</param>
        /// <param name="values">The computed row so far.</param>
        /// <returns></returns>
        static public string ComputeDestinationValue (int loadNumber, int rowNumber, 
                                       IList<string> row, 
                                       IDictionary<string, IMethodResolver> methodResolvers,
                                       IDictionary<string, IExistence> existenceObjects,
                                       IDictionary<string, ILookup> lookups,
                                       ISourceValueMap map, 
                                       IDictionary<string, string> values)
        {
            var destinationValue = String.Empty;

            // Case 1:  Where there's no transformation function, and we just want to migrate
            // data from the source to the destination (this is by far the most common case).
            if (String.IsNullOrEmpty (map.Method)) {
                if (String.IsNullOrEmpty (map.Value) &&
                    !String.IsNullOrEmpty (map.SrcColumns)) {
                    destinationValue = ObtainSingleValueFromSourceWithNoTransform (row, map, 
                                                                                  lookups);
                } else if (String.IsNullOrEmpty (map.SrcColumns)) {

                    // Case 2:  We just want to take a constant value from the feedmap, and put
                    // it in the destination.

                    if (String.IsNullOrEmpty(map.Value)) {
                        destinationValue = String.Empty; // Null is not permitted.
                    }
                    else {
                        destinationValue = ProcessSingleValueWithLookupsIfSpecified(
                            map.Value.Trim(), map, lookups);
                    }
                }
            } else { 
                // Case 3: A Method tag exists.  This means we want to call a transformation 
                // function to compute a result.  The transformation function can take 1-N 
                // arguments, and those argument values can come out of an ILookup object, 
                // using the source values as keys to the lookup.
                destinationValue = ComputeDestinationValueUsingTransform (loadNumber, 
                                               rowNumber, methodResolvers, existenceObjects, 
                                               lookups, row, map, values);
            }
            return destinationValue;
        }


        /// <summary>
        /// This function is called once for each row.   It attempts to obtain a designated
        /// foreign Id for the row, i.e., the piece of data that will be most reconizable to
        /// the purveyor of the feed that will help them identify a bad row.   It repurposes
        /// the same codepath that does general destination value calculation, so that it can
        /// use transform functions and lookups.
        /// </summary>
        /// <param name="loadNumber">The LoadNumber.</param>
        /// <param name="dataSourceCode">The DataSourceCode for the feed.</param>
        /// <param name="plan">The FeedFilePlan drives the FeedFile integration.</param>
        /// <param name="rowNumber">The numeric index of the row we're processing.</param>
        /// <param name="methodResolvers">Method Resolver collection.</param>
        /// <param name="existenceObjects">Existence Objects collection.</param>" 
        /// <param name="lookups">Lookup objects collection.<param name="">/ 
        /// <param name="row">The raw data for the row.</param>
        /// <returns>ForeignId string.</returns>
        static public string GetForeignId (int loadNumber, string dataSourceCode, 
                        FeedFilePlan plan, int rowNumber, 
                        IDictionary<string, IMethodResolver> methodResolvers,
                        IDictionary<string, IExistence> existenceObjects,
                        IDictionary<string, ILookup> lookups, IList<string> row)
        {
            if (plan.ForeignIdDescriptor == null) {
                throw new Exception (String.Format (
                    "GetForeignId - no foreignId found for DataSourceCode {0}, this is fatal",
                    dataSourceCode));
            }

            // We pass null for the 'row-so-far' argument, since it's meaningless at this
            // point.  This is all before the row calculation has really begun; it's 
            // preparatory in order to generate a foreign id for the bad rows table.
            try {
                var foreignKey =  ComputeDestinationValue(loadNumber, rowNumber, row, 
                    methodResolvers,existenceObjects, lookups, plan.ForeignIdDescriptor, null);


                if (foreignKey.Length > maxForeignKeyLen) {
                    foreignKey = foreignKey.
                            Substring(0, maxForeignKeyLen - truncatedMessage.Length) +
                            truncatedMessage;
                }
                return foreignKey;
            } catch (Exception ex) {
                return String.Format (ex.Message);
            }
        }

        /// <summary>
        /// This function handles the case where a single source column is migrated to the 
        /// destination column.  This is the most common case.
        /// </summary>
        /// <param name="row">The raw data row.</param>
        /// <param name="map">Map that describes how to process the row.</param>
        /// <returns>The migrated destination value.</returns>
        static string ObtainSingleValueFromSourceWithNoTransform (IList<string> row, 
                                   ISourceValueMap map, IDictionary<string, ILookup> lookups)
        {
            int iVal;
            if (!int.TryParse (map.SrcColumns, out iVal)) {
                throw new Exception (
                    String.Format ("ObtainSingleValueFromSourceWithNoTransform - {0} not " +
                    "parseable as int",
                        map.SrcColumns));
            }
            // single value taken from src case
            if (iVal >= row.Count) {
                throw new Exception (
                    String.Format ("ObtainSingleValueFromSourceWithNoTransform - feedmap for " 
                    + "destinatonRow {0} specifies srcCol {1}, but data row is only {2} cells "
                    + "long", map.DestCol, iVal, row.Count));
            }
            var value = row [iVal].Trim ();
            return ProcessSingleValueWithLookupsIfSpecified(value, map, lookups);
        }

        /// <summary>
        /// This is a helper function that enables using lookups with either values from the
        /// source columns, or constants in the feed map as a key.   All processed columns
        /// go through ProcessLookups, one way or another; this path is for the two cases when
        /// no method is specified (constant in feedmap and srcColumns).
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <param name="map">The map for this data item.</param>
        /// <param name="lookups">The global lookups collection.</param>
        /// <returns>A looked up value, if lookup exists for this map, or just returns the
        /// original passed in value, otherwsie.</returns>
        static string ProcessSingleValueWithLookupsIfSpecified(string value,
                                    IMethodMap map, IDictionary<string, ILookup> lookups)
        {
            IList<string> list = new List<string>();

            // ProcessLookups takes a list.
            list.Add(value);
            list = ProcessLookups(list, map, lookups);
            return list[0];
        }

        /// <summary>
        /// This function is called when a transformation function is specified in the map for
        /// this computed data value.  
        /// </summary>
        /// <param name="loadNum">The LoadNumber.</param>
        /// <param name="rowNumber">The numeric index of the row.</param>
        /// <param name="methodResolvers">Method Resolver collection.</param>
        /// <param name="existenceObjects">Existence Objects collection.</param>" 
        /// <param name="lookups">Lookup objects collection.<param name="">/ 
        /// <param name="row">The raw data row.</param>
        /// <param name="map">An ISourceValueMap that describes how to perform the calculation.
        /// </param>
        /// <param name="values">The computed row so far.</param>
        /// <returns>The computed destination value.</returns>
        public static string ComputeDestinationValueUsingTransform (int loadNum, int rowNumber, 
                                       IDictionary<string, IMethodResolver> methodResolvers,
                                       IDictionary<string, IExistence> existenceObjects,
                                       IDictionary<string, ILookup> lookups,
                                       IList<string> row, ISourceValueMap map, 
                                       IDictionary<string, string> values)
        {
            if (String.IsNullOrEmpty (map.MethodTag)) {
                throw new Exception (
                    String.Format ("TransformRow - have method '{0}', but no methodTag",
                        map.Method));
            }
            if (!methodResolvers.ContainsKey (map.MethodTag)) {
                throw new Exception (
                    String.Format ("TransformRow - method tag '{0}' for method '{1}' " +
                    "missing from map of MethodResolvers - config error",
                        map.MethodTag, map.Method));
            }
            var args = ParseMethodArguments (row, map, lookups);
            var method = methodResolvers [map.MethodTag].ResolveTransformationMethod (
                                                                           map.Method);
            try {
                return method (loadNum, lookups, existenceObjects, values, args);
            } catch (BadRowException) {
                throw;
            } catch (Exception ex) {
                throw new Exception (
                    String.Format ("ComputeDestinationValueUsingTransform - transformation " +
                    "function {0} incorrectly threw an error {1} - this is a " +
                    "fatal bug in the transformation function code",
                        map.Method, ex.Message));
            }
        }
    }
}
