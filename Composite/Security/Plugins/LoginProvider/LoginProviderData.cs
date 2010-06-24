using System;

using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;

namespace Composite.Security.Plugins.LoginProvider
{
    public class LoginProviderData : NameTypeConfigurationElement
    {
        public LoginProviderData() : base("Unnamed", typeof(ILoginProvider)) { }

        public LoginProviderData(string name, Type type) : base(name, type) { }
    }
}
