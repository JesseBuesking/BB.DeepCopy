using System;

namespace Tests
{
    /// <summary>
    /// Assists in getting objects off of types.
    /// </summary>
    public class ObjectGetter
    {
        private readonly Type _type;

        private readonly dynamic _dyn;

        public ObjectGetter(Type type, dynamic dyn)
        {
            this._type = type;
            this._dyn = dyn;
        }

        public object FieldValue(string name)
        {
            return this._type.GetField(name).GetValue(this._dyn);
        }

        public object PrivateFieldValue(string name)
        {
            return PropertyExtensions.GetPrivateFieldValue<object>(this._dyn, name);
        }

        public object PropertyValue(string name)
        {
            return this._type.GetProperty(name).GetValue(this._dyn);
        }

        public object PrivatePropertyValue(string name)
        {
            return PropertyExtensions.GetPrivatePropertyValue<object>(this._dyn, name);
        }
    }
}