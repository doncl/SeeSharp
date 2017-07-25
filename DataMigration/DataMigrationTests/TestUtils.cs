using System;
using System.IO;

namespace DataMigrationTests
{
    /// <summary>
    /// TestUtils is for shareable utilities for this project.
    /// </summary>
    public class TestUtils
    {
        /// <summary>
        /// Returns embedded resource text file.
        /// </summary>
        /// <returns>A string, with the contents of the file.</returns>
        /// <param name="filename">The name of the resource.</param>
        public string GetResourceTextFile (string filename)
        {
            string result;
            var rsrcName = "DataMigrationTests.resources." + filename;

            try {
                using (var stream = this.GetType ().Assembly.
                    GetManifestResourceStream (rsrcName)) {
                    using (var streamReader = new StreamReader (stream)) {
                        result = streamReader.ReadToEnd ();
                    }
                }
            } catch (Exception ex) {
                throw new Exception (String.Format (
                    "GetResourceTextFile - error loading rsrc {0}, err = {1}, stack = {2}",
                    filename, ex.Message, ex.StackTrace), ex);
            }
            return result;
        }
    }
}
