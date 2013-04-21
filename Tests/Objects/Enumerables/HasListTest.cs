using System;
using System.Collections.Generic;
using System.Reflection;
using AssemblyToProcess.Basic;
using Xunit;

namespace Tests.Objects.Enumerables
{
    public class HasListTest
    {
        private readonly Assembly _assembly;

        public HasListTest()
        {
            this._assembly = WeaverHelper.WeaveAssembly();
        }

//		/// <summary>
//		/// Run this and watch memory usage. If it doesn't go up, there's no memory leak!
//		/// </summary>
//		[Fact]
//		public void MemoryTest()
//		{
//			var fieldsType = this._assembly.GetType("AssemblyToProcess.Basic.Fields");
//
//			dynamic fieldInstanceOne = this.GetFieldInstance("first", 1);
//			dynamic fieldInstanceTwo = this.GetFieldInstance("second", 2);
//
//			var listType = Assembly.GetAssembly(typeof (List<>))
//				.GetType("System.Collections.Generic.List`1");
//
//			var listInstance = (dynamic) Activator.CreateInstance(listType.MakeGenericType(fieldsType));
//			dynamic listAdd = listInstance.GetType().GetMethod("Add");
//			listAdd.Invoke(listInstance, new[] {fieldInstanceOne});
//			listAdd.Invoke(listInstance, new[] {fieldInstanceTwo});
//			listAdd.Invoke(listInstance, new dynamic[] {null});
//
//			var hasListType = this._assembly.GetType("AssemblyToProcess.Enumerables.HasList");
//			var hasListInstance = (dynamic) Activator.CreateInstance(hasListType);
//			hasListType.GetField("ListOfObjects").SetValue(hasListInstance, listInstance);
//
//			for (int i = 0; i < 10000000000; i++)
//			{
//				hasListInstance = hasListInstance.DeepCopy();
//			}
//		}

        [Fact]
        public void ObjectListTest()
        {
            var fieldsType = this._assembly.GetType("AssemblyToProcess.Basic.Fields");

            dynamic fieldInstanceOne = this.GetFieldInstance("first", 1);
            dynamic fieldInstanceTwo = this.GetFieldInstance("second", 2);

            var listType = Assembly.GetAssembly(typeof (List<>))
                .GetType("System.Collections.Generic.List`1");

            var listInstance = (dynamic) Activator.CreateInstance(listType.MakeGenericType(fieldsType));
            dynamic listAdd = listInstance.GetType().GetMethod("Add");
            listAdd.Invoke(listInstance, new[] {fieldInstanceOne});
            listAdd.Invoke(listInstance, new[] {fieldInstanceTwo});
            listAdd.Invoke(listInstance, new dynamic[] {null});

            var hasListType = this._assembly.GetType("AssemblyToProcess.Enumerables.HasList");
            var hasListInstance = (dynamic) Activator.CreateInstance(hasListType);
            hasListType.GetField("ListOfObjects").SetValue(hasListInstance, listInstance);

            // Hand copy.
            var hCopy = hasListInstance.HCopy();

            var hGetter = new ObjectGetter(fieldsType, hCopy.ListOfObjects[0]);
            Assert.Equal("first", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, hGetter.FieldValue("PublicField"));

            hGetter = new ObjectGetter(fieldsType, hCopy.ListOfObjects[1]);
            Assert.Equal("second", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, hGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields) hCopy.ListOfObjects[2]);

            // Deep copy.
            var dCopy = hasListInstance.DeepCopy();

            var dGetter = new ObjectGetter(fieldsType, dCopy.ListOfObjects[0]);
            Assert.Equal("first", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, dGetter.FieldValue("PublicField"));

            dGetter = new ObjectGetter(fieldsType, dCopy.ListOfObjects[1]);
            Assert.Equal("second", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, dGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields) dCopy.ListOfObjects[2]);

            // Bad copy.
            var bCopy = hasListInstance.BCopy();

            var bGetter = new ObjectGetter(fieldsType, bCopy.ListOfObjects[0]);
            Assert.Equal("first", bGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, bGetter.FieldValue("PublicField"));

            bGetter = new ObjectGetter(fieldsType, bCopy.ListOfObjects[1]);
            Assert.Equal("second", bGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, bGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields) bCopy.ListOfObjects[2]);

            // Modify.
            dynamic listClear = listInstance.GetType().GetMethod("Clear");
            listClear.Invoke(listInstance, new dynamic[0]);
            listAdd.Invoke(listInstance, new[] {fieldInstanceTwo});
            listAdd.Invoke(listInstance, new dynamic[] {null});

            // Hand copy.
            hGetter = new ObjectGetter(fieldsType, hCopy.ListOfObjects[0]);
            Assert.Equal("first", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, hGetter.FieldValue("PublicField"));

            hGetter = new ObjectGetter(fieldsType, hCopy.ListOfObjects[1]);
            Assert.Equal("second", hGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, hGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields) hCopy.ListOfObjects[2]);

            // Deep copy.
            dGetter = new ObjectGetter(fieldsType, dCopy.ListOfObjects[0]);
            Assert.Equal("first", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(1, dGetter.FieldValue("PublicField"));

            dGetter = new ObjectGetter(fieldsType, dCopy.ListOfObjects[1]);
            Assert.Equal("second", dGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, dGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields) dCopy.ListOfObjects[2]);

            // Bad copy.
            bGetter = new ObjectGetter(fieldsType, bCopy.ListOfObjects[0]);
            Assert.Equal("second", bGetter.PrivateFieldValue("_privateField"));
            Assert.Equal(2, bGetter.FieldValue("PublicField"));

            // Are nulls handled properly?
            Assert.Equal(null, (Fields) bCopy.ListOfObjects[1]);
        }

        private dynamic GetFieldInstance(string stringValue, int integerValue)
        {
            var fieldsType = this._assembly.GetType("AssemblyToProcess.Basic.Fields");

            var fieldsInstance = (dynamic) Activator.CreateInstance(fieldsType);

            PropertyExtensions.SetPrivateFieldValue(fieldsInstance, "_privateField", stringValue);
            fieldsType.GetField("PublicField").SetValue(fieldsInstance, integerValue);

            return fieldsInstance;
        }

        [Fact]
        public void PrimitiveListTest()
        {
            var primitiveType = typeof (int);

            var listType = Assembly.GetAssembly(typeof (List<>))
                .GetType("System.Collections.Generic.List`1");

            var listInstance = (dynamic) Activator.CreateInstance(listType.MakeGenericType(primitiveType));
            dynamic listAdd = listInstance.GetType().GetMethod("Add");
            listAdd.Invoke(listInstance, new[] {(dynamic) 1});
            listAdd.Invoke(listInstance, new[] {(dynamic) 2});

            var hasListType = this._assembly.GetType("AssemblyToProcess.Enumerables.HasList");
            var hasListInstance = (dynamic) Activator.CreateInstance(hasListType);
            hasListType.GetField("ListOfPrimitives").SetValue(hasListInstance, listInstance);

            // Hand copy.
            var hCopy = hasListInstance.HCopy();

            Assert.Equal(1, hCopy.ListOfPrimitives[0]);
            Assert.Equal(2, hCopy.ListOfPrimitives[1]);

            // Deep copy.
            var dCopy = hasListInstance.DeepCopy();

            Assert.Equal(1, dCopy.ListOfPrimitives[0]);
            Assert.Equal(2, dCopy.ListOfPrimitives[1]);

            // Bad copy.
            var bCopy = hasListInstance.BCopy();

            Assert.Equal(1, bCopy.ListOfPrimitives[0]);
            Assert.Equal(2, bCopy.ListOfPrimitives[1]);

            // Modify.
            dynamic listClear = listInstance.GetType().GetMethod("Clear");
            listClear.Invoke(listInstance, new dynamic[0]);
            listAdd.Invoke(listInstance, new[] {(dynamic) 2});

            // Hand copy.
            Assert.Equal(1, hCopy.ListOfPrimitives[0]);
            Assert.Equal(2, hCopy.ListOfPrimitives[1]);

            // Deep copy.
            Assert.Equal(1, dCopy.ListOfPrimitives[0]);
            Assert.Equal(2, dCopy.ListOfPrimitives[1]);

            // Bad copy.
            Assert.Equal(2, bCopy.ListOfPrimitives[0]);
        }
    }
}