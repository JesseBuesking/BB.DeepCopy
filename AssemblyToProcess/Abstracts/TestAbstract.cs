using System;
using AssemblyToProcess.Interfaces;

namespace AssemblyToProcess.Abstracts
{
    [Serializable] // For Clone.
    public abstract class TestAbstract : ITestInterface
    {
        public int InterfaceProperty
        {
            get;
            set;
        }

        public abstract string IgnoredMethod();
    }
}