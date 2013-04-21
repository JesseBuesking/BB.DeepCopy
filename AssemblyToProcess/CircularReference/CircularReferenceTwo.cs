using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AssemblyToProcess.CircularReference
{
    public class CircularReferenceTwo
    {
        public int Identifier;

        public CircularReferenceOne CR1;

        public CircularReferenceTwo HCopy()
        {
            var track = new Dictionary<int, object>();
            return this.HCircCopy(this, track);
        }

        public CircularReferenceTwo HCircCopy(object obj, Dictionary<int, object> track)
        {
            object clone;
            if (track.TryGetValue(RuntimeHelpers.GetHashCode(obj), out clone))
            {
                var c = clone as CircularReferenceTwo;
                if (null != c)
                    return c;
            }

            var myClone = new CircularReferenceTwo();
            track.Add(RuntimeHelpers.GetHashCode(obj), myClone);

            myClone.Identifier = this.Identifier;
            CircularReferenceOne cr = this.CR1;
            myClone.CR1 = null == cr ? null : cr.HCircCopy(cr, track);

            return myClone;
        }

        public CircularReferenceTwo BCopy()
        {
            return new CircularReferenceTwo
                {
                    Identifier = this.Identifier,
                    CR1 = this.CR1
                };
        }
    }
}
