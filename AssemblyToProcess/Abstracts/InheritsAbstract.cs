using System;

namespace AssemblyToProcess.Abstracts
{
    [Serializable] // For Clone.
    public class InheritsAbstract : TestAbstract
    {
        public string MainObject;

        public override string IgnoredMethod()
        {
            throw new System.NotImplementedException();
        }

        public InheritsAbstract HCopy()
        {
            return new InheritsAbstract
                {
                    InterfaceProperty = this.InterfaceProperty,
                    MainObject = this.MainObject
                };
        }
    }
}