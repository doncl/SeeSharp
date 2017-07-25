using System;

namespace DataMigration
{
    /// <summary>
    /// ReadyToUsePlans describe collections of components for use.  Some components just need
    /// an assembly name to load, but others need properties, also.  So various types of
    /// object descriptors in the plan will implement this interface.
    /// </summary>
    public interface IComponentDescriptor
    {
        /// <summary>
        /// Assembly-qualified name.
        /// </summary>
        /// <value>The assembly.</value>
        String Assembly { get; set; }

        /// <summary>
        /// A descriptive name for the component.
        /// </summary>
        /// <value>The name.</value>
        String Name {get; set;}

        /// <summary>
        /// Static properties declared in the plan for this instance of the component.
        /// </summary>
        /// <value>The properties.</value>
        DescriptorProperty[] Properties { get; set; }
    }
}
