﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composite.Security;
using Composite.ConsoleEventSystem;
using Composite.Serialization;

namespace Composite.Actions
{

    public sealed class MessageBoxActionTokenActionExecutor : IActionExecutor
    {
        public FlowToken Execute(EntityToken entityToken, ActionToken actionToken, FlowControllerServicesContainer flowControllerServicesContainer)
        {
            MessageBoxActionToken messageBoxActionToken = (MessageBoxActionToken)actionToken;

            IManagementConsoleMessageService managementConsoleMessageService = flowControllerServicesContainer.GetService<IManagementConsoleMessageService>();

            managementConsoleMessageService.ShowMessage(messageBoxActionToken.DialogType, messageBoxActionToken.Title, messageBoxActionToken.Message);

            return null;
        }
    }



    [ActionExecutor(typeof(MessageBoxActionTokenActionExecutor))]
    public sealed class MessageBoxActionToken : ActionToken
    {
        private List<PermissionType> _permissionTypes;


        public MessageBoxActionToken(string title, string message, DialogType dialogType)
            :this(title, message, dialogType, new List<PermissionType>() { PermissionType.Add, PermissionType.Administrate, PermissionType.Approve, PermissionType.Delete, PermissionType.Edit, PermissionType.Publish, PermissionType.Read })
        {            
        }


        public MessageBoxActionToken(string title, string message, DialogType dialogType, List<PermissionType> permissionTypes)
        {
            _permissionTypes = permissionTypes;
            this.Title = title;
            this.Message = message;
            this.DialogType = dialogType;
        }


        public string Title { get; private set; }
        public string Message { get; private set; }
        public DialogType DialogType { get; private set; }


        public override IEnumerable<PermissionType> PermissionTypes
        {
            get { return _permissionTypes; }
        }


        public override string Serialize()
        {
            StringBuilder sb = new StringBuilder();

            StringConversionServices.SerializeKeyValuePair(sb, "Title", this.Title);
            StringConversionServices.SerializeKeyValuePair(sb, "Message", this.Message);
            StringConversionServices.SerializeKeyValuePair(sb, "DialogType", this.DialogType.ToString());
            StringConversionServices.SerializeKeyValuePair(sb, "PermissionTypes", _permissionTypes.SerializePermissionTypes());

            return sb.ToString();
        }


        public static ActionToken Deserialize(string serializedData)
        {
            Dictionary<string, string> dic = StringConversionServices.ParseKeyValueCollection(serializedData);

            return new MessageBoxActionToken
            (
                StringConversionServices.DeserializeValueString(dic["Title"]),
                StringConversionServices.DeserializeValueString(dic["Message"]),
                (DialogType)Enum.Parse(typeof(DialogType), StringConversionServices.DeserializeValueString(dic["DialogType"])),
                StringConversionServices.DeserializeValueString(dic["PermissionTypes"]).DesrializePermissionTypes().ToList()
            );
        }
    }

}
