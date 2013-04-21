using System;
using System.Reflection;
using AssemblyToProcess.Performance;
using Xunit;

namespace Tests.Performance
{
    public class PerformanceTests
    {
        private const long _warmupIterations = 10000;

        private const long _iterations = 100000;

        private readonly Assembly _assembly;

        public PerformanceTests()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void FieldTest()
        {
            var timingHelper = new TimingHelper(_warmupIterations, _iterations);

            // One field.
            var oneFieldType = this._assembly.GetType("AssemblyToProcess.Performance.OneField");
            var instance = (dynamic) Activator.CreateInstance(oneFieldType);
            var oneField = new OneField();

            timingHelper.TimeIt("One Field",
                new TimingHelper.Data("Deep Copy", () => instance.DeepCopy()),
                new TimingHelper.Data("Hand Copy", () => instance.HCopy()),
                new TimingHelper.Data("Clone", () => oneField.Clone())
                );

            // Five fields.
            var fiveFieldsType = this._assembly.GetType("AssemblyToProcess.Performance.FiveFields");
            instance = (dynamic) Activator.CreateInstance(fiveFieldsType);
            var fiveFields = new FiveFields();

            timingHelper.TimeIt("Five Fields",
                new TimingHelper.Data("Deep Copy", () => instance.DeepCopy()),
                new TimingHelper.Data("Hand Copy", () => instance.HCopy()),
                new TimingHelper.Data("Clone", () => fiveFields.Clone())
                );

            // Ten fields.
            var tenFieldsType = this._assembly.GetType("AssemblyToProcess.Performance.TenFields");
            instance = (dynamic) Activator.CreateInstance(tenFieldsType);
            var tenFields = new TenFields();

            timingHelper.TimeIt("Ten Fields",
                new TimingHelper.Data("Deep Copy", () => instance.DeepCopy()),
                new TimingHelper.Data("Hand Copy", () => instance.HCopy()),
                new TimingHelper.Data("Clone", () => tenFields.Clone())
                );
        }

        [Fact]
        public void PropertyTest()
        {
            var timingHelper = new TimingHelper(_warmupIterations, _iterations);

            // One property.
            var onePropertyType = this._assembly.GetType("AssemblyToProcess.Performance.OneProperty");
            var instance = (dynamic) Activator.CreateInstance(onePropertyType);
            var oneProperty = new OneProperty();

            timingHelper.TimeIt("One property",
                new TimingHelper.Data("Deep Copy", () => instance.DeepCopy()),
                new TimingHelper.Data("Hand Copy", () => instance.HCopy()),
                new TimingHelper.Data("Clone", () => oneProperty.Clone())
                );

            // Five properties.
            var fivePropertiesType = this._assembly.GetType("AssemblyToProcess.Performance.FiveProperties");
            instance = (dynamic) Activator.CreateInstance(fivePropertiesType);
            var fiveProperties = new FiveProperties();

            timingHelper.TimeIt("Five properties",
                new TimingHelper.Data("Deep Copy", () => instance.DeepCopy()),
                new TimingHelper.Data("Hand Copy", () => instance.HCopy()),
                new TimingHelper.Data("Clfive", () => fiveProperties.Clone())
                );

            // Ten properties.
            var tenPropertiesType = this._assembly.GetType("AssemblyToProcess.Performance.TenProperties");
            instance = (dynamic) Activator.CreateInstance(tenPropertiesType);
            var tenProperties = new TenProperties();

            timingHelper.TimeIt("Ten properties",
                new TimingHelper.Data("Deep Copy", () => instance.DeepCopy()),
                new TimingHelper.Data("Hand Copy", () => instance.HCopy()),
                new TimingHelper.Data("Clten", () => tenProperties.Clone())
                );
        }

        [Fact]
        public void ArrayTest()
        {
            // Array of primitives.
            // Array of objects.
        }

        [Fact]
        public void ListTest()
        {
            // List of primitives.
            // List of objects.
        }
    }
}