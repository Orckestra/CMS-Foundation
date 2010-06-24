using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Transactions;
using System.Workflow.Activities;
using Composite.Actions;
using Composite.ConsoleEventSystem;
using Composite.Data;
using Composite.Data.DynamicTypes;
using Composite.Data.ProcessControlled;
using Composite.Data.ProcessControlled.ProcessControllers.GenericPublishProcessController;
using Composite.ResourceSystem;
using Composite.Transactions;
using Composite.Types;
using Composite.Users;
using Composite.Linq;
using Composite.Workflow;
using System.Reflection;


namespace Composite.StandardPlugins.Elements.ElementProviders.GeneratedDataTypesElementProvider
{
    public sealed partial class LocalizeDataWorkflow : Composite.Workflow.Activities.FormsWorkflow
    {
        public LocalizeDataWorkflow()
        {
            InitializeComponent();
        }



        private void ValidateLocalizeProcess(object sender, ConditionalEventArgs e)
        {
            DataEntityToken dataEntityToken = (DataEntityToken)this.EntityToken;
            ILocalizedControlled data = dataEntityToken.Data as ILocalizedControlled;

            IEnumerable<ReferenceFailingPropertyInfo> referenceFailingPropertyInfos = DataLocalizationFacade.GetReferencingLocalizeFailingProperties(data).Evaluate();

            if (referenceFailingPropertyInfos.Where(f => f.OptionalReferenceWithValue == false).Any() == true)
            {
                List<string> row = new List<string>();

                row.Add(StringResourceSystemFacade.GetString("Composite.StandardPlugins.GeneratedDataTypesElementProvider", "LocalizeDataWorkflow.ShowError.Description"));

                foreach (ReferenceFailingPropertyInfo referenceFailingPropertyInfo in referenceFailingPropertyInfos.Where(f => f.OptionalReferenceWithValue == false))
                {
                    row.Add(string.Format(StringResourceSystemFacade.GetString("Composite.StandardPlugins.GeneratedDataTypesElementProvider", "LocalizeDataWorkflow.ShowError.FieldErrorFormat"), referenceFailingPropertyInfo.DataFieldDescriptor.Name, referenceFailingPropertyInfo.ReferencedType.GetTypeTitle(), referenceFailingPropertyInfo.OriginLocaleDataValue.GetLabel()));
                }

                List<List<string>> rows = new List<List<string>> { row };

                this.UpdateBinding("ErrorHeader", new List<string> { "Fields" });
                this.UpdateBinding("Errors", rows);

                e.Result = false;
            }
            else
            {
                e.Result = true;
            }
        }



        private void localizeCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            DataEntityToken dataEntityToken = (DataEntityToken)this.EntityToken;
            ILocalizedControlled data = dataEntityToken.Data as ILocalizedControlled;

            CultureInfo targetCultureInfo = UserSettings.ActiveLocaleCultureInfo;

            if (ExistsInLocale(data, targetCultureInfo))
            {
                string title = StringResourceSystemFacade.GetString("Composite.StandardPlugins.GeneratedDataTypesElementProvider",
                                                                    "LocalizeDataWorkflow.ShowError.LayoutLabel");

                string description = StringResourceSystemFacade.GetString("Composite.StandardPlugins.GeneratedDataTypesElementProvider",
                                                                    "LocalizeDataWorkflow.ShowError.AlreadyTranslated");
                var messageBox = new MessageBoxMessageQueueItem
                                     {
                                         DialogType = DialogType.Message,
                                         Message = description,
                                         Title = title
                                     };

                ConsoleMessageQueueFacade.Enqueue(messageBox, GetCurrentConsoleId());
                return;
            }

            IEnumerable<ReferenceFailingPropertyInfo> referenceFailingPropertyInfos = DataLocalizationFacade.GetReferencingLocalizeFailingProperties(data).Evaluate();

            IData newData = null;

            using (TransactionScope transactionScope = TransactionsFacade.CreateNewScope())
            {
                if ((data is IPublishControlled) == true)
                {
                    IPublishControlled publishControlled = data as IPublishControlled;

                    if ((publishControlled.PublicationStatus == GenericPublishProcessController.Draft) || (publishControlled.PublicationStatus == GenericPublishProcessController.AwaitingApproval))
                    {
                        data = DataFacade.GetDataFromOtherScope(data, DataScopeIdentifier.Public).Single();
                    }
                }


                using (new DataScope(targetCultureInfo))
                {
                    newData = DataFacade.BuildNew(data.DataSourceId.InterfaceType);

                    data.ProjectedCopyTo(newData);

                    ILocalizedControlled localizedControlled = newData as ILocalizedControlled;
                    localizedControlled.CultureName = targetCultureInfo.Name;
                    localizedControlled.SourceCultureName = targetCultureInfo.Name;

                    if ((newData is IPublishControlled) == true)
                    {
                        IPublishControlled publishControlled = newData as IPublishControlled;
                        publishControlled.PublicationStatus = GenericPublishProcessController.Draft;
                    }

                    foreach (ReferenceFailingPropertyInfo referenceFailingPropertyInfo in referenceFailingPropertyInfos)
                    {
                        PropertyInfo propertyInfo = data.DataSourceId.InterfaceType.GetPropertiesRecursively().Where(f => f.Name == referenceFailingPropertyInfo.DataFieldDescriptor.Name).Single();
                        if ((propertyInfo.PropertyType.IsGenericType == true) &&
                            (propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        {
                            propertyInfo.SetValue(newData, null, null);
                        }
                        else if (propertyInfo.PropertyType == typeof(string))
                        {
                            propertyInfo.SetValue(newData, null, null);
                        }
                        else
                        {
                            propertyInfo.SetValue(newData, referenceFailingPropertyInfo.DataFieldDescriptor.DefaultValue.Value, null);
                        }
                    }

                    newData = DataFacade.AddNew(newData);
                }

                transactionScope.Complete();
            }

            ParentTreeRefresher parentTreeRefresher = this.CreateParentTreeRefresher();
            parentTreeRefresher.PostRefreshMesseges(newData.GetDataEntityToken());

            if (this.Payload == "Global")
            {
                this.ExecuteWorklow(newData.GetDataEntityToken(), typeof(EditDataWorkflow));
            }
            else if (this.Payload == "Pagefolder")
            {
                this.ExecuteWorklow(newData.GetDataEntityToken(), WorkflowFacade.GetWorkflowType("Composite.Elements.ElementProviderHelpers.AssociatedDataElementProviderHelper.EditAssociatedDataWorkflow"));
            }
        }

        private static bool ExistsInLocale(IData data, CultureInfo locale)
        {
            Type dataType = data.DataSourceId.InterfaceType;

            MethodInfo getDataFromOtherScopeMethodInfo = typeof(DataFacade).GetMethod("GetDataFromOtherLocale", BindingFlags.Public | BindingFlags.Static);

            MethodInfo genericMethod = getDataFromOtherScopeMethodInfo.MakeGenericMethod(new[] { dataType });

            object result = genericMethod.Invoke(null, new object[] { data, locale });

            if (result == null) return false;

            var enumerable = result as IEnumerable;
            Verify.IsNotNull(enumerable, "Enumeration expected");

            foreach (object o in enumerable)
            {
                return true;
            }

            return false;
        }
    }
}
