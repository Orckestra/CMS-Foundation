using System;
using Composite.Security;
using Composite.Types;


namespace Composite.StandardPlugins.Elements.ElementProviders.VirtualElementProvider
{
    [SecurityAncestorProvider(typeof(VirtualElementProviderSecurityAncestorProvider))]
    public sealed class VirtualElementProviderEntityToken : EntityToken
    {
        private string _source;
        private string _id;
        private string _type;



        public VirtualElementProviderEntityToken()
        {
            _type = TypeManager.SerializeType(this.GetType());
        }



        public VirtualElementProviderEntityToken(string source, string id)
            :this()
        {
            _source = source;
            _id = id;            
        }



        public override string Type
        {
            get { return _type; }
        }



        public override string Source
        {
            get { return _source; }
        }



        public override string Id
        {
            get { return _id; }
        }



        public override string Serialize()
        {
            return DoSerialize();
        }



        public static EntityToken Deserialize(string serializedEntityToken)
        {
            string type, source, id;

            EntityToken.DoDeserialize(serializedEntityToken, out type, out source, out id);

            return new VirtualElementProviderEntityToken(source, id);
        }
    }
}
