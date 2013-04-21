using System;
using AssemblyToProcess.Basic;

namespace AssemblyToProcess.Arrays
{
    [Serializable] // For Clone.
    public class ArrayOfObjects
    {
        public Fields[] FieldsArray;

        public ArrayOfObjects HCopy()
        {
            Fields[] fieldsArray = null;
            if (null != this.FieldsArray)
            {
                int length = this.FieldsArray.Length;
                fieldsArray = new Fields[length];
                for (int i = 0; i < length; i++)
                    fieldsArray[i] = null == this.FieldsArray[i] ? null : this.FieldsArray[i].HCopy();
            }

            return new ArrayOfObjects
                {
                    FieldsArray = fieldsArray
                };
        }

        public ArrayOfObjects BCopy()
        {
            return new ArrayOfObjects
                {
                    FieldsArray = this.FieldsArray
                };
        }
    }
}