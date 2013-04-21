using System;
using System.Reflection;
using Xunit;

namespace Tests.Objects.Basic
{
    public class PropertiesTest
    {
        private readonly Assembly _assembly;

        public PropertiesTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void PropertiesBasic()
        {
            var propertiesType = this._assembly.GetType("AssemblyToProcess.Basic.Properties");
            var propertiesInstance = (dynamic) Activator.CreateInstance(propertiesType);

            PropertyExtensions.SetPrivatePropertyValue(
                propertiesInstance, "BackedProperty", 1.23);
            PropertyExtensions.SetPrivatePropertyValue(
                propertiesInstance, "PrivateProperty", 123);
            propertiesType.GetProperty("PublicProperty").SetValue(propertiesInstance, "I'm public.");

            // Hand copy.
            var hCopy = propertiesInstance.HCopy();
            var hGetter = new ObjectGetter(propertiesType, hCopy);

            Assert.Equal(1.23, hGetter.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(123, hGetter.PrivatePropertyValue("PrivateProperty"));
            Assert.Equal("I'm public.", hGetter.PropertyValue("PublicProperty"));

            // Deep copy.
            var dCopy = propertiesInstance.DeepCopy();
            var dGetter = new ObjectGetter(propertiesType, dCopy);

            Assert.Equal(1.23, dGetter.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(123, dGetter.PrivatePropertyValue("PrivateProperty"));
            Assert.Equal("I'm public.", dGetter.PropertyValue("PublicProperty"));
        }
    }
}
