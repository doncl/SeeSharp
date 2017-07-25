using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LumenWorks.Framework.IO.Csv;

namespace DataMigration
{
    /// <summary>
    /// Csv parser.  Wraps the LumenWorks.Framework.IO Csv reader, published at 
    /// http://www.codeproject.com/Articles/9258/A-Fast-CSV-Reader.  
    /// </summary>
    public class CsvParser : IParser
    {
        private CsvReader csvReader;
        private int row = 0;
        protected char delimiter = ',';  

        #region IParser implementation

        /// <summary>
        /// Open the file the specified Path. The caller is responsible for calling Dispose 
        /// once Open() has been called.
        /// </summary>
        /// <param name="filePath">File path.</param>
        virtual public void Open(string filePath)
        {
            try {
                GetDelimiter();

                csvReader = new CsvReader(new StreamReader(filePath), false, delimiter);
                csvReader.SupportsMultiline = false;
            }
            catch (Exception ex) {
                throw new Exception(String.Format("Open - error opening file {0} = {1}",
                        filePath, ex.Message), ex);
            }
        }

        /// <summary>
        /// Looks in Properties collection for specified delimiter.
        /// </summary>
        protected virtual void GetDelimiter()
        {
            if (Properties == null) {
                return;
            }
            var delimiterProp = Properties.SingleOrDefault(p => p.Name.Equals("delimiter"));
            if (delimiterProp != null && !String.IsNullOrEmpty(delimiterProp.Value)) {
                char delimPropVal;
                if (char.TryParse(delimiterProp.Value, out delimPropVal)) {
                    delimiter = delimPropVal;
                }
            }
        }

        /// <summary>
        /// Returns the next row of data, and advances the CurrentRow counter.
        /// </summary>
        /// <returns>The row.</returns>
        public IList<string> NextRow()
        {
            var done = !csvReader.ReadNextRecord();
            if (done) {
                return null;
            }
            var returnedRow = new List<String>();
            for (var i = 0; i < csvReader.FieldCount; i++) {
                returnedRow.Add(csvReader[i]);
            }
            row++;
            return returnedRow;
        }

        /// <summary>
        /// Gets or sets the Properties for this Parser. This is a set of key-value pairs, 
        /// described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation.
        /// </summary>
        /// <value>The properties.</value>
        public IEnumerable<DescriptorProperty> Properties { get; set; }

        /// <summary>
        /// Gets the current row, zero-relative.
        /// </summary>
        /// <value>The current row.</value>
        public int CurrentRow { get { return row;} }
        #endregion


        #region IDisposable implementation

        /// <summary>
        /// Releases all resources used by the underlying csvReader object.
        /// </summary>
        public void Dispose()
        {
            csvReader.Dispose();
        }
        #endregion
    }
}

