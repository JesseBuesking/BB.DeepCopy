using System;
using System.Reflection;
using AssemblyToProcess.Basic;
using Xunit;

namespace Tests.Objects.Arrays
{
    public class ArrayOfObjectsTest
    {
        private readonly Assembly _assembly;

        public ArrayOfObjectsTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void ArrayOfObjects()
        {
            var fieldsType = this._assembly.GetType("AssemblyToProcess.Basic.Fields");

            dynamic fieldInstanceOne = this.GetFieldInstance("first", 1);
            dynamic fieldInstanceTwo = this.GetFieldInstance("second", 2);

            dynamic fieldsArrayInstance = Array.CreateInstance(fieldsType, 3);
            fieldsArrayInstance.SetValue(fieldInstanceOne, 0);
            fieldsArrayInstance.SetValue(fieldInstanceTwo, 1);
            fieldsArrayInstance.SetValue(null, 2);

            var arrayOfObjectsType = this._assembly
                .GetType("AssemblyToProcess.Arrays.ArrayOfObjects");
            var arrayOfObjectsInstance = (dynamic) Activator.CreateInstance(arrayOfObjectsType);
            arrayOfObjectsType.GetField("FieldsArray").SetValue(arrayOfObjectsInstance, fieldsArrayInstance);

            // Hand copy.
            var hCopy = arrayOfObjectsInstance.HCopy();

            var hGetter = new ObjectGetter(fieldsType, hCopy.FieldsArray[0]);
            Assert.Equal("first", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, hGetter.FieldValue("PublicField"));

            hGetter = new ObjectGetter(fieldsType, hCopy.FieldsArray[1]);
            Assert.Equal("second", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, hGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields)hCopy.FieldsArray[2]);

            // Deep copy.
            var dCopy = arrayOfObjectsInstance.DeepCopy();

            var dGetter = new ObjectGetter(fieldsType, dCopy.FieldsArray[0]);
            Assert.Equal("first", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, dGetter.FieldValue("PublicField"));

            dGetter = new ObjectGetter(fieldsType, dCopy.FieldsArray[1]);
            Assert.Equal("second", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, dGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields)dCopy.FieldsArray[2]);
 
            // Bad copy.
            var bCopy = arrayOfObjectsInstance.BCopy();

            var bGetter = new ObjectGetter(fieldsType, bCopy.FieldsArray[0]);
            Assert.Equal("first", bGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, bGetter.FieldValue("PublicField"));

            bGetter = new ObjectGetter(fieldsType, bCopy.FieldsArray[1]);
            Assert.Equal("second", bGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, bGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields)bCopy.FieldsArray[2]);

            // Modify.
            fieldInstanceOne = this.GetFieldInstance("third", 3);
            fieldInstanceTwo = this.GetFieldInstance("fourth", 4);
            fieldsArrayInstance.SetValue(fieldInstanceOne, 0);
            fieldsArrayInstance.SetValue(fieldInstanceTwo, 1);
            arrayOfObjectsType.GetField("FieldsArray").SetValue(arrayOfObjectsInstance, fieldsArrayInstance);

            // Hand copy (should stay the same).
            hGetter = new ObjectGetter(fieldsType, hCopy.FieldsArray[0]);
            Assert.Equal("first", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, hGetter.FieldValue("PublicField"));

            hGetter = new ObjectGetter(fieldsType, hCopy.FieldsArray[1]);
            Assert.Equal("second", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, hGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields)hCopy.FieldsArray[2]);

            // Deep copy (should stay the same).
            dGetter = new ObjectGetter(fieldsType, dCopy.FieldsArray[0]);
            Assert.Equal("first", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, dGetter.FieldValue("PublicField"));

            dGetter = new ObjectGetter(fieldsType, dCopy.FieldsArray[1]);
            Assert.Equal("second", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, dGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields)dCopy.FieldsArray[2]);
 
            // Bad copy (should be modified).
            bGetter = new ObjectGetter(fieldsType, bCopy.FieldsArray[0]);
            Assert.Equal("third", bGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(3, bGetter.FieldValue("PublicField"));

            bGetter = new ObjectGetter(fieldsType, bCopy.FieldsArray[1]);
            Assert.Equal("fourth", bGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(4, bGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields)bCopy.FieldsArray[2]);
        }

        private dynamic GetFieldInstance(string stringValue, int integerValue)
        {
            var fieldsType = this._assembly.GetType("AssemblyToProcess.Basic.Fields");

            var fieldsInstance = (dynamic) Activator.CreateInstance(fieldsType);

            PropertyExtensions.SetPrivateFieldValue(fieldsInstance, "_privateField", stringValue);
            fieldsType.GetField("PublicField").SetValue(fieldsInstance, integerValue);

            return fieldsInstance;
        }
    }
}