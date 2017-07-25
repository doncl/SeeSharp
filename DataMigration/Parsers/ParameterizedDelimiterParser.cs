using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataMigration
{
    /// <summary>
    /// ParameterizedDelimiterParser takes some sort of character property from the 
    /// DataSourcePlan, and uses that to parse the file.   Thus a crude CSV parser (putting 
    /// aside complications inherent in csv) would specify a comma, where a tab-delimited-text
    /// parser specifies a tab.
    /// </summary>
    public class ParameterizedDelimiterParser : IParser
    {
        int row = 0;
        TextReader textReader;
        char delimiter = '\t';

        #region IParser implementation

        /// <summary>
        /// Gets or sets the Properties for this Parser. For the ParameterizedDelimiterParser, 
        /// it's the delimiter.
        /// </summary>
        /// <value>The properties.</value>
        public IEnumerable<DescriptorProperty> Properties { get; set; }
        
        /// <summary>
        /// Gets the current row, zero-relative.
        /// </summary>
        public int CurrentRow { get { return row; } }

        /// <summary>
        /// Open the file the specified Path. The caller is responsible for calling Dispose 
        /// once Open() has been called. This implementation will use the delimiter as 
        /// described in the Plan.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public void Open (string filePath)
        {
            try {
                textReader = File.OpenText (filePath);
            } catch (Exception ex) {
                throw new Exception (String.Format (
                    "Open - error opening {0} = {1}",
                    filePath, ex.Message), ex);
            }

            if (Properties != null) {
                var delimiterProp = Properties.SingleOrDefault (
                                        p => p.Name.Equals ("delimiter"));

                char delimPropVal;
                if (delimiterProp != null && !String.IsNullOrEmpty (delimiterProp.Value)) {
                    if (char.TryParse (delimiterProp.Value, out delimPropVal)) {
                        delimiter = delimPropVal;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the next row of data, and advances the CurrentRow.
        /// </summary>
        /// <returns>The row.</returns>
        public IList<string> NextRow ()
        {
            var line = textReader.ReadLine ();
            if (String.IsNullOrEmpty (line)) {
                return null;
            }
            var returnedRow = line.Split(delimiter);
            row++;
            return returnedRow;
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        ///  Disposes of the textReader member.
        /// </summary>
        public void Dispose ()
        {
            if (textReader != null) {
                textReader.Dispose ();
            }
        }

        #endregion
    }
}
