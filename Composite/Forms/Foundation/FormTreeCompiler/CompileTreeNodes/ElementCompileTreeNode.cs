using System.Collections.Generic;


namespace Composite.Forms.Foundation.FormTreeCompiler.CompileTreeNodes
{
    public class ElementCompileTreeNode : CompileTreeNode
    {
        private object _producer = null;
        private Dictionary<int, List<PropertyCompileTreeNode>> _addedProperties = new Dictionary<int, List<PropertyCompileTreeNode>>();

        public ElementCompileTreeNode(XmlSourceNodeInformation sourceInformation)
            : base(sourceInformation)
        {
        }

        public object Producer
        {
            get { return _producer; }
            set { _producer = value; }
        }

        public Dictionary<int, List<PropertyCompileTreeNode>> AddedProperties
        {
            get { return _addedProperties; }
        }
    }
}
