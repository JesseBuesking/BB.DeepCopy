namespace AssemblyToProcess.Interfaces
{
    public interface ITestInterface
    {
        int InterfaceProperty
        {
            get;
            set;
        }

        string IgnoredMethod();
    }
}
