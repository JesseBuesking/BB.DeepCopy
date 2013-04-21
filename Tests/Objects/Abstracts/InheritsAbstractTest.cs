using System;
using System.Reflection;
using Xunit;

namespace Tests.Objects.Abstracts
{
    public class InheritsAbstractTest
    {
        private readonly Assembly _assembly;

        public InheritsAbstractTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void ObjectInheritanceTest()
        {
            var inheritsAbstractType = this._assembly.GetType("AssemblyToProcess.Abstracts.InheritsAbstract");
            var inheritsAbstractInstance = (dynamic) Activator.CreateInstance(inheritsAbstractType);

            inheritsAbstractType.GetField("MainObject").SetValue(inheritsAbstractInstance, "main obj");
            inheritsAbstractType.GetProperty("InterfaceProperty").SetValue(inheritsAbstractInstance, 123);

            // Hand copy.
            var hCopy = inheritsAbstractInstance.HCopy();

            var hGet = new ObjectGetter(inheritsAbstractType, hCopy);

            Assert.Equal("main obj", hGet.FieldValue("MainObject"));
            Assert.Equal(123, hGet.PropertyValue("InterfaceProperty"));

            // Deep copy.
            var dCopy = inheritsAbstractInstance.DeepCopy();

            var dGet = new ObjectGetter(inheritsAbstractType, dCopy);

            Assert.Equal("main obj", dGet.FieldValue("MainObject"));
            Assert.Equal(123, dGet.PropertyValue("InterfaceProperty"));
        }
    }
}