using System;
using System.Collections.Generic;
using AssemblyToProcess.Basic;

namespace AssemblyToProcess.Enumerables
{
    [Serializable] // For Clone.
    public class HasList
    {
        public List<Fields> ListOfObjects;

        public List<int> ListOfPrimitives;

        public HasList HCopy()
        {
            List<Fields> listOfObjects = null;
            if (null != this.ListOfObjects)
            {
                var count = this.ListOfObjects.Count;
                listOfObjects = new List<Fields>(count);
                for (int index = 0; index < count; index++)
                {
                    Fields listObject = this.ListOfObjects[index];
                    listOfObjects.Add(null == listObject ? null : listObject.HCopy());
                }
            }

            List<int> listOfPrimitives = null;
            if (null != this.ListOfPrimitives)
            {
                int count = this.ListOfPrimitives.Count;
                listOfPrimitives = new List<int>(count);
                for (int index = 0; index < count; index++)
                {
                    int listPrimitive = this.ListOfPrimitives[index];
                    listOfPrimitives.Add(listPrimitive);
                }
            }

            return new HasList
                {
                    ListOfObjects = listOfObjects,
                    ListOfPrimitives = listOfPrimitives
                };
        }

        public HasList BCopy()
        {
            return new HasList
                {
                    ListOfObjects = this.ListOfObjects,
                    ListOfPrimitives = this.ListOfPrimitives
                };
        }
    }
}