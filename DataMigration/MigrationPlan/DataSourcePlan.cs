using System;
using System.Configuration;
using System.Reflection;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// The master DataMigrationPlan has N DataSourcePlans.  There is 1 DataSourcePlan per 
    /// DataSource.   DataSource's are a way of partitioning the feed space; typically, each
    /// source of data (i.e. each separate entity providing a feed) gets their own datasource
    /// code.   However, it is possible to partition a single feed source entity into multiple
    /// datasourcecodes, if they have multiple feeds that are logically and physically distinct
    /// and/or get dropped on a different schedule.
    /// </summary>
    [Serializable]
    [XmlRoot ("dataSource")]
    public class DataSourcePlan
    {
        /// <summary>
        /// These are the Script methods attached to this DataSourcePlan.   This can be null.
        /// If not null, it means there was a script file found in the subdirectory associated
        /// with this datasourceplan, and it compiled successfully to this in-memory assembly,
        /// and the TransformMethods and LitmusTests have been groveled out successfully.
        /// </summary>
        [XmlIgnore] 
        public ScriptMethodResolver ScriptMethodResolver { get; set; }

        /// <summary>
        /// If the DataSourcePlan specifies a ReadyToUseSubPlanTag, then it will use this, 
        /// rather than the stager/manager/accessor/phaselogger descriptors below.  This 
        /// enables authors of integrations to specify a lot less boilerplate.
        /// </summary>
        [XmlElement("readyToUseSubPlanTag")]
        public string ReadyToUseSubPlanTag { get; set; }

        /// <summary>
        /// The phase logger describes an object that the datasource can use to communicate
        /// about each phase of the process.  This can be used to monitor a long-running feed
        /// process.  It's used in addition to Log4Net because parts of this process 
        /// (the PostProcessors) may be doing significant amounts of work on a remote locale
        /// such that they can't use the log4net log, e.g. Sql Server stored procedures.
        /// </summary>
        /// <value>The stage logger.</value>
        [XmlElement("phaseLogger")]
        public PhaseLoggerDescriptor PhaseLogger { get; set; }
    
        /// <summary>
        /// Every datasource gets its own DataSourceCode.  A hypothetical example of this would 
        /// be if (at this writing, it's not clear) each of the NAMG clubs gets its own 
        /// physical feed file.   We might choose to make Fishing club feeds, 'FIS', hunting 
        /// 'HNT', Handyman 'HDY'.   Future feeds with completely different semantics will have 
        /// their own DataSourceCode.
        /// </summary>
        /// <value>The data source code.</value>
        [XmlElement ("dataSourceCode")]
        public String DataSourceCode { get; set; }

        /// <summary>
        /// Name is just a human-readable string that helps us identify what the datasource is 
        /// in the Data Migration Plan.
        /// </summary>
        [XmlElement("name")]
        public String Name { get; set; }

        /// <summary>
        /// PostProcessing can be turned off or on.   This is for development purposes, but 
        /// also it might prove useful to load critical feeds in stages, i.e., run the first
        /// part of the process to stage the feed, examine it, and then run the PostProcessing
        /// when satisfied.  Once a particular migration process has settled down, and we have 
        /// confidence, this would normally be set to true, and the entire thing automated and 
        /// scheduled with cron or any number of similar techniques.
        /// </summary>
        /// <value><c>true</c> postprocess; otherwise, <c>false</c>.</value>
        [XmlElement ("postProcess")]
        public bool PostProcess { get; set; }

        /// <summary>
        /// Lookups are key-value pairs that are used by the RowProcessor(s) to transform
        /// the incoming raw data.
        /// </summary>
        /// <value>The lookups.</value>
        [XmlArray("lookups")]
        [XmlArrayItem("lookup")]
        public LookupDescriptor[] Lookups { get; set; }

        /// <summary>
        /// Existences are data structures that can be seeded with the complete set of a 
        /// particular data item from some data store (e.g. Sql Server), and then added to
        /// as rows are processed to keep track of the set, ongoing.
        /// </summary>
        /// <value>The existences.</value>
        [XmlArray("existences")]
        [XmlArrayItem("existence")]
        public ExistenceDescriptor[] Existences { get; set; }

        /// <summary>
        /// The root location for all the feed files for this DataSourceCode.  Typically, this 
        /// is an s3 url, although the system can hypothetically support pretty much any sort 
        /// of remote or local file storage mechanism.
        /// </summary>
        [XmlElement ("filesLocation")]
        public String FilesLocation { get; set; }

        /// <summary>
        /// Feed Managers together with the FeedAccessor, manages acquiring the feed from 
        /// remote locations.
        /// </summary>
        [XmlElement("feedManager")]
        public string FeedManager { get; set; }

        [XmlElement("feedAccessor")]
        public string FeedAccessor {get; set;}

        /// <summary>
        /// Describes the Feed Stager object used for feed files for this DataSourceCode.
        /// </summary>
        /// <value>The feed stager.</value>
        [XmlElement ("feedStager")]
        public FeedStagerDescriptor FeedStager { get; set; }
        
        [XmlArray("postProcessors")]
        [XmlArrayItem("postProcessor")]
        public PostProcessorDescriptor[] PostProcessors {get; set;}

        /// <summary>
        /// Declaratively describes the set of feed files for this datasourcecode, with
        /// information as to how to transform and process them.
        /// </summary>
        [XmlArray ("feedFilePlans")]
        [XmlArrayItem ("feedFilePlan")]
        public FeedFilePlan[] FeedFilePlans { get; set; }
    }
}
