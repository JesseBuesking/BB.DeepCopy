using System;

namespace AssemblyToProcess.Arrays
{
    [Serializable] // For Clone.
    public class ArrayOfPrimitives
    {
        public int[] IntsArray;

        public ArrayOfPrimitives HCopy()
        {
            int[] intsArray = null;
            if (null != this.IntsArray)
            {
                int length = this.IntsArray.Length;
                intsArray = new int[length];
                for (int i = 0; i < length; i++)
                    intsArray[i] = this.IntsArray[i];
            }

            return new ArrayOfPrimitives
                {
                    IntsArray = intsArray
                };
        }

        public ArrayOfPrimitives BCopy()
        {
            return new ArrayOfPrimitives
                {
                    IntsArray = this.IntsArray
                };
        }
    }
}