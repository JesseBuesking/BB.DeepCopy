using System;
using System.Reflection;
using Xunit;

namespace Tests.Objects.Basic
{
    public class FieldsTest
    {
        private readonly Assembly _assembly;

        public FieldsTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void FieldsBasic()
        {
            var fieldsType = this._assembly.GetType("AssemblyToProcess.Basic.Fields");
            var fieldsInstance = (dynamic) Activator.CreateInstance(fieldsType);

            PropertyExtensions.SetPrivateFieldValue(fieldsInstance, "_privateField", "I'm private.");
            fieldsType.GetField("PublicField").SetValue(fieldsInstance, 123);

            // Hand copy.
            var hCopy = fieldsInstance.HCopy();
            var hGetter = new ObjectGetter(fieldsType, hCopy);

            Assert.Equal("I'm private.", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(123, hGetter.FieldValue("PublicField"));

            // Deep copy.
            var dCopy = fieldsInstance.DeepCopy();
            var dGetter = new ObjectGetter(fieldsType, dCopy);

            Assert.Equal("I'm private.", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(123, dGetter.FieldValue("PublicField"));
        }
    }
}