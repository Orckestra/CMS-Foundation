﻿using System;
using System.Collections.Generic;
using System.Configuration;
using Composite.Collections.Generic;
using Composite.EventSystem;
using Composite.Security.Plugins.UserGroupPermissionDefinitionProvider;
using Composite.Security.Plugins.UserGroupPermissionDefinitionProvider.Runtime;


namespace Composite.Security.Foundation.PluginFacades
{
    internal static class UserGroupPermissionDefinitionProviderPluginFacade
    {
        private static ResourceLocker<Resources> _resourceLocker = new ResourceLocker<Resources>(new Resources(), Resources.DoInitializeResources);



        static UserGroupPermissionDefinitionProviderPluginFacade()
        {
            GlobalEventSystemFacade.SubscribeToFlushEvent(OnFlushEvent);
        }


        
        public static IEnumerable<UserGroupPermissionDefinition> AllUserGroupPermissionDefinitions
        {
            get
            {
                    return _resourceLocker.Resources.Plugin.AllUserGroupPermissionDefinitions;
            }
        }


        
        public static bool CanAlterDefinitions
        {
            get
            {
                    return _resourceLocker.Resources.Plugin.CanAlterDefinitions;
            }
        }



        public static IEnumerable<UserGroupPermissionDefinition> GetPermissionsByUserGroup(Guid userGroupId)
        {
                return _resourceLocker.Resources.Plugin.GetPermissionsByUserGroup(userGroupId);
        }


        
        public static void SetUserGroupPermissionDefinition(UserGroupPermissionDefinition userGroupPermissionDefinition)
        {
            if (userGroupPermissionDefinition == null) throw new ArgumentNullException("userGroupPermissionDefinition");
            if (string.IsNullOrEmpty(userGroupPermissionDefinition.SerializedEntityToken) == true) throw new ArgumentNullException("userGroupPermissionDefinition");
            if (userGroupPermissionDefinition.UserGroupId == Guid.Empty) throw new ArgumentNullException("userGroupPermissionDefinition");

                _resourceLocker.Resources.Plugin.SetUserGroupPermissionDefinition(userGroupPermissionDefinition);
        }



        public static void RemoveUserGroupPermissionDefinition(Guid userGroupID, string serializedEntityToken)
        {
            if (userGroupID == Guid.Empty) throw new ArgumentNullException("userGroupID");
            if (string.IsNullOrEmpty(serializedEntityToken) == true) throw new ArgumentNullException("serializedEntityToken");

                _resourceLocker.Resources.Plugin.RemoveUserGroupPermissionDefinition(userGroupID, serializedEntityToken);
        }



        private static void Flush()
        {
            _resourceLocker.ResetInitialization();
        }



        private static void OnFlushEvent(FlushEventArgs args)
        {
            Flush();
        }



        private static void HandleConfigurationError(Exception ex)
        {
            Flush();

            throw new ConfigurationErrorsException(string.Format("Failed to load the configuration section '{0}' from the configuration.", UserGroupPermissionDefinitionProviderSettings.SectionName), ex);
        }



        private sealed class Resources
        {
            public UserGroupPermissionDefinitionProviderFactory Factory { get; set; }
            public IUserGroupPermissionDefinitionProvider Plugin { get; set; }

            public static void DoInitializeResources(Resources resources)
            {
                try
                {
                    resources.Factory = new UserGroupPermissionDefinitionProviderFactory();
                    resources.Plugin = resources.Factory.CreateDefault();
                }
                catch (ArgumentException ex)
                {
                    HandleConfigurationError(ex);
                }
                catch (ConfigurationErrorsException ex)
                {
                    HandleConfigurationError(ex);
                }
            }
        }
    }
}
