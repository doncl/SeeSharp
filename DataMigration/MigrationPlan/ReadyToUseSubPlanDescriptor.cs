using System;
using System.Xml.Serialization;

namespace DataMigration
{
    /// <summary>
    /// This class is the deserialized form of the xml section in the DataMigrationPlan that
    /// lists ReadyToUseSubPlans, i.e. collections of components that actual datasourceplans 
    /// consume in order to do their work.  The MigrationEngine will deserialized this thing,
    /// and use it to construct an actual ReadyToUseSubPlan, which will be made available for
    /// the 
    /// </summary>
    [Serializable]
    [XmlRoot ("readyToUseSubPlan")]
    public class ReadyToUseSubPlanDescriptor
    {
        [XmlElement("tag")]
        public string Tag { get; set;}

        [XmlElement("feedStager")]
        public FeedStagerDescriptor FeedStager {get; set;}

        [XmlElement("feedManager")]
        public string FeedManager { get; set;}

        [XmlElement("feedAccessor")]
        public string FeedAccessor {get; set;}

        [XmlElement("phaseLogger")]
        public PhaseLoggerDescriptor PhaseLogger {get; set;}

        [XmlElement("parser")]
        public ParserDescriptor Parser { get; set; }

        [XmlArray("postProcessors")]
        [XmlArrayItem("postProcessor")]
        public PostProcessorDescriptor[] PostProcessors { get; set; }
    }
}
