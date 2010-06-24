using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.UI;
using System.Workflow.Activities;
using System.Workflow.Runtime;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.Xsl;
using Composite.Actions;
using Composite.ConsoleEventSystem;
using Composite.Data;
using Composite.Data.Plugins.DataProvider.Streams;
using Composite.Data.Types;
using Composite.Extensions;
using Composite.Functions;
using Composite.Functions.ManagedParameters;
using Composite.Logging;
using Composite.ResourceSystem;
using Composite.StandardPlugins.Functions.FunctionProviders.XsltBasedFunctionProvider;
using Composite.Threading;
using Composite.Transactions;
using Composite.WebClient;
using Composite.WebClient.FlowMediators.FormFlowRendering;
using Composite.WebClient.FunctionCallEditor;
using Composite.WebClient.State;
using Composite.Workflow;
using Composite.Workflow.Foundation;
using Composite.Xml;
using Composite.Localization;
using Composite.Users;
using Composite.Renderings.Page;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using Composite.StandardPlugins.Elements.ElementProviders.BaseFunctionProviderElementProvider;


namespace Composite.StandardPlugins.Elements.ElementProviders.XsltBasedFunctionProviderElementProvider
{
    [EntityTokenLock()]
    [AllowPersistingWorkflow(WorkflowPersistingType.Idle)]
    public sealed partial class EditXsltFunctionWorkflow : Composite.Workflow.Activities.FormsWorkflow
    {
        public EditXsltFunctionWorkflow()
        {
            InitializeComponent();
        }



        private void CheckActiveLanguagesExists(object sender, ConditionalEventArgs e)
        {
            e.Result = UserSettings.ActiveLocaleCultureInfo != null;
        }



        private void CheckPageExists(object sender, ConditionalEventArgs e)
        {
            e.Result = DataFacade.GetData<IPage>().Any();
        }



        private void MissingActiveLanguageCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            ShowMessage(DialogType.Message,
                GetString("EditXsltFunctionWorkflow.MissingActiveLanguageTitle"),
                GetString("EditXsltFunctionWorkflow.MissingActiveLanguageMessage"));
        }



        private void MissingPageCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            ShowMessage(
                DialogType.Message,
                GetString("EditXsltFunctionWorkflow.MissingPageTitle"),
                GetString("EditXsltFunctionWorkflow.MissingPageMessage"));
        }



        private void initializeCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            DataEntityToken dataEntityToken = (DataEntityToken)this.EntityToken;
            IXsltFunction xsltFunction = (IXsltFunction)dataEntityToken.Data;
            IFile file = IFileServices.GetFile<IXsltFile>(xsltFunction.XslFilePath);
            IEnumerable<ManagedParameterDefinition> parameters = ManagedParameterManager.Load(xsltFunction.Id);
            this.Bindings.Add("CurrentXslt", dataEntityToken.Data);
            this.Bindings.Add("Parameters", parameters);

            // popular type widgets
            List<Type> popularTypes = DataFacade.GetAllInterfaces(UserType.Developer);
            var popularWidgetTypes = FunctionFacade.WidgetFunctionSupportedTypes.Where(f => f.GetGenericArguments().Any(g => popularTypes.Any(h => h.IsAssignableFrom(g))));

            this.Bindings.Add("ParameterTypeOptions", FunctionFacade.FunctionSupportedTypes.Union(popularWidgetTypes).Union(FunctionFacade.WidgetFunctionSupportedTypes).ToList());

            this.Bindings.Add("XslTemplate", file.ReadAllText());
            List<string> functionErrors;
            List<NamedFunctionCall> FunctionCalls = RenderHelper.GetValidFunctionCalls(xsltFunction.Id, out functionErrors).ToList();

            if ((functionErrors != null) && (functionErrors.Any() == true))
            {
                foreach (string error in functionErrors)
                {
                    this.ShowMessage(DialogType.Error, "A function call has been dropped", error);
                }
            }

            this.Bindings.Add("FunctionCalls", FunctionCalls);

            List<KeyValuePair<Guid, string>> pages = PageStructureInfo.PageListInDocumentOrder().ToList();
            if (pages.Count > 0)
            {
                this.Bindings.Add("PageId", pages.First().Key);
            }
            else
            {
                this.Bindings.Add("PageId", Guid.Empty);
            }

            this.Bindings.Add("PageList", pages);

            if (UserSettings.ActiveLocaleCultureInfo != null)
            {
                List<KeyValuePair<string, string>> activeCulturesDictionary = UserSettings.ActiveLocaleCultureInfos.Select(f => new KeyValuePair<string, string>(f.Name, StringResourceSystemFacade.GetString("Composite.Cultures", f.Name))).ToList();
                this.Bindings.Add("ActiveCultureName", UserSettings.ActiveLocaleCultureInfo.Name);
                this.Bindings.Add("ActiveCulturesList", activeCulturesDictionary);
            }

            this.Bindings.Add("PageDataScopeName", DataScopeIdentifier.AdministratedName);
            this.Bindings.Add("PageDataScopeList", new Dictionary<string, string> 
            { 
                { DataScopeIdentifier.AdministratedName, GetString("EditXsltFunction.LabelAdminitrativeScope") }, 
                { DataScopeIdentifier.PublicName, GetString("EditXsltFunction.LabelPublicScope") } 
            });


            // Creating a session state object
            Guid stateId = Guid.NewGuid();
            var state = new FunctionCallDesignerState { WorkflowId = WorkflowInstanceId, ConsoleIdInternal = GetCurrentConsoleId() };
            SessionStateManager.DefaultProvider.AddState<IFunctionCallEditorState>(stateId, state, DateTime.Now.AddDays(7.0));

            this.Bindings.Add("SessionStateProvider", SessionStateManager.DefaultProviderName);
            this.Bindings.Add("SessionStateId", stateId);
        }


        private void IsValidData(object sender, ConditionalEventArgs e)
        {
            IXsltFunction function = this.GetBinding<IXsltFunction>("CurrentXslt");

            if (function.Name == string.Empty)
            {
                this.ShowFieldMessage("CurrentXslt.Name", GetString("EditXsltFunctionWorkflow.EmptyMethodName"));
                e.Result = false;
                return;
            }
            if (!function.Namespace.IsCorrectNamespace('.'))
            {
                this.ShowFieldMessage("CurrentXslt.Namespace", GetString("EditXsltFunctionWorkflow.InvalidNamespace"));
                e.Result = false;
                return;
            }
            if (!(function.XslFilePath.StartsWith("\\") || function.XslFilePath.StartsWith("/")))
            {
                this.ShowFieldMessage("CurrentXslt.XslFilePath", GetString("EditXsltFunctionWorkflow.InvalidFileName"));
                e.Result = false;
                return;
            }

            e.Result = true;
        }


        private void editPreviewActivity_ExecuteCode(object sender, EventArgs e)
        {
            Stopwatch functionCallingStopwatch = null;
            long millisecondsToken = 0;

            CultureInfo oldCurrentCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo oldCurrentUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                IXsltFunction xslt = this.GetBinding<IXsltFunction>("CurrentXslt");
                string xslTemplate = this.GetBinding<string>("XslTemplate");

                List<NamedFunctionCall> namedFunctions = this.GetBinding<IEnumerable<NamedFunctionCall>>("FunctionCalls").ToList();
                List<ManagedParameterDefinition> parameterDefinitions = this.GetBinding<IEnumerable<ManagedParameterDefinition>>("Parameters").ToList();

                Guid pageId = this.GetBinding<Guid>("PageId");
                string dataScopeName = this.GetBinding<string>("PageDataScopeName");
                string cultureName = this.GetBinding<string>("ActiveCultureName");
                CultureInfo cultureInfo = null;
                if (cultureName != null)
                {
                    cultureInfo = CultureInfo.CreateSpecificCulture(cultureName);
                }


                TransformationInputs transformationInput;
                using (new DataScope(DataScopeIdentifier.Deserialize(dataScopeName), cultureInfo))
                {
                    Thread.CurrentThread.CurrentCulture = cultureInfo;
                    Thread.CurrentThread.CurrentUICulture = cultureInfo;

                    IPage page = DataFacade.GetData<IPage>(f => f.Id == pageId).FirstOrDefault();
                    if (page != null)
                    {
                        PageRenderer.CurrentPage = page;
                    }

                    functionCallingStopwatch = Stopwatch.StartNew();
                    transformationInput = RenderHelper.BuildInputDocument(namedFunctions, parameterDefinitions, true);
                    functionCallingStopwatch.Stop();

                    Thread.CurrentThread.CurrentCulture = oldCurrentCulture;
                    Thread.CurrentThread.CurrentUICulture = oldCurrentUICulture;
                }


                string output = "";
                string error = "";
                try
                {
                    Thread.CurrentThread.CurrentCulture = cultureInfo;
                    Thread.CurrentThread.CurrentUICulture = cultureInfo;

                    var styleSheet = XElement.Parse(xslTemplate);

                    XsltBasedFunctionProvider.ResolveImportIncludePaths(styleSheet);

                    LocalizationParser.Parse(styleSheet);

                    XDocument transformationResult = new XDocument();
                    using (XmlWriter writer = new LimitedDepthXmlWriter(transformationResult.CreateWriter()))
                    {
                        XslCompiledTransform xslTransformer = new XslCompiledTransform();
                        xslTransformer.Load(styleSheet.CreateReader(), XsltSettings.TrustedXslt, new XmlUrlResolver());

                        XsltArgumentList transformArgs = new XsltArgumentList();
                        XslExtensionsManager.Register(transformArgs);

                        if (transformationInput.ExtensionDefinitions != null)
                        {
                            foreach (IXsltExtensionDefinition extensionDef in transformationInput.ExtensionDefinitions)
                            {
                                transformArgs.AddExtensionObject(extensionDef.ExtensionNamespace.ToString(),
                                                                 extensionDef.EntensionObjectAsObject);
                            }
                        }

                        Exception exception = null;

                        Thread thread = new Thread(delegate()
                           {
                               Stopwatch transformationStopwatch = Stopwatch.StartNew();

                               Thread.CurrentThread.CurrentCulture = cultureInfo;
                               Thread.CurrentThread.CurrentUICulture = cultureInfo;
                               try
                               {
                                   using (ThreadDataManager.Initialize())
                                   {
                                       var reader = transformationInput.InputDocument.CreateReader();
                                       xslTransformer.Transform(reader, transformArgs, writer);
                                   }
                               }
                               catch (ThreadAbortException ex)
                               {
                                   exception = ex;
                                   Thread.ResetAbort();
                               }
                               catch (Exception ex)
                               {
                                   exception = ex;
                               }

                               transformationStopwatch.Stop();

                               millisecondsToken = transformationStopwatch.ElapsedMilliseconds;
                           });

                        thread.Start();
                        bool res = thread.Join(1000);  // sadly, this needs to be low enough to prevent StackOverflowException from fireing.

                        if (res == false)
                        {
                            if (thread.ThreadState == System.Threading.ThreadState.Running)
                            {
                                thread.Abort();
                            }
                            throw new XslLoadException("Transformation took more than 1000 milliseconds to complete. This could be due to a never ending recursive call. Execution aborted to prevent fatal StackOverflowException.");
                        }

                        if (exception != null)
                        {
                            throw exception;
                        }
                    }

                    if (xslt.OutputXmlSubType == "XHTML")
                    {
                        XhtmlDocument xhtmlDocument = new XhtmlDocument(transformationResult);

                        output = xhtmlDocument.Root.ToString();
                    }
                    else
                    {
                        output = transformationResult.Root.ToString();
                    }
                }
                catch (Exception ex)
                {
                    output = "<error/>";
                    error = string.Format("{0}\n{1}", ex.GetType().Name, ex.Message);

                    Exception inner = ex.InnerException;

                    string indent = "";

                    while (inner != null)
                    {
                        indent = indent + " - ";
                        error = error + "\n" + indent + inner.Message;
                        inner = inner.InnerException;
                    }
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = oldCurrentCulture;
                    Thread.CurrentThread.CurrentUICulture = oldCurrentUICulture;
                }
                
                Page currentPage = HttpContext.Current.Handler as Page;
                if (currentPage == null) throw new InvalidOperationException("The Current HttpContext Handler must be a System.Web.Ui.Page");

                UserControl inOutControl = (UserControl)currentPage.LoadControl(UrlUtils.ResolveAdminUrl("controls/Misc/MarkupInOutView.ascx"));
                inOutControl.Attributes.Add("in", transformationInput.InputDocument.ToString());
                inOutControl.Attributes.Add("out", output);
                inOutControl.Attributes.Add("error", error);
                inOutControl.Attributes.Add("statusmessage", string.Format("Execution times: Total {0} ms. Functions: {1} ms. XSLT: {2} ms.",
                    millisecondsToken + functionCallingStopwatch.ElapsedMilliseconds,
                    functionCallingStopwatch.ElapsedMilliseconds,
                    millisecondsToken));

                FlowControllerServicesContainer serviceContainer = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
                var webRenderService = serviceContainer.GetService<IFormFlowWebRenderingService>();
                webRenderService.SetNewPageOutput(inOutControl);
            }
            catch (Exception ex)
            {
                FlowControllerServicesContainer serviceContainer = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
                Control errOutput = new LiteralControl("<pre>" + ex.ToString() + "</pre>");
                var webRenderService = serviceContainer.GetService<IFormFlowWebRenderingService>();
                webRenderService.SetNewPageOutput(errOutput);
            }
        }


        private IEnumerable<INamedFunctionCall> ConvertFunctionCalls(IEnumerable<NamedFunctionCall> FunctionCalls, Guid xsltId)
        {
            foreach (NamedFunctionCall namedFunctionCall in FunctionCalls)
            {
                INamedFunctionCall newNamedFunctionCall = DataFacade.BuildNew<INamedFunctionCall>();
                newNamedFunctionCall.XsltFunctionId = xsltId;
                newNamedFunctionCall.Name = namedFunctionCall.Name;
                newNamedFunctionCall.SerializedFunction = namedFunctionCall.FunctionCall.Serialize().ToString(SaveOptions.DisableFormatting);

                yield return newNamedFunctionCall;
            }
        }


        private void saveCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            try
            {
                IXsltFunction xslt = this.GetBinding<IXsltFunction>("CurrentXslt");
                IXsltFunction previousXslt = DataFacade.GetData<IXsltFunction>(f => f.Id == xslt.Id).SingleOrDefault();

                string xslTemplate = this.GetBinding<string>("XslTemplate");
                var parameters = this.GetBinding<IEnumerable<ManagedParameterDefinition>>("Parameters");

                IEnumerable<NamedFunctionCall> FunctionCalls = this.GetBinding<IEnumerable<NamedFunctionCall>>("FunctionCalls");

                using (TransactionScope transactionScope = TransactionsFacade.CreateNewScope())
                {
                    // Renaming related file if necessary
                    string newRelativePath = AddNewXsltFunctionWorkflow.CreateXslFilePath(xslt).Replace("/", "\\");

                    if (string.Compare(xslt.XslFilePath, newRelativePath, true) != 0)
                    {
                        var xlsFile = IFileServices.GetFile<IXsltFile>(xslt.XslFilePath);
                        string systemPath = (xlsFile as FileSystemFileBase).SystemPath;
                        // Implement it in another way?
                        string xsltFilesRoot = systemPath.Substring(0, systemPath.Length - xslt.XslFilePath.Length);

                        string newSystemPath = xsltFilesRoot + newRelativePath;

                        if (string.Compare(systemPath, newSystemPath, true) != 0
                            && File.Exists(newSystemPath))
                        {
                            FlowControllerServicesContainer serviceContainer = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
                            var consoleMessageService = serviceContainer.GetService<IManagementConsoleMessageService>();
                            consoleMessageService.ShowMessage(
                                DialogType.Error,
                                GetString("EditXsltFunctionWorkflow.InvalidName"),
                                GetString("EditXsltFunctionWorkflow.CannotRenameFileExists").FormatWith(newSystemPath));
                            return;
                        }

                        string directoryPath = Path.GetDirectoryName(newSystemPath);
                        if(!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        File.Move(systemPath, newSystemPath);

                        xslt.XslFilePath = newRelativePath;

                        // TODO: Implement removing empty Xslt directories
                    }


                    IFile file = IFileServices.GetFile<IXsltFile>(xslt.XslFilePath);
                    file.SetNewContent(xslTemplate);

                    ManagedParameterManager.Save(xslt.Id, parameters);

                    DataFacade.Update(xslt);
                    DataFacade.Update(file);

                    DataFacade.Delete<INamedFunctionCall>(f => f.XsltFunctionId == xslt.Id);
                    DataFacade.AddNew<INamedFunctionCall>(ConvertFunctionCalls(FunctionCalls, xslt.Id));

                    transactionScope.Complete();
                }

                if (previousXslt.Namespace != xslt.Namespace || previousXslt.Name != xslt.Name || previousXslt.Description != xslt.Description)
                {
                    // This is a some what nasty hack. Due to the nature of the BaseFunctionProviderElementProvider, this hack is needed
                    BaseFunctionFolderElementEntityToken entityToken = new BaseFunctionFolderElementEntityToken("ROOT:XsltBasedFunctionProviderElementProvider");
                    RefreshEntityToken(entityToken);                    
                }
                
                SetSaveStatus(true);
                
            }
            catch (Exception ex)
            {
                LoggingService.LogCritical("XSLT Save", ex);

                FlowControllerServicesContainer serviceContainer = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
                var consoleMsgService = serviceContainer.GetService<IManagementConsoleMessageService>();
                consoleMsgService.ShowMessage(DialogType.Error, "Error", ex.Message);

                SetSaveStatus(false);
            }
        }

        private static string GetString(string key)
        {
            return StringResourceSystemFacade.GetString("Composite.StandardPlugins.XsltBasedFunction", key);
        }

        // This is for propergating error message with never ending recursive calls
        private sealed class XslLoadException : Exception
        {
            public XslLoadException(string message)
                : base(message)
            {
            }
        }

        [Serializable]
        public sealed class FunctionCallDesignerState : IFunctionCallEditorState
        {
            public Guid WorkflowId { get; set; }
            public string ConsoleIdInternal { get; set; }

            private FormData GetFormData()
            {
                return WorkflowFacade.GetFormData(WorkflowId);
            }

            #region IFunctionCallEditorState Members

            [XmlIgnore]
            public List<NamedFunctionCall> FunctionCalls
            {
                get { return GetFormData().Bindings["FunctionCalls"] as List<NamedFunctionCall>; }
                set { GetFormData().Bindings["FunctionCalls"] = value; }
            }

            [XmlIgnore]
            public List<ManagedParameterDefinition> Parameters
            {
                get { return GetFormData().Bindings["Parameters"] as List<ManagedParameterDefinition>; }
                set { GetFormData().Bindings["Parameters"] = value; }
            }

            [XmlIgnore]
            public List<Type> ParameterTypeOptions
            {
                get { return (GetFormData().Bindings["ParameterTypeOptions"] as IEnumerable<Type>).ToList(); }
                set { GetFormData().Bindings["ParameterTypeOptions"] = value.ToList(); }
            }

            public bool WidgetFunctionSelection
            {
                get { return false; }
            }

            public bool ShowLocalFunctionNames
            {
                get { return true; }
            }

            public bool AllowLocalFunctionNameEditing
            {
                get { return true; }
            }

            public bool AllowSelectingInputParameters
            {
                get { return true; }
            }

            public Type[] AllowedResultTypes
            {
                get { return new[] {
                    typeof (XDocument), typeof (XElement), typeof (IEnumerable<XElement>),
                    typeof(bool), typeof(int), typeof(string), typeof(DateTime), typeof(Guid), typeof(CultureInfo),
                    typeof(IDataReference), typeof(IXsltExtensionDefinition)};
                }
            }

            public int MaxFunctionAllowed
            {
                get { return 1000; }
            }

            string IFunctionCallEditorState.ConsoleId
            {
                get { return ConsoleIdInternal; }
            }

            #endregion
        }
    }
}
