using System.Xml.Linq;
using System;
using Composite.Functions.Foundation;
using Composite.ResourceSystem;


namespace Composite.Functions
{
	public sealed class HelpDefinition
	{
        public HelpDefinition GetLocalized()
        {
            if (this.HelpText.StartsWith("${"))
            {
                return new HelpDefinition(StringResourceSystemFacade.ParseString(this.HelpText));
            }
            else
            {
                return new HelpDefinition(this.HelpText);
            }
        }

        public HelpDefinition(string helpText)
        {
            this.HelpText = helpText;
        }


        public string HelpText
        {
            get;
            private set;
        }



        public XElement Serialize()
        {
            XElement element = XElement.Parse(string.Format(@"<f:{0} xmlns:f=""{1}"" />", FunctionTreeConfigurationNames.HelpDefinitionTagName, FunctionTreeConfigurationNames.NamespaceName));

            element.Add(new XAttribute(FunctionTreeConfigurationNames.HelpTextAttributeName, this.HelpText));

            return element;
        }


        public static HelpDefinition Deserialize(XElement serializedHelpDefinition)
        {
            if (serializedHelpDefinition == null) throw new ArgumentNullException("serializedHelpDefinition");

            if (serializedHelpDefinition.Name.LocalName != FunctionTreeConfigurationNames.HelpDefinitionTagName) throw new ArgumentException("Wrong serialized format");

            XAttribute helpTextAttribute = serializedHelpDefinition.Attribute(FunctionTreeConfigurationNames.HelpTextAttributeName);
            if (helpTextAttribute == null) throw new ArgumentException("Wrong serialized format");

            return new HelpDefinition(helpTextAttribute.Value);
        }
	}
}
