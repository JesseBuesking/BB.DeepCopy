using System;
using AssemblyToProcess.Basic;

namespace AssemblyToProcess.Inherits
{
    [Serializable] // For Clone.
    public class TypicalInheritance
    {
        public Properties Properties;

        public TypicalInheritance HCopy()
        {
            Properties properties = new Properties
                {
                    BackedProperty = this.Properties.BackedProperty,
                    PublicProperty = this.Properties.PublicProperty,
                    // Can't access since it's private
                    //Private Property = this.Properties.PrivateProperty
                };

            return new TypicalInheritance
                {
                    Properties = properties
                };
        }

        public TypicalInheritance BCopy()
        {
            return new TypicalInheritance
                {
                    Properties = this.Properties
                };
        }
    }
}