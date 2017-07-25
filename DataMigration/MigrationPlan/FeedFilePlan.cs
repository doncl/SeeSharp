using System;
using System.IO;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// Feed File Plans are objects that describe the complete plan for first stage processing 
    /// of any particular feed file.   A Feed can consist of multiple files, each with its own 
    /// parser, and set of transformations. 
    /// </summary>
    [Serializable]
    [XmlRoot ("feedFilePlan")]
    public class FeedFilePlan
    {
        [XmlElement ("fileName")]
        public string FileName { get; set; }

        [XmlElement ("parser")]
        public ParserDescriptor Parser { get; set; }

        /// <summary>
        /// It's a common case that the first N lines of a feed file need to be skipped over
        /// before coming to the data.  
        /// </summary>
        /// <value>The integral number of lines to skip.</value>
        [XmlElement ("skipLines", IsNullable = true)]
        public int? SkipLines { get; set; }

        /// <summary>
        /// Contextual information for the FeedProcessor to pass to the FeedStager about the 
        /// desired location to stage the transformed feed.  It varies depending on the type
        /// of the Stager.   The canonical example would be a database/table, e.g.
        /// 'cpms.dbo.StageLoadMember'.
        /// </summary>
        /// <value>The staging location.</value>
        [XmlElement ("stagingLocation")]
        public string StagingLocation { get; set; }

        ///<summary>
        /// Similarly to the StagingLocation, the BadRowsStagingLocation is contextual 
        /// information to pass to the Feed Stager about the desired location to stage the
        /// bad rows encounterd while processing the feed.   Typically, this will be a table
        /// in an RDBMS, but it doesn't have to be.
        ///</summary>
        [XmlElement("badRowsStagingLocation")]
        public string BadRowsStagingLocation {get; set;}

        ///<summary>
        /// Similarly to the StagingLocation and BadRowsStagingLocation, the 
        /// WarnRowsStagingLocation is contextual information to pass to the Feed Stager about 
        /// the desired location to stage the  bad rows encounterd while processing the feed.   
        /// Typically, this will be a table in an RDBMS, but it doesn't have to be.
        ///</summary>
        [XmlElement("warnRowsStagingLocation")]
        public string WarnRowsStagingLocation { get; set; }

        [XmlElement("badRowsForeignId")]
        public BadRowsForeignIdDescriptor ForeignIdDescriptor { get; set; }

        [XmlArray("litmusTests")]
        [XmlArrayItem("litmusTest")]
        public LitmusTestDescriptor[] LitmusTestDescriptors { get; set; }

        // Warning Tests are exactly the same as litmus tests, except that a false result does
        // not cause the row to be thrown out; rather, a copy of it is sent to the WarningRows
        // table, but it is still processed.
        [XmlArray("warningTests")]
        [XmlArrayItem("warningTest")]
        public LitmusTestDescriptor[] WarningTestDescriptors { get; set; }

        // PostProcessors are much like litmus tests, in that they describe a function that 
        // gets called, but they do not return anything, or perform any test.  Rather, they
        // describe a method that gets called on successful processing of a row.  
        [XmlArray("postRowProcessors")]
        [XmlArrayItem("postRowProcessor")]
        public PostRowProcessorDescriptor[] PostRowProcessorDescriptors {get; set;}

        /// <summary>
        /// The collection of transform maps, which describe:
        /// a)  The destination columns to populate with data.
        /// b)  The sources (1-N) to for that data.
        /// c)  An optional transformation function to scrub/transform the data with.
        /// 
        /// N.B. Sources can be multiple columns only if there is an appropriate xform function 
        /// to take the multiple inputs, and coalesce to a single output.  Destination columns 
        /// always and only can take a single value.
        /// </summary>
        /// <value>The transform maps.</value>
        [XmlArray ("transformMap")]
        [XmlArrayItem ("mapping")]
        public TransformMapping[] TransformMaps { get; set; }

        /// <summary>
        /// This is not declaratively described by the plan at all; it's procedurally populated 
        /// at runtime with the local file location of the feed file.   The intent is to bundle 
        /// this necessary knowledge along with the declarative knowledge needed to process the
        /// file.
        /// </summary>
        /// <value>The local file.</value>
        [XmlIgnore]
        public FileInfo LocalFile { get; set; }
    }
}
