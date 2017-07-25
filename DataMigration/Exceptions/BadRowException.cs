using System;

namespace DataMigration
{
    /// <summary>
    /// BadRowExceptions are thrown by row processors when they absolutely can do nothing with
    /// a raw row of data.  The data is included, and responsibility for handling it returns
    /// up the stack to the FeedProcessor.
    /// </summary>
    public class BadRowException : Exception
    {
        public int LoadNumber { get; set; }

        public int RowNumber { get; set; }
        
        /// <summary>
        /// The destination column that we were processing when the error ocurred.  Note while
        /// the source column number is probably interesting, it could be multiple source 
        /// columns that caused the problem, but it is always and only a single destination
        /// Column that can be associated with the problem.
        /// </summary>
        public string DestColumn {get; set;}

        /// <summary>
        /// This is a textual explanation of what caused us to reject the row.
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// This is the id, designated for this feed file, that means something to the 
        /// originator of the feed.   
        /// </summary>
        /// <value>The foreign identifier.</value>
        public string ForeignId {get; set;}

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMigration.BadRowException"/> 
        /// class.
        /// </summary>
        /// <param name="reason">An explanation of the reason the row is bad.</param>
        public BadRowException(string reason)
        {
            Reason = reason;
        }
    }
}
