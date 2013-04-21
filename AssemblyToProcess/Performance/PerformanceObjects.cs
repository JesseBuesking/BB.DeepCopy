using System;

namespace AssemblyToProcess.Performance
{
    [Serializable]
    public class OneField
    {
        public int One;

        public OneField HCopy()
        {
            return new OneField
                {
                    One = this.One
                };
        }
    }

    [Serializable]
    public class OneProperty
    {
        public int One
        {
            get;
            set;
        }

        public OneProperty HCopy()
        {
            return new OneProperty
                {
                    One = this.One
                };
        }
    }

    [Serializable]
    public class FiveFields
    {
        public int One;

        public int Two;

        public int Three;

        public int Four;

        public int Five;

        public FiveFields HCopy()
        {
            return new FiveFields
                {
                    One = this.One,
                    Two = this.Two,
                    Three = this.Three,
                    Four = this.Four,
                    Five = this.Five
                };
        }
    }

    [Serializable]
    public class FiveProperties
    {
        public int One
        {
            get;
            set;
        }

        public int Two
        {
            get;
            set;
        }

        public int Three
        {
            get;
            set;
        }

        public int Four
        {
            get;
            set;
        }

        public int Five
        {
            get;
            set;
        }

        public FiveProperties HCopy()
        {
            return new FiveProperties
                {
                    One = this.One,
                    Two = this.Two,
                    Three = this.Three,
                    Four = this.Four,
                    Five = this.Five
                };
        }
    }

    [Serializable]
    public class TenFields
    {
        public int One;

        public int Two;

        public int Three;

        public int Four;

        public int Five;

        public int Six;

        public int Seven;

        public int Eight;

        public int Nine;

        public int Ten;

        public TenFields HCopy()
        {
            return new TenFields
                {
                    One = this.One,
                    Two = this.Two,
                    Three = this.Three,
                    Four = this.Four,
                    Five = this.Five,
                    Six = this.Six,
                    Seven = this.Seven,
                    Eight = this.Eight,
                    Nine = this.Nine,
                    Ten = this.Ten
                };
        }
    }

    [Serializable]
    public class TenProperties
    {
        public int One
        {
            get;
            set;
        }

        public int Two
        {
            get;
            set;
        }

        public int Three
        {
            get;
            set;
        }

        public int Four
        {
            get;
            set;
        }

        public int Five
        {
            get;
            set;
        }

        public int Six
        {
            get;
            set;
        }

        public int Seven
        {
            get;
            set;
        }

        public int Eight
        {
            get;
            set;
        }

        public int Nine
        {
            get;
            set;
        }

        public int Ten
        {
            get;
            set;
        }

        public TenProperties HCopy()
        {
            return new TenProperties
                {
                    One = this.One,
                    Two = this.Two,
                    Three = this.Three,
                    Four = this.Four,
                    Five = this.Five,
                    Six = this.Six,
                    Seven = this.Seven,
                    Eight = this.Eight,
                    Nine = this.Nine,
                    Ten = this.Ten
                };
        }
    }
}