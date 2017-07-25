using System;
using System.Collections.Generic;
using System.Data;

namespace DataMigration
{
    /// <summary>
    /// IFeedStager describes a contract such that implementing entities take responsiblity for 
    /// staging a DataTable input to some location (typically persistent, although this is not 
    /// an absolute requirement).  
    /// </summary>
    public interface IFeedStager
    {
        /// <summary>
        /// Gets or sets the Properties for this FeedStager.
        /// This is a set of key-value pairs, described in the DataSourcePlan, that have scalar 
        /// values specific to this implementation. 
        /// </summary>
        /// <value>The properties.</value>
        IEnumerable<DescriptorProperty> Properties { get; set; }

        /// <summary>
        /// Gets or sets the location.  This is a string that has meaning to the FeedStager,
        /// e.g., for the SqlDataStager, it's the sql database and table where the staging 
        /// results will be persisted.
        /// </summary>
        String Location { get; set; }

        /// <summary>
        /// Gets or sets the BadRowsLocation.  This is a string that has meaning to the 
        /// FeedStager,
        /// e.g., fo the SqlDataStager, it's the sql database and table where the bad rows 
        /// collected will be persisted.
        /// </summary>
        string BadRowsLocation { get; set; }

        /// <summary>
        /// Gets or sets the WarnRowsLocation.  This is a string that has meaning to the 
        /// FeedStager,
        /// e.g., for the SqlDataStager, it's the sql database and table where the warn rows 
        /// collected will be persisted.
        /// </summary>
        string WarnRowsLocation { get; set; } 

        /// <summary>
        /// This is the number of rows at a time the FeedStager will need to process.  This
        /// should be set before calling BulkLoad(), and the DataTable passed to BulkLoad()
        /// should have this many rows in it.
        /// </summary>
        /// <value>The size of the batch.</value>
        int  BatchSize { get; set; }

        /// <summary>
        /// Gets or sets the seconds timeout.
        /// </summary>
        /// <value>The seconds timeout.</value>
        int  SecondsTimeout { get; set; }
        
        /// <summary>
        /// Gets the load number.  This is used by the DataProcessor when building the 
        /// DataTable that gets passed to this FeedStager in BulkLoad, and is used to
        /// disambiguate one load from another in the staged results.
        /// </summary>
        /// <returns>The load number.</returns>
        int GetLoadNumber ();

        /// <summary>
        /// This entry point loads the data in the datatable argument into staging.
        /// It also records the bad rows.
        /// </summary>
        /// <param name="dataTable">DataTable with valid data.</param>
        /// <param name="badRowsDataTable">DataTable with bad rows in it.</param>        
        /// <param name="warnRowsDataTable">DataTable with warning rows in it.</param>     
        void BulkLoad (DataTable dataTable, DataTable badRowsDataTable, 
                       DataTable warnRowsDataTable);
    }
}
