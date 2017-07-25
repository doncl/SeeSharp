using System;

namespace DataMigration
{
    /// <summary>
    /// For some feeds, the LumenWorks.Framework.IO Csv does a better job than the home-grown
    /// ParameterizedDelimiterParser.  But...it turns out there's a bug in the Microsoft Xml
    /// deserialization code when trying to specify \t in the plan.  So instead we just derive
    /// from the CsvParser here, and hardcode the \t.
    /// </summary>
    public class CsvTabDelimitedParser : CsvParser
    {
        protected override void GetDelimiter()
        {
            delimiter = '\t';
        }
    }
}

