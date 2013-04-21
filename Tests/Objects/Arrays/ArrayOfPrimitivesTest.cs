using System;
using System.Reflection;
using Xunit;

namespace Tests.Objects.Arrays
{
    public class ArrayOfPrimitivesTest
    {
        private readonly Assembly _assembly;

        public ArrayOfPrimitivesTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

        [Fact]
        public void ArrayOfPrimitives()
        {
            Type fieldsType = typeof (int);
            dynamic intsArrayInstance = Array.CreateInstance(fieldsType, 2);
            intsArrayInstance.SetValue(1, 0);
            intsArrayInstance.SetValue(2, 1);

            var arrayOfPrimitivesType = this._assembly.GetType("AssemblyToProcess.Arrays.ArrayOfPrimitives");
            var arrayOfPrimitivesInstance = (dynamic) Activator.CreateInstance(arrayOfPrimitivesType);
            arrayOfPrimitivesType.GetField("IntsArray").SetValue(arrayOfPrimitivesInstance, intsArrayInstance);

            // Hand copy.
            var hCopy = arrayOfPrimitivesInstance.HCopy();

            Assert.Equal(1, hCopy.IntsArray[0]);
            Assert.Equal(2, hCopy.IntsArray[1]);

            // Deep copy.
            var dCopy = arrayOfPrimitivesInstance.DeepCopy();

            Assert.Equal(1, dCopy.IntsArray[0]);
            Assert.Equal(2, dCopy.IntsArray[1]);

            // Bad copy.
            var bCopy = arrayOfPrimitivesInstance.BCopy();

            Assert.Equal(1, bCopy.IntsArray[0]);
            Assert.Equal(2, bCopy.IntsArray[1]);

            // Modify.
            intsArrayInstance.SetValue(2, 0);
            intsArrayInstance.SetValue(3, 1);
            arrayOfPrimitivesType.GetField("IntsArray").SetValue(arrayOfPrimitivesInstance, intsArrayInstance);

            // Hand copy (should stay the same).
            Assert.Equal(1, hCopy.IntsArray[0]);
            Assert.Equal(2, hCopy.IntsArray[1]);

            // Deep copy (should stay the same).
            Assert.Equal(1, dCopy.IntsArray[0]);
            Assert.Equal(2, dCopy.IntsArray[1]);

            // Bad copy (should be modified).
            Assert.Equal(2, bCopy.IntsArray[0]);
            Assert.Equal(3, bCopy.IntsArray[1]);
        }
    }
}