namespace DataMigration
{
    /// <summary>
    /// This is a lookup object, with key-values explicitly listed in the DataSourcePlan.
    /// </summary>
    public class PlanDrivenLookup : AbstractBaseLookup
    {
        #region ILookup implementation

        /// <summary>
        /// This is essentially a two-phase ctor.  The sequence of construction is, instantiate
        /// the lookup, then set its properties, then call Init() so it can migrate the 
        /// property values to its internal dictionary.
        /// </summary>
        public override void Init()
        {
            if (Properties != null) {
                foreach (var property in Properties) {
                    LookupValues.Add(property.Name, property.Value);
                }
            }
        }
        #endregion

    }
}
