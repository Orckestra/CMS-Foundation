using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Transactions;
using System.Workflow.Runtime;
using System.Xml.Linq;
using Composite.Actions;
using Composite.ConsoleEventSystem;
using Composite.Data;
using Composite.Data.DynamicTypes;
using Composite.Data.Types;
using Composite.Elements;
using Composite.Forms;
using Composite.Forms.DataServices;
using Composite.Forms.Flows;
using Composite.ResourceSystem;
using Composite.Security;
using Composite.Security.Cryptography;
using Composite.Transactions;
using Composite.Types;
using Composite.Users;
using Composite.Validation;
using Composite.Validation.ClientValidationRules;
using Composite.Workflow;
using Composite.Xml;
using Microsoft.Practices.EnterpriseLibrary.Validation;


namespace Composite.StandardPlugins.Elements.ElementProviders.UserElementProvider
{
    [EntityTokenLock()]
    [AllowPersistingWorkflow(WorkflowPersistingType.Idle)]
    public sealed partial class EditUserWorkflow : Composite.Workflow.Activities.FormsWorkflow
    {
        private static string UserBindingName { get { return "User"; } }



        public EditUserWorkflow()
        {
            InitializeComponent();
        }



        private void CheckActiveLanguagesExists(object sender, System.Workflow.Activities.ConditionalEventArgs e)
        {
            e.Result = DataLocalizationFacade.ActiveLocalizationCultures.Any();
        }



        private void initializeCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            DataEntityToken dataEntityToken = (DataEntityToken)this.EntityToken;

            IUser user = (IUser)dataEntityToken.Data;

            user.EncryptedPassword = user.EncryptedPassword.Decrypt();

            this.Bindings.Add(UserBindingName, user);

            CultureInfo userCulture = UserSettings.GetUserCultureInfo(user.Username);
            List<KeyValuePair> regionLanguageList = StringResourceSystemFacade.GetApplicationRegionAndLanguageList();
            this.Bindings.Add("CultureName", userCulture.Name);
            this.Bindings.Add("RegionLanguageList", regionLanguageList);

            if ((UserSettings.GetActiveLocaleCultureInfos(user.Username).Count() > 0) && (user.Username != UserSettings.Username))
            {
                this.Bindings.Add("ActiveLocaleName", UserSettings.GetCurrentActiveLocaleCultureInfo(user.Username).Name);
                this.Bindings.Add("ActiveLocaleList", DataLocalizationFacade.ActiveLocalizationCultures.ToDictionary(f => f.Name, f => StringResourceSystemFacade.GetString("Composite.Cultures", f.Name)));
            }

            Dictionary<string, List<ClientValidationRule>> clientValidationRules = new Dictionary<string, List<ClientValidationRule>>();
            clientValidationRules.Add("Username", ClientValidationRuleFacade.GetClientValidationRules(user, "Username"));
            clientValidationRules.Add("EncryptedPassword", ClientValidationRuleFacade.GetClientValidationRules(user, "EncryptedPassword"));
            clientValidationRules.Add("Group", ClientValidationRuleFacade.GetClientValidationRules(user, "Group"));


            IFormMarkupProvider markupProvider = new FormDefinitionFileMarkupProvider(@"\Administrative\EditUserStep1.xml");

            XDocument formDocument = XDocument.Load(markupProvider.GetReader());

            XElement bindingsElement = formDocument.Root.Element(DataTypeDescriptorFormsHelper.CmsNamespace + FormKeyTagNames.Bindings);
            XElement layoutElement = formDocument.Root.Element(DataTypeDescriptorFormsHelper.CmsNamespace + FormKeyTagNames.Layout);
            XElement tabPanelsElement = layoutElement.Element(DataTypeDescriptorFormsHelper.MainNamespace + "TabPanels");
            List<XElement> placeHolderElements = tabPanelsElement.Elements(DataTypeDescriptorFormsHelper.MainNamespace + "PlaceHolder").ToList();

            UpdateFormDefinitionWithActivePerspectives(user, bindingsElement, placeHolderElements[1]);
            //UpdateFormDefinitionWithGlobalPermissions(user, bindingsElement, placeHolderElements[1]);
            UpdateFormDefinitionWithUserGroups(user, bindingsElement, placeHolderElements[2]);

            if (DataLocalizationFacade.ActiveLocalizationCultures.Count() > 0)
            {
                UpdateFormDefinitionWithActiveLocales(user, bindingsElement, placeHolderElements[1]);
            }

            string formDefinition = formDocument.GetDocumentAsString();

            this.DeliverFormData(
                    user.Username,
                    StandardUiContainerTypes.Document,
                    formDefinition,
                    this.Bindings,
                    clientValidationRules
                );
        }



        private void UpdateFormDefinitionWithActivePerspectives(IUser user, XElement bindingsElement, XElement placeHolderElement)
        {
            List<string> serializedEntityToken = UserPerspectiveFacade.GetSerializedEntityTokens(user.Username).ToList();

            ActivePerspectiveFormsHelper helper = new ActivePerspectiveFormsHelper(
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.ActivePerspectiveFieldLabel"),
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.ActivePerspectiveMultiSelectLabel"),
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.ActivePerspectiveMultiSelectHelp")
                );

            bindingsElement.Add(helper.GetBindingsMarkup());
            placeHolderElement.Add(helper.GetFormMarkup());

            helper.UpdateWithNewBindings(this.Bindings, serializedEntityToken);
        }



        private void UpdateFormDefinitionWithGlobalPermissions(IUser user, XElement bindingsElement, XElement placeHolderElement)
        {
            GlobalPermissionsFormsHelper helper = new GlobalPermissionsFormsHelper(
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.GlobalPermissionsFieldLabel"),
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.GlobalPermissionsMultiSelectLabel"),
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.GlobalPermissionsMultiSelectHelp")
                );

            bindingsElement.Add(helper.GetBindingsMarkup());
            placeHolderElement.Add(helper.GetFormMarkup());

            EntityToken rootEntityToken = ElementFacade.GetRootsWithNoSecurity().Select(f => f.ElementHandle.EntityToken).Single();
            UserToken userToken = new UserToken(user.Username);
            IEnumerable<PermissionType> permissionTypes = PermissionTypeFacade.GetLocallyDefinedUserPermissionTypes(userToken, rootEntityToken);

            helper.UpdateWithNewBindings(this.Bindings, permissionTypes);
        }



        private void UpdateFormDefinitionWithActiveLocales(IUser user, XElement bindingsElement, XElement placeHolderElement)
        {
            ActiveLocalesFormsHelper helper = new ActiveLocalesFormsHelper(
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.ActiveLocalesFieldLabel"),
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.ActiveLocalesMultiSelectLabel"),
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.ActiveLocalesMultiSelectHelp")
                );

            bindingsElement.Add(helper.GetBindingsMarkup());
            placeHolderElement.Add(helper.GetFormMarkup());

            helper.UpdateWithNewBindings(this.Bindings, UserSettings.GetActiveLocaleCultureInfos(user.Username));
        }



        private void UpdateFormDefinitionWithUserGroups(IUser user, XElement bindingsElement, XElement placeHolderElement)
        {
            UserGroupsFormsHelper helper = new UserGroupsFormsHelper(
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.UserGroupsFieldLabel"),
                    StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.UserGroupsMultiSelectHelp")
                );

            bindingsElement.Add(helper.GetBindingsMarkup());
            placeHolderElement.Add(helper.GetFormMarkup());

            List<Guid> relations = DataFacade.GetData<IUserUserGroupRelation>(f => f.UserId == user.Id).Select(f => f.UserGroupId).ToList();

            helper.UpdateWithNewBindings(this.Bindings, relations);
        }



        private void saveCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            IUser user = this.GetBinding<IUser>(UserBindingName);

            bool userValidated = true;

            ValidationResults validationResults = ValidationFacade.Validate(user);

            foreach (ValidationResult result in validationResults)
            {
                this.ShowFieldMessage(string.Format("{0}.{1}", UserBindingName, result.Key), result.Message);
                userValidated = false;
            }


            List<CultureInfo> newActiveLocales = ActiveLocalesFormsHelper.GetSelectedLocalesTypes(this.Bindings).ToList();
            List<CultureInfo> currentActiveLocales = null;
            CultureInfo selectedActiveLocal = null;

            if (newActiveLocales.Count > 0)
            {
                currentActiveLocales = UserSettings.GetActiveLocaleCultureInfos(user.Username).ToList();


                string selectedActiveLocaleName = (user.Username != UserSettings.Username ?
                    this.GetBinding<string>("ActiveLocaleName") :
                    UserSettings.ActiveLocaleCultureInfo.ToString());

                if (selectedActiveLocaleName != null)
                {
                    selectedActiveLocal = CultureInfo.CreateSpecificCulture(selectedActiveLocaleName);
                    if (newActiveLocales.Contains(selectedActiveLocal) == false)
                    {
                        if (user.Username != UserSettings.Username)
                        {
                            this.ShowFieldMessage("ActiveLocaleName", StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.ActiveLocaleNotChecked"));
                        }
                        else
                        {
                            this.ShowFieldMessage("ActiveLocalesFormsHelper_Selected", StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.NoActiveLocaleSelected"));
                        }
                        userValidated = false;
                    }
                }
            }
            else
            {
                this.ShowFieldMessage("ActiveLocalesFormsHelper_Selected", StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.NoActiveLocaleSelected"));
                userValidated = false;
            }


            if (userValidated == true)
            {
                UpdateTreeRefresher updateTreeRefresher = this.CreateUpdateTreeRefresher(this.EntityToken);

                user.EncryptedPassword = user.EncryptedPassword.Encrypt();

                bool reloadUsersConsoles = false;

                using (TransactionScope transactionScope = TransactionsFacade.CreateNewScope())
                {
                    DataFacade.Update(user);

                    string cultureName = this.GetBinding<string>("CultureName");
                    UserSettings.SetUserCultureInfo(user.Username, CultureInfo.CreateSpecificCulture(cultureName));


                    IEnumerable<string> newSerializedEntityToken = ActivePerspectiveFormsHelper.GetSelectedSerializedEntityTokens(this.Bindings);
                    IEnumerable<string> existingSerializedEntityToken = UserPerspectiveFacade.GetSerializedEntityTokens(user.Username);

                    int intersectCount = existingSerializedEntityToken.Intersect(newSerializedEntityToken).Count();
                    if ((intersectCount != newSerializedEntityToken.Count()) || (intersectCount != existingSerializedEntityToken.Count()))
                    {
                        UserPerspectiveFacade.SetSerializedEntityTokens(user.Username, ActivePerspectiveFormsHelper.GetSelectedSerializedEntityTokens(this.Bindings));

                        if (UserSettings.Username == user.Username)
                        {
                            reloadUsersConsoles = true;
                        }
                    }

                    UserToken userToken = new UserToken(user.Username);
                    EntityToken rootEntityToken = ElementFacade.GetRootsWithNoSecurity().Select(f => f.ElementHandle.EntityToken).Single();

                    /*IEnumerable<PermissionType> oldPermissionTypes = PermissionTypeFacade.GetLocallyDefinedUserPermissionTypes(userToken, rootEntityToken);
                    IEnumerable<PermissionType> newPermissionTypes = GlobalPermissionsFormsHelper.GetSelectedPermissionTypes(this.Bindings);

                    if ((user.Username == UserSettings.Username) &&
                        (oldPermissionTypes.Contains(PermissionType.Administrate) == true) &&
                        (newPermissionTypes.Contains(PermissionType.Administrate) == false))
                    {
                        newPermissionTypes = newPermissionTypes.Concat(new PermissionType[] { PermissionType.Administrate });
                        this.ShowFieldMessage(GlobalPermissionsFormsHelper.GetFieldBindingPath(), StringResourceSystemFacade.GetString("Composite.Management", "Website.Forms.Administrative.EditUserStep1.GlobalPermissions.IgnoredOwnAdministrativeRemoval"));
                    }

                    UserPermissionDefinition userPermissionDefinition =
                        new ConstructorBasedUserPermissionDefinition(
                            user.Username,
                            newPermissionTypes,
                            EntityTokenSerializer.Serialize(rootEntityToken)
                        );

                    PermissionTypeFacade.SetUserPermissionDefinition(userPermissionDefinition);*/


                    if (DataLocalizationFacade.ActiveLocalizationCultures.Count() > 0)
                    {
                        foreach (CultureInfo cultureInfo in newActiveLocales)
                        {
                            if (currentActiveLocales.Contains(cultureInfo) == false)
                            {
                                UserSettings.AddActiveLocaleCultureInfo(user.Username, cultureInfo);
                            }
                        }

                        foreach (CultureInfo cultureInfo in currentActiveLocales)
                        {
                            if (newActiveLocales.Contains(cultureInfo) == false)
                            {
                                UserSettings.RemoveActiveLocaleCultureInfo(user.Username, cultureInfo);
                            }
                        }

                        if (selectedActiveLocal != null)
                        {
                            if (UserSettings.GetCurrentActiveLocaleCultureInfo(user.Username).Equals(selectedActiveLocal) == false)
                            {
                                reloadUsersConsoles = true;
                            }

                            UserSettings.SetCurrentActiveLocaleCultureInfo(user.Username, selectedActiveLocal);
                        }
                        else if (UserSettings.GetActiveLocaleCultureInfos(user.Username).Count() > 0)
                        {
                            UserSettings.SetCurrentActiveLocaleCultureInfo(user.Username, UserSettings.GetActiveLocaleCultureInfos(user.Username).First());
                        }
                    }


                    List<IUserUserGroupRelation> oldRelations = DataFacade.GetData<IUserUserGroupRelation>(f => f.UserId == user.Id).ToList();
                    List<Guid> newUserGroupIds = UserGroupsFormsHelper.GetSelectedUserGroupIds(this.Bindings);

                    IEnumerable<IUserUserGroupRelation> deleteRelations =
                        from r in oldRelations
                        where newUserGroupIds.Contains(r.UserGroupId) == false
                        select r;

                    DataFacade.Delete(deleteRelations);


                    foreach (Guid newUserGroupId in newUserGroupIds)
                    {
                        Guid groupId = newUserGroupId;
                        if (oldRelations.Where(f => f.UserGroupId == groupId).Any() == true) continue;

                        IUserUserGroupRelation userUserGroupRelation = DataFacade.BuildNew<IUserUserGroupRelation>();
                        userUserGroupRelation.UserId = user.Id;
                        userUserGroupRelation.UserGroupId = newUserGroupId;

                        DataFacade.AddNew(userUserGroupRelation);
                    }

                    transactionScope.Complete();
                }

                if (reloadUsersConsoles == true)
                {
                    foreach (string consoleId in ConsoleFacade.GetConsoleIdsByUsername(user.Username))
                    {
                        ConsoleMessageQueueFacade.Enqueue(new RebootConsoleMessageQueueItem(), consoleId);
                    }
                }

                SetSaveStatus(true);
                updateTreeRefresher.PostRefreshMesseges(user.GetDataEntityToken());
            }
        }



        private void IsUserLoggedOn(object sender, System.Workflow.Activities.ConditionalEventArgs e)
        {
            DataEntityToken dataEntityToken = (DataEntityToken)this.EntityToken;

            IUser user = (IUser)dataEntityToken.Data;

            string selectedActiveLocaleName = (user.Username != UserSettings.Username ?
                    this.GetBinding<string>("ActiveLocaleName") :
                    UserSettings.ActiveLocaleCultureInfo.ToString());

            if (selectedActiveLocaleName != null)
            {
                CultureInfo selectedActiveLocale = CultureInfo.CreateSpecificCulture(selectedActiveLocaleName);

                if (UserSettings.GetCurrentActiveLocaleCultureInfo(user.Username).Equals(selectedActiveLocale) == false)
                {
                    e.Result = ConsoleFacade.GetConsoleIdsByUsername(user.Username).Count() > 0;
                    return;
                }
            }

            e.Result = false;
        }



        private void IsSameUser(object sender, System.Workflow.Activities.ConditionalEventArgs e)
        {
            DataEntityToken dataEntityToken = (DataEntityToken)this.EntityToken;

            IUser user = (IUser)dataEntityToken.Data;

            e.Result = user.Username == UserSettings.Username;
        }



        private void MissingActiveLanguageCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            FlowControllerServicesContainer flowControllerServicesContainer = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
            var managementConsoleMessageService = flowControllerServicesContainer.GetService<IManagementConsoleMessageService>();

            managementConsoleMessageService.ShowMessage(
                DialogType.Message,
                StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.MissingActiveLanguageTitle"),
                StringResourceSystemFacade.GetString("Composite.Management", "UserElementProvider.MissingActiveLanguageMessage"));
        }
    }
}
