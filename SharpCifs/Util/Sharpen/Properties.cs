using System.IO;

namespace SharpCifs.Util.Sharpen
{
    internal class Properties
    {
        protected Hashtable _properties;

        public Properties()
        {
            this._properties = new Hashtable();
        }

        public Properties(Properties defaultProp) : this()
        {
            this.PutAll(defaultProp._properties);
        }

        public void PutAll(Hashtable properties)
        {
            foreach (var key in properties.Keys)
            {
                this._properties.Put(key, properties[key]);
            }
        }

        public void SetProperty(object key, object value)
        {
            this._properties.Put(key, value);
        }

        public object GetProperty(object key)
        {
            return this._properties.Keys.Contains(key)
                ? this._properties[key]
                : null;
        }

        public object GetProperty(object key, object def)
        {
            return this._properties.Get(key) ?? def;
        }
    }
}