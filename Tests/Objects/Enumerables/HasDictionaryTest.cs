using System;
using System.Collections.Generic;
using System.Reflection;
using AssemblyToProcess.Basic;
using Xunit;

namespace Tests.Objects.Enumerables
{
    public class HasDictionaryTest
    {
        private readonly Assembly _assembly;

        public HasDictionaryTest()
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
//			var dictionaryType = Assembly.GetAssembly(typeof (Dictionary<>))
//				.GetType("System.Collections.Generic.Dictionary`1");
//
//			var dictionaryInstance = (dynamic) Activator.CreateInstance(dictionaryType.MakeGenericType(fieldsType));
//			dynamic dictionaryAdd = dictionaryInstance.GetType().GetMethod("Add");
//			dictionaryAdd.Invoke(dictionaryInstance, new[] {fieldInstanceOne});
//			dictionaryAdd.Invoke(dictionaryInstance, new[] {fieldInstanceTwo});
//			dictionaryAdd.Invoke(dictionaryInstance, new dynamic[] {null});
//
//			var hasDictionaryType = this._assembly.GetType("AssemblyToProcess.Enumerables.HasDictionary");
//			var hasDictionaryInstance = (dynamic) Activator.CreateInstance(hasDictionaryType);
//			hasDictionaryType.GetField("DictionaryOfObjects").SetValue(hasDictionaryInstance, dictionaryInstance);
//
//			for (int i = 0; i < 10000000000; i++)
//			{
//				hasDictionaryInstance = hasDictionaryInstance.DeepCopy();
//			}
//		}

        [Fact]
        public void ObjectDictionaryTest()
        {
            var fieldsType = this._assembly.GetType("AssemblyToProcess.Basic.Fields");

            dynamic fieldInstanceOne = this.GetFieldInstance("first", 1);
            dynamic fieldInstanceTwo = this.GetFieldInstance("second", 2);
            dynamic fieldInstanceThree = this.GetFieldInstance("third", 3);

            var dictionaryType = Assembly.GetAssembly(typeof (Dictionary<Fields, Fields>))
                .GetType("System.Collections.Generic.Dictionary`2");

            var dictionaryInstance = (dynamic) Activator
                .CreateInstance(dictionaryType.MakeGenericType(fieldsType, fieldsType));
            dynamic dictionaryAdd = dictionaryInstance.GetType().GetMethod("Add");
            dictionaryAdd.Invoke(dictionaryInstance, new[] {fieldInstanceOne, fieldInstanceOne});
            dictionaryAdd.Invoke(dictionaryInstance, new[] {fieldInstanceTwo, fieldInstanceTwo});
            dictionaryAdd.Invoke(dictionaryInstance, new[] {fieldInstanceThree, null});

            var hasDictionaryType = this._assembly.GetType("AssemblyToProcess.Enumerables.HasDictionary");
            var hasDictionaryInstance = (dynamic) Activator.CreateInstance(hasDictionaryType);
            hasDictionaryType.GetField("DictionaryOfObjects").SetValue(hasDictionaryInstance, dictionaryInstance);

            // Hand copy.
            var hCopy = hasDictionaryInstance.HCopy();

            foreach (var item in hCopy.DictionaryOfObjects)
            {
                ObjectGetter hGetter = new ObjectGetter(fieldsType, item.Value);
                if (1 == item.Key.PublicField)
                {
                    Assert.Equal("first", hGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(1, hGetter.FieldValue("PublicField"));
                }
                else if (2 == item.Key.PublicField)
                {
                    Assert.Equal("second", hGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(2, hGetter.FieldValue("PublicField"));
                }
                else
                {
                    // Are nulls handled properly?
                    Assert.Equal(null, (Fields) item.Value);
                }
            }

            // Deep copy.
            dynamic dCopy = hasDictionaryInstance.DeepCopy();

            foreach (var item in dCopy.DictionaryOfObjects)
            {
                ObjectGetter dGetter = new ObjectGetter(fieldsType, item.Value);
                if (1 == item.Key.PublicField)
                {
                    Assert.Equal("first", dGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(1, dGetter.FieldValue("PublicField"));
                }
                else if (2 == item.Key.PublicField)
                {
                    Assert.Equal("second", dGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(2, dGetter.FieldValue("PublicField"));
                }
                else
                {
                    // Are nulls handled properly?
                    Assert.Equal(null, (Fields) item.Value);
                }
            }

            // Bad copy.
            dynamic bCopy = hasDictionaryInstance.BCopy();

            foreach (var item in bCopy.DictionaryOfObjects)
            {
                ObjectGetter bGetter = new ObjectGetter(fieldsType, item.Value);
                if (1 == item.Key.PublicField)
                {
                    Assert.Equal("first", bGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(1, bGetter.FieldValue("PublicField"));
                }
                else if (2 == item.Key.PublicField)
                {
                    Assert.Equal("second", bGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(2, bGetter.FieldValue("PublicField"));
                }
                else
                {
                    // Are nulls handled properly?
                    Assert.Equal(null, (Fields) item.Value);
                }
            }

            // Modify.
            dynamic dictionaryClear = dictionaryInstance.GetType().GetMethod("Clear");
            dictionaryClear.Invoke(dictionaryInstance, new dynamic[0]);
            dictionaryAdd.Invoke(dictionaryInstance, new[] {fieldInstanceTwo, fieldInstanceOne});
            dictionaryAdd.Invoke(dictionaryInstance, new[] {fieldInstanceThree, null});

            // Hand copy.
            foreach (var item in hCopy.DictionaryOfObjects)
            {
                ObjectGetter hGetter = new ObjectGetter(fieldsType, item.Value);
                if (1 == item.Key.PublicField)
                {
                    Assert.Equal("first", hGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(1, hGetter.FieldValue("PublicField"));
                }
                else if (2 == item.Key.PublicField)
                {
                    Assert.Equal("second", hGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(2, hGetter.FieldValue("PublicField"));
                }
                else
                {
                    // Are nulls handled properly?
                    Assert.Equal(null, (Fields) item.Value);
                }
            }

            // Deep copy.
            foreach (var item in dCopy.DictionaryOfObjects)
            {
                ObjectGetter dGetter = new ObjectGetter(fieldsType, item.Value);
                if (1 == item.Key.PublicField)
                {
                    Assert.Equal("first", dGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(1, dGetter.FieldValue("PublicField"));
                }
                else if (2 == item.Key.PublicField)
                {
                    Assert.Equal("second", dGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(2, dGetter.FieldValue("PublicField"));
                }
                else
                {
                    // Are nulls handled properly?
                    Assert.Equal(null, (Fields) item.Value);
                }
            }

            // Bad copy.
            foreach (var item in bCopy.DictionaryOfObjects)
            {
                ObjectGetter bGetter = new ObjectGetter(fieldsType, item.Value);
                if (2 == item.Key.PublicField)
                {
                    Assert.Equal("first", bGetter.PrivateFieldValue("_privateField"));
                    Assert.Equal(1, bGetter.FieldValue("PublicField"));
                }
                else
                {
                    // Are nulls handled properly?
                    Assert.Equal(null, (Fields) item.Value);
                }
            }
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
        public void PrimitiveDictionaryTest()
        {
            var keyType = typeof (int);
            var valueType = typeof (int);

            var dictionaryType = Assembly.GetAssembly(typeof (Dictionary<int, int>))
                .GetType("System.Collections.Generic.Dictionary`2");

            var dictionaryInstance = (dynamic) Activator.CreateInstance(
                dictionaryType.MakeGenericType(keyType, valueType));
            dynamic dictionaryAdd = dictionaryInstance.GetType().GetMethod("Add");
            dictionaryAdd.Invoke(dictionaryInstance, new[] {(dynamic) 1, (dynamic) 1});
            dictionaryAdd.Invoke(dictionaryInstance, new[] {(dynamic) 2, (dynamic) 2});

            var hasDictionaryType = this._assembly.GetType("AssemblyToProcess.Enumerables.HasDictionary");
            var hasDictionaryInstance = (dynamic) Activator.CreateInstance(hasDictionaryType);
            hasDictionaryType.GetField("DictionaryOfPrimitives").SetValue(hasDictionaryInstance, dictionaryInstance);

            // Hand copy.
            var hCopy = hasDictionaryInstance.HCopy();

            Assert.Equal(1, hCopy.DictionaryOfPrimitives[1]);
            Assert.Equal(2, hCopy.DictionaryOfPrimitives[2]);

            // Deep copy.
            var dCopy = hasDictionaryInstance.DeepCopy();

            Assert.Equal(1, dCopy.DictionaryOfPrimitives[1]);
            Assert.Equal(2, dCopy.DictionaryOfPrimitives[2]);

            // Bad copy.
            var bCopy = hasDictionaryInstance.BCopy();

            Assert.Equal(1, bCopy.DictionaryOfPrimitives[1]);
            Assert.Equal(2, bCopy.DictionaryOfPrimitives[2]);

            // Modify.
            dynamic dictionaryClear = dictionaryInstance.GetType().GetMethod("Clear");
            dictionaryClear.Invoke(dictionaryInstance, new dynamic[0]);
            dictionaryAdd.Invoke(dictionaryInstance, new[] {(dynamic) 2, (dynamic) 3});

            // Hand copy.
            Assert.Equal(1, hCopy.DictionaryOfPrimitives[1]);
            Assert.Equal(2, hCopy.DictionaryOfPrimitives[2]);

            // Deep copy.
            Assert.Equal(1, dCopy.DictionaryOfPrimitives[1]);
            Assert.Equal(2, dCopy.DictionaryOfPrimitives[2]);

            // Bad copy.
            Assert.Equal(3, bCopy.DictionaryOfPrimitives[2]);
        }
    }
}