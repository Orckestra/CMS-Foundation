using Composite.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;


namespace Composite.Forms.Plugins.FunctionFactory
{
    [ConfigurationElementType(typeof(NonConfigurableFunctionFactory))]
    public class FunctionFactoryData : NameTypeManagerTypeConfigurationElement
    {
    }
}
