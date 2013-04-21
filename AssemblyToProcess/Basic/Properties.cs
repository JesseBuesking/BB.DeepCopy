using System;

namespace AssemblyToProcess.Basic
{
    [Serializable]
    public class Properties
    {
        public double BackedProperty
        {
            get { return this._backedProperty; }
            set { this._backedProperty = value; }
        }

        private double _backedProperty;

        private int PrivateProperty
        {
            get;
            set;
        }

        public string PublicProperty
        {
            get;
            set;
        }

        public Properties HCopy()
        {
            return new Properties
                {
                    _backedProperty = this._backedProperty,
                    PrivateProperty = this.PrivateProperty,
                    PublicProperty = this.PublicProperty
                };
        }
    }
}