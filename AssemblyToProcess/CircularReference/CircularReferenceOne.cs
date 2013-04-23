using System.Collections.Generic;

namespace AssemblyToProcess.CircularReference
{
    public class CircularReferenceOne
    {
        public int Identifier;

        public CircularReferenceTwo CR2;

        public CircularReferenceOne HCopy()
        {
            var track = new Dictionary<object, object>(ObjectReferenceComparer.Instance);
            return this.HCircCopy(track);
        }

        public CircularReferenceOne HCircCopy(Dictionary<object, object> track)
        {
            object clone;
            if (track.TryGetValue(this, out clone))
            {
                var c = clone as CircularReferenceOne;
                if (null != c)
                    return c;
            }

            var myClone = new CircularReferenceOne();
            track.Add(this, myClone);

            myClone.Identifier = this.Identifier;
            CircularReferenceTwo cr = this.CR2;
            myClone.CR2 = null == cr ? null : cr.HCircCopy(track);

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