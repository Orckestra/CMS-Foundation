using System;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.ObjectBuilder;
using Microsoft.Practices.ObjectBuilder;


namespace Composite.ResourceSystem.Plugins.ResourceProvider
{
    [Assembler(typeof(NonConfigurableResourceProviderAssembler))]
    public class NonConfigurableResourceProvider : ResourceProviderData
    {
    }


    public sealed class NonConfigurableResourceProviderAssembler : IAssembler<IResourceProvider, ResourceProviderData>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
        public IResourceProvider Assemble(IBuilderContext context, ResourceProviderData objectConfiguration, IConfigurationSource configurationSource, ConfigurationReflectionCache reflectionCache)
        {
            return (IResourceProvider)Activator.CreateInstance(objectConfiguration.Type);
        }
    }
}
