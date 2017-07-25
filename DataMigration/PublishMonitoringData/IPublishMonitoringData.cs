using System;
using System.Collections.Generic;
using System.Linq;

namespace DataMigration
{
    public interface IPublishMonitoringData
    {
        void PublishSuccess(DataSourcePlan plan, IOrderedEnumerable<PhaseLogEntry> logEntries);
                    
        void PublishFailure(DataSourcePlan plan, IOrderedEnumerable<PhaseLogEntry> logEntries,
                            Exception exception);
    }
}

