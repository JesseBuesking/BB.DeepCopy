using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AssemblyToProcess.CircularReference
{
    public class CircularReferenceOne
    {
        public int Identifier;

        public CircularReferenceTwo CR2;

        public CircularReferenceOne HCopy()
        {
            var track = new Dictionary<int, object>();
            return this.HCircCopy(this, track);
        }

        public CircularReferenceOne HCircCopy(object obj, Dictionary<int, object> track)
        {
            object clone;
            if (track.TryGetValue(RuntimeHelpers.GetHashCode(obj), out clone))
            {
                var c = clone as CircularReferenceOne;
                if (null != c)
                    return c;
            }

            var myClone = new CircularReferenceOne();
            track.Add(RuntimeHelpers.GetHashCode(obj), myClone);

            myClone.Identifier = this.Identifier;
            CircularReferenceTwo cr = this.CR2;
            myClone.CR2 = null == cr ? null : cr.HCircCopy(cr, track);

            return myClone;
        }

        public CircularReferenceOne BCopy()
        {
            return new CircularReferenceOne
                {
                    Identifier = this.Identifier,
                    CR2 = this.CR2
                };
        }
    }
}