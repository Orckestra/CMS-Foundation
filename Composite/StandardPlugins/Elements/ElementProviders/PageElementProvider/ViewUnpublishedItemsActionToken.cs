﻿using System.Collections.Generic;

using Composite.Actions;
using Composite.Security;
using Composite.StandardPlugins.Elements.ElementProviders.VirtualElementProvider;
using Composite.ConsoleEventSystem;
using Composite.WebClient;
using System.Web;
using Composite.ResourceSystem;

namespace Composite.StandardPlugins.Elements.ElementProviders.PageElementProvider
{
    [ActionExecutor(typeof(ViewUnpublishedItemsActionExecutor))]
    public sealed class ViewUnpublishedItemsActionToken : ActionToken
    {
        private static IEnumerable<PermissionType> _permissionType = new PermissionType[] { PermissionType.Read };

        public ViewUnpublishedItemsActionToken()
        {
        }

        public override IEnumerable<PermissionType> PermissionTypes
        {
            get { return _permissionType; }
        }

        public override string Serialize()
        {
            return "ViewUnpublishedPageItems";
        }


        public static ActionToken Deserialize(string serializedData)
        {
            return new ViewUnpublishedItemsActionToken();
        }
    }




    public sealed class ViewUnpublishedItemsActionExecutor : Composite.Actions.IActionExecutor
    {
        public FlowToken Execute(EntityToken entityToken, ActionToken actionToken, FlowControllerServicesContainer flowControllerServicesContainer)
        {
            //string documentTitle = "Unpublished Pages and Page Folder Data";
            //string description = "The list below display pages and page data which are currently being edited or are ready to be approved / published.";
            string documentTitle = StringResourceSystemFacade.GetString("Composite.StandardPlugins.PageElementProvider", "PageElementProvider.ViewUnpublishedItems-document-title");
            string description = StringResourceSystemFacade.GetString("Composite.StandardPlugins.PageElementProvider", "PageElementProvider.ViewUnpublishedItems-document-description");
            string emptyLabel = StringResourceSystemFacade.GetString("Composite.StandardPlugins.GeneratedDataTypesElementProvider", "ViewUnpublishedItems-document-empty-label");
            string url = string.Format("{0}?showpagedata=true&title={1}&description={2}&emptyLabel={3}",
                UrlUtils.ResolveAdminUrl(string.Format("content/views/publishworkflowstatus/ViewUnpublishedItems.aspx")),
                HttpUtility.UrlEncodeUnicode(documentTitle),
                HttpUtility.UrlEncodeUnicode(description),
                HttpUtility.UrlEncodeUnicode(emptyLabel));

            IManagementConsoleMessageService consoleServices = flowControllerServicesContainer.GetService<IManagementConsoleMessageService>();
            OpenViewMessageQueueItem openViewMsg = new OpenViewMessageQueueItem
            {
                EntityToken = EntityTokenSerializer.Serialize(entityToken, true),
                ViewId = "ViewUnpublishedPageItems",
                Label = documentTitle,
                Url = url,
                ViewType = ViewType.Main
            };

            ConsoleMessageQueueFacade.Enqueue(openViewMsg, consoleServices.CurrentConsoleId);

            return null;
        }
    }


}
