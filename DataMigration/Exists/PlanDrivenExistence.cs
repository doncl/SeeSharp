using System;

namespace DataMigration
{
    /// <summary>
    /// Plan driven existence.  This existence object will take whatever is hardcoded in the
    /// Plan (either the MigrationPlan or the DataSourcePlan) and add those values to the
    /// HashSet.  It can be left empty for use as a de-duping first-writer-wins strategy on
    /// feed data objects.
    /// </summary>
    public class PlanDrivenExistence : AbstractBaseExistence
    {
        #region IExistence implementation

        /// <summary>
        /// This is essentially a two-phase ctor. The sequence of construction is, instantiate
        /// the object, then set its properties, then call Init() so it can migrate the 
        /// property values to their implementation-specific roles.  In this concrete child
        /// implementation, it is using the hardcode values in the Plan to populate the 
        /// HashSet.
        /// </summary>
        public override void Init()
        {
            if (Properties != null) {
                foreach (var property in Properties) {
                    Values.Add(property.Value);
                }
            }
        }
        #endregion
    }
}

