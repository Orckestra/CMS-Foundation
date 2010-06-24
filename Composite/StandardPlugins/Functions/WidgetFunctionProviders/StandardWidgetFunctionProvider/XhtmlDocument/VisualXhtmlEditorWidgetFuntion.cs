using System;
using System.Xml.Linq;
using System.Collections.Generic;

using Composite.Functions;
using Composite.StandardPlugins.Functions.WidgetFunctionProviders.StandardWidgetFunctionProvider.Foundation;
using Composite.Types;
using Composite.Xml;


namespace Composite.StandardPlugins.Functions.WidgetFunctionProviders.StandardWidgetFunctionProvider.XhtmlDocument
{
	public sealed class VisualXhtmlEditorFuntion : CompositeWidgetFunctionBase
    {
        public static IEnumerable<Type> GetOptions(object typeManagerTypeName)
        {
            yield return TypeManager.GetType((string)typeManagerTypeName);
        }

        private const string _functionName = "VisualXhtmlEditor";
        public const string CompositeName = CompositeWidgetFunctionBase.CommonNamespace + ".XhtmlDocument." + _functionName;

        public const string ClassConfigurationNameParameterName = "ClassConfigurationName";
        public const string EmbedableFieldTypeParameterName = "EmbedableFieldsType";


        public VisualXhtmlEditorFuntion(EntityTokenFactory entityTokenFactory)
            : base(CompositeName, typeof(Composite.Xml.XhtmlDocument), entityTokenFactory)
        {
            SetParameterProfiles("common");
        }




        public override XElement GetWidgetMarkup(ParameterList parameters, string label, HelpDefinition help, string bindingSourceName)
        {
            XElement element = base.BuildBasicWidgetMarkup("InlineXhtmlEditor", "Xhtml", label, help, bindingSourceName);
            element.Add(new XAttribute("ClassConfigurationName", parameters.GetParameter<string>(VisualXhtmlEditorFuntion.ClassConfigurationNameParameterName)));
            
            Type embedableFieldType = parameters.GetParameter<Type>(VisualXhtmlEditorFuntion.EmbedableFieldTypeParameterName);
            if (embedableFieldType!=null)
            {
                XNamespace f = Namespaces.BindingFormsStdFuncLib10;

                element.Add(
                    new XElement(element.Name.Namespace + "InlineXhtmlEditor.EmbedableFieldsTypes",
                        new XElement(f + "StaticMethodCall",
                           new XAttribute("Type", TypeManager.SerializeType(this.GetType())),
                           new XAttribute("Parameters", TypeManager.SerializeType(embedableFieldType)),
                           new XAttribute("Method", "GetOptions"))));

            }

            return element;
        }


        private void SetParameterProfiles(string classConfigurationName)
        {
            ParameterProfile classConfigNamePP =
                new ParameterProfile(VisualXhtmlEditorFuntion.ClassConfigurationNameParameterName,
                    typeof(string), false,
                    new ConstantValueProvider(classConfigurationName), StandardWidgetFunctions.TextBoxWidget, null,
                    "Class configuration name", new HelpDefinition("The visual editor can be configured to offer the editor a special set of class names for formatting xhtml elements. The default value is '" + classConfigurationName + "'"));

            base.AddParameterProfile(classConfigNamePP);

            ParameterProfile typeNamePP =
                new ParameterProfile(VisualXhtmlEditorFuntion.EmbedableFieldTypeParameterName,
                    typeof(Type), false,
                    new ConstantValueProvider(null), StandardWidgetFunctions.DataTypeSelectorWidget, null,
                    "Embedable fields, Data type", new HelpDefinition("If a data type is selected, fields from this type can be inserted into the xhtml."));

            base.AddParameterProfile(typeNamePP);

        }

    }
}
