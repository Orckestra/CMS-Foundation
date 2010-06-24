using System;
using System.Text;
using System.Collections.Generic;

using Composite.Serialization;


namespace Composite.Elements
{
    public sealed class ElementProviderHandle
    {
        private string _serializedData = null;


        public ElementProviderHandle(string providerName)
        {
            if (string.IsNullOrEmpty(providerName) == true) throw new ArgumentNullException("providerName");

            this.ProviderName = providerName;
        }



        public string ProviderName
        {
            get;
            private set;
        }



        public string Serialize()
        {
            return ToString();
        }



        public static ElementProviderHandle Deserialize(string serializedElementHandle)
        {
            Dictionary<string, string> dic = StringConversionServices.ParseKeyValueCollection(serializedElementHandle);

            if (dic.ContainsKey("_providerName_") == false)
            {
                throw new ArgumentException("The serializedElementProviderHandle is not a serialized element provider handle", "serializedElementProviderHandle");
            }

            string providerName = StringConversionServices.DeserializeValueString(dic["_providerName_"]);

            return new ElementProviderHandle(providerName);
        }



        public override string ToString()
        {
            if (_serializedData == null)
            {
                StringBuilder sb = new StringBuilder();
                StringConversionServices.SerializeKeyValuePair(sb, "_providerName_", ProviderName);

                _serializedData = sb.ToString();
            }

            return _serializedData;
        }
    }
}
