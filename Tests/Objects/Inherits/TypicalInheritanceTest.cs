using System;
using System.Reflection;
using Xunit;

namespace Tests.Objects.Inherits
{
    public class TypicalInheritanceTest
    {
        private readonly Assembly _assembly;

        public TypicalInheritanceTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void Tests()
        {
            var propT = this._assembly.GetType("AssemblyToProcess.Basic.Properties");
            var propI = (dynamic) Activator.CreateInstance(propT);

            PropertyExtensions.SetPrivatePropertyValue(propI, "BackedProperty", 1.23);
            PropertyExtensions.SetPrivatePropertyValue(propI, "PrivateProperty", 123);
            propT.GetProperty("PublicProperty").SetValue(propI, "I'm public.");

            var typicalIT = this._assembly.GetType("AssemblyToProcess.Inherits.TypicalInheritance");
            var typicalII = (dynamic) Activator.CreateInstance(typicalIT);

            typicalIT.GetField("Properties").SetValue(typicalII, propI);

            // Hand copy.
            var hCopy = typicalII.HCopy();
            var hGet = new ObjectGetter(typicalIT, hCopy);

            var fValue = hGet.FieldValue("Properties");
            hGet = new ObjectGetter(propT, fValue);

            Assert.Equal(1.23, hGet.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(0, hGet.PrivatePropertyValue("PrivateProperty")); // Won't copy since it's private.
            Assert.Equal("I'm public.", hGet.PropertyValue("PublicProperty"));

            // Deep copy.
            var dCopy = typicalII.DeepCopy();
            var dGet = new ObjectGetter(typicalIT, dCopy);

            fValue = dGet.FieldValue("Properties");
            dGet = new ObjectGetter(propT, fValue);

            Assert.Equal(1.23, dGet.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(123, dGet.PrivatePropertyValue("PrivateProperty"));
            Assert.Equal("I'm public.", dGet.PropertyValue("PublicProperty"));

            // Bad copy.
            var bCopy = typicalII.BCopy();
            var bGet = new ObjectGetter(typicalIT, bCopy);

            fValue = bGet.FieldValue("Properties");
            bGet = new ObjectGetter(propT, fValue);
 
            Assert.Equal(1.23, bGet.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(123, bGet.PrivatePropertyValue("PrivateProperty"));
            Assert.Equal("I'm public.", bGet.PropertyValue("PublicProperty"));
            
            // Modify.
            PropertyExtensions.SetPrivatePropertyValue(propI, "BackedProperty", 2.34);
            PropertyExtensions.SetPrivatePropertyValue(propI, "PrivateProperty", 234);
            propT.GetProperty("PublicProperty").SetValue(propI, "I'm changed!");
            typicalIT.GetField("Properties").SetValue(typicalII, propI);

            // Hand copy.
            hGet = new ObjectGetter(typicalIT, hCopy);

            fValue = hGet.FieldValue("Properties");
            hGet = new ObjectGetter(propT, fValue);

            Assert.Equal(1.23, hGet.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(0, hGet.PrivatePropertyValue("PrivateProperty")); // Won't copy since it's private.
            Assert.Equal("I'm public.", hGet.PropertyValue("PublicProperty"));

            // Deep copy.
            dGet = new ObjectGetter(typicalIT, dCopy);

            fValue = dGet.FieldValue("Properties");
            dGet = new ObjectGetter(propT, fValue);

            Assert.Equal(1.23, dGet.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(123, dGet.PrivatePropertyValue("PrivateProperty"));
            Assert.Equal("I'm public.", dGet.PropertyValue("PublicProperty"));

            // Bad copy.
            bGet = new ObjectGetter(typicalIT, bCopy);

            fValue = bGet.FieldValue("Properties");
            bGet = new ObjectGetter(propT, fValue);
 
            Assert.Equal(2.34, bGet.PrivatePropertyValue("BackedProperty"));
            Assert.Equal(234, bGet.PrivatePropertyValue("PrivateProperty"));
            Assert.Equal("I'm changed!", bGet.PropertyValue("PublicProperty"));
        }
    }
}