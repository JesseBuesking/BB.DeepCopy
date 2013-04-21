using System;
using System.Collections.Generic;
using AssemblyToProcess.Basic;

namespace AssemblyToProcess.Enumerables
{
    [Serializable] // For Clone.
    public class HasDictionary
    {
        public Dictionary<Fields, Fields> DictionaryOfObjects;

        public Dictionary<int, int> DictionaryOfPrimitives;

        public HasDictionary HCopy()
        {
            Dictionary<Fields, Fields> dictionaryOfObjects = null;
            if (null != this.DictionaryOfObjects)
            {
                var count = this.DictionaryOfObjects.Count;
                dictionaryOfObjects = new Dictionary<Fields, Fields>(count, this.DictionaryOfObjects.Comparer);
                foreach (var kvp in this.DictionaryOfObjects)
                {
                    // No need to check for null on the key, since it cannot possibly be null.
                    var value = kvp.Value;
                    dictionaryOfObjects.Add(kvp.Key.HCopy(), null == value ? null : value.HCopy());
                }
            }

            Dictionary<int, int> dictionaryOfPrimitives = null;
            if (null != this.DictionaryOfPrimitives)
            {
                int count = this.DictionaryOfPrimitives.Count;
                dictionaryOfPrimitives = new Dictionary<int, int>(count, this.DictionaryOfPrimitives.Comparer);
                foreach (var kvp in this.DictionaryOfPrimitives)
                {
                    dictionaryOfPrimitives.Add(kvp.Key, kvp.Value);
                }
            }

            return new HasDictionary
                {
                    DictionaryOfObjects = dictionaryOfObjects,
                    DictionaryOfPrimitives = dictionaryOfPrimitives
                };
        }

        public HasDictionary BCopy()
        {
            return new HasDictionary
                {
                    DictionaryOfObjects = this.DictionaryOfObjects,
                    DictionaryOfPrimitives = this.DictionaryOfPrimitives
                };
        }
    }
}