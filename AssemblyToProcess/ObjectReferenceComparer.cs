using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AssemblyToProcess
{
    internal sealed class ObjectReferenceComparer : EqualityComparer<object>
    {
        internal static readonly ObjectReferenceComparer Instance = new ObjectReferenceComparer();

        static ObjectReferenceComparer()
        {
            
        }

        private ObjectReferenceComparer()
        {
            
        }

        public override bool Equals(object first, object second)
        {
            return object.ReferenceEquals(first, second);
        }

        public override int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}