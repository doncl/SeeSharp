using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace DataMigration
{
    /// <summary>
    /// Xml document parser.  Crude, dead-simple, puts-the-entire-file-in-memory parser. Do
    /// not use for feeds of any size or complexity.  There are lots of features that can be
    /// added to this one for small feeds that will be added on an in-demand basis, e.g. 
    /// xslt transformations as a property, change ProcessingHelper so it can use xPath 
    /// fragments as value selectors, etc., blah, blah.   None of that for now.   
    /// </summary>
    public class XmlDocParser : IParser
    {
        XmlDocument doc;
        int rowIndex = 0;
        string rowSelectorXPath;
        List<List<string>> rows;

        #region IParser implementation

        /// <summary>
        /// Open the file at the specified Path. Applies the row selector xPath to get out
        /// 'rows', (e.g. xPath elements). 
        /// </summary>
        /// <param name="filePath">File path.</param>
        public void Open(string filePath)
        {
            doc = new XmlDocument();
            doc.Load(filePath);
            rows = new List<List<string>>();

            rowSelectorXPath = Utilities.GetRequiredProperty("rowSelectorXPath", Properties);

            // TODO: If we ever need it, implement XmlNamespaceFactory jazz.
            var nodeList = doc.SelectNodes(rowSelectorXPath);
            foreach (var rowNode in nodeList) {
                var rowEle = rowNode as XmlElement;
                var row = rowEle.ChildNodes.OfType<XmlElement>()
                                .Select(childEle => childEle.InnerText).ToList();
                rows.Add(row);
            }
        }

        /// <summary>
        /// Returns the next row of data, and advances the CurrentRow.
        /// </summary>
        /// <returns>The row.</returns>
        public IList<string> NextRow()
        {
            if (rowIndex == rows.Count) {
                return null;
            }
            return rows[rowIndex++];
        }

        /// <summary>
        /// Gets or sets the Properties for this Parser. This is a set of key-value pairs, 
        /// described in the DataSourcePlan, that have scalar values specific to this 
        /// implementation.  This parser requires a 'rowSelectorXPath' property.
        /// </summary>
        /// <value>The properties.</value>
        public IEnumerable<DescriptorProperty> Properties { get; set; }

        /// <summary>
        /// Gets the current row, zero-relative.
        /// </summary>
        /// <value>The current row.</value>
        public int CurrentRow {
            get {
                return rowIndex;
            }
        }
        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Does nothing; this implementation has no native resources to release.
        /// </summary>
        public void Dispose()
        {
            // Do nothing.                
        }
        #endregion
    }
}

