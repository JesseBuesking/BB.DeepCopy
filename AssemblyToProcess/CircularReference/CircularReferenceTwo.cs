using System.Collections.Generic;

namespace AssemblyToProcess.CircularReference
{
    public class CircularReferenceTwo
    {
        public int Identifier;

        public CircularReferenceOne CR1;

        public CircularReferenceTwo HCopy()
        {
            var track = new Dictionary<object, object>(ObjectReferenceComparer.Instance);
            return this.HCircCopy(track);
        }

        public CircularReferenceTwo HCircCopy(Dictionary<object, object> track)
        {
            object clone;
            if (track.TryGetValue(this, out clone))
            {
                var c = clone as CircularReferenceTwo;
                if (null != c)
                    return c;
            }

            var myClone = new CircularReferenceTwo();
            track.Add(this, myClone);

            myClone.Identifier = this.Identifier;
            CircularReferenceOne cr = this.CR1;
            myClone.CR1 = null == cr ? null : cr.HCircCopy(track);

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