using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

using Composite.Types;
using Composite.Security;
using Composite.Serialization;


namespace Composite.Actions
{
    public sealed class FlowHandle
    {
        private FlowToken _flowToken;

        private string _serializedData = null;



        public FlowHandle(FlowToken FlowToken)
        {
            _flowToken = FlowToken;
        }



        public FlowToken FlowToken
        {
            get { return _flowToken; }
        }



        public static FlowHandle Deserialize(string serializedFlowHandle)
        {
            Dictionary<string, string> dic = StringConversionServices.ParseKeyValueCollection(serializedFlowHandle);

            if ((dic.ContainsKey("_flowTokenType_") == false) ||
                (dic.ContainsKey("_flowToken_") == false))
            {
                throw new ArgumentException("The serializedFlowHandle is not a serialized flow handle", "serializedFlowHandle");
            }

            string flowTokenTypeString = StringConversionServices.DeserializeValueString(dic["_flowTokenType_"]);
            string flowTokenString = StringConversionServices.DeserializeValueString(dic["_flowToken_"]);

            Type flowTokenType = TypeManager.GetType(flowTokenTypeString);

            MethodInfo methodInfo = flowTokenType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new InvalidOperationException(string.Format("The flow token '{0}' is missing a public static Deserialize method taking a string as parameter and returning an '{1}'", flowTokenType, typeof(FlowToken)));
            }


            FlowToken flowToken;
            try
            {
                flowToken = (FlowToken)methodInfo.Invoke(null, new object[] { flowTokenString });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("The flow token '{0}' is missing a public static Deserialize method taking a string as parameter and returning an '{1}'", flowTokenType, typeof(FlowToken)), ex);
            }

            if (flowToken == null)
            {
                throw new InvalidOperationException(string.Format("public static Deserialize method taking a string as parameter and returning an '{0}' on the flow token '{1}' did not return an object", flowTokenType, typeof(FlowToken)));
            }

            return new FlowHandle(flowToken);
        }



        public string Serialize()
        {
            return ToString();
        }



        public override string ToString()
        {
            if (_serializedData == null)
            {
                StringBuilder sb = new StringBuilder();
                StringConversionServices.SerializeKeyValuePair(sb, "_flowTokenType_", TypeManager.SerializeType(_flowToken.GetType()));
                StringConversionServices.SerializeKeyValuePair(sb, "_flowToken_", _flowToken.Serialize());

                _serializedData = sb.ToString();
            }

            return _serializedData;
        }
    }
}
