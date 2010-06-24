using System.Collections.Generic;
using System.Xml;


namespace Composite.Forms
{
    /// <summary>
    /// </summary>
    public class FormDefinition
    {
        public FormDefinition(XmlReader formMarkup, Dictionary<string, object> bindings)
        {
            this.FormMarkup = formMarkup;
            this.Bindings = bindings;
        }

        public XmlReader FormMarkup { get; private set; }
        public Dictionary<string, object> Bindings { get; private set; }
    }
}