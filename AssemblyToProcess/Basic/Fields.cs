using System;

namespace AssemblyToProcess.Basic
{
    [Serializable] // For Clone.
    public class Fields
    {
        private string _privateField;

        public int PublicField;

        public Fields()
        {
            
        }

        public Fields(string priv)
        {
            this._privateField = priv;
        }

        public Fields HCopy()
        {
            return new Fields
                {
                    _privateField = this._privateField,
                    PublicField = this.PublicField
                };
        }

        public override int GetHashCode()
        {
            return (null == _privateField ? 0 : _privateField.GetHashCode()) ^
                (null == PublicField ? 0 : PublicField.GetHashCode());
        }
    }
}