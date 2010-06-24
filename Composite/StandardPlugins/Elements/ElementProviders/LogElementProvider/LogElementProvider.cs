﻿using System.Collections.Generic;
using Composite.Elements;
using Composite.Elements.Plugins.ElementProvider;
using Composite.ResourceSystem;
using Composite.ResourceSystem.Icons;
using Composite.Security;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.ObjectBuilder;


namespace Composite.StandardPlugins.Elements.ElementProviders.LogElementProvider
{
    [ConfigurationElementType(typeof(LogElementProviderData))]
    public sealed class LogElementProvider : IHooklessElementProvider
    {
        private ElementProviderContext _elementProviderContext;


        public static ResourceHandle RootFolderIcon { get { return GetIconHandle("log-folder-closed"); } }
        public static ResourceHandle ShowLogIcon { get { return GetIconHandle("log-showlog"); } }
        private static ResourceHandle GetIconHandle(string name) { return new ResourceHandle(BuildInIconProviderName.ProviderName, name); }

        private static readonly ActionGroup PrimaryActionGroup = new ActionGroup(ActionGroupPriority.PrimaryHigh);

        public ElementProviderContext Context
        {
            set { _elementProviderContext = value; }
        }



        public IEnumerable<Element> GetRoots(SearchToken seachToken)
        {
            Element element = new Element(_elementProviderContext.CreateElementHandle(new LogElementProviderEntityToken(LogElementProviderEntityToken.RootFolderId)))
            {
                VisualData = new ElementVisualizedData
                {
                    Label = StringResourceSystemFacade.GetString("Composite.Management", "LogElementProvider.RootLabel"),
                    ToolTip = StringResourceSystemFacade.GetString("Composite.Management", "LogElementProvider.RootLabelToolTip"),
                    HasChildren = false,
                    Icon = LogElementProvider.RootFolderIcon,
                    OpenedIcon = LogElementProvider.RootFolderIcon
                }
            };



            element.AddAction(new ElementAction(new ActionHandle(new ShowLogActionToken()))
            {
                VisualData = new ActionVisualizedData
                {
                    Label = StringResourceSystemFacade.GetString("Composite.Management", "LogElementProvider.ShowLogLabel"),
                    ToolTip = StringResourceSystemFacade.GetString("Composite.Management", "LogElementProvider.ShowLogToolTip"),
                    Icon = LogElementProvider.ShowLogIcon,
                    Disabled = false,
                    ActionLocation = new ActionLocation
                    {
                        ActionType = ActionType.Other,
                        IsInFolder = false,
                        IsInToolbar = true,
                        ActionGroup = PrimaryActionGroup
                    }
                }
            });


            return new List<Element> { element };
        }



        public IEnumerable<Element> GetChildren(EntityToken entityToken, SearchToken seachToken)
        {
            return new List<Element>();
        }
    }




    [Assembler(typeof(NonConfigurableHooklessElementProviderAssembler))]
    public sealed class LogElementProviderData : HooklessElementProviderData
    {
    }
}
