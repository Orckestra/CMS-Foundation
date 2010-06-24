using System;
using System.Collections.Generic;
using Composite.Actions;
using Composite.PackageSystem;
using Composite.Forms.CoreUiControls;
using Composite.Workflow;
using Composite.ResourceSystem;
using Composite.ConsoleEventSystem;
using Composite.EventSystem;


namespace Composite.StandardPlugins.Elements.ElementProviders.PackageElementProvider
{
    [AllowPersistingWorkflow(WorkflowPersistingType.Idle)]
    public sealed partial class InstallLocalPackageWorkflow : Composite.Workflow.Activities.FormsWorkflow
    {
        public InstallLocalPackageWorkflow()
        {
            InitializeComponent();
        }



        private void WasFileSelected(object sender, System.Workflow.Activities.ConditionalEventArgs e)
        {
            UploadedFile uploadedFile = this.GetBinding<UploadedFile>("UploadedFile");

            e.Result = uploadedFile.HasFile;
        }



        private void DidValidate(object sender, System.Workflow.Activities.ConditionalEventArgs e)
        {
            e.Result = this.BindingExist("Errors") == false;
        }



        private void initializeCodeActivity_Initialize_ExecuteCode(object sender, EventArgs e)
        {
            this.Bindings.Add("UploadedFile", new UploadedFile());
        }



        private void step1CodeActivity_ValidateInstallation_ExecuteCode(object sender, EventArgs e)
        {
            try
            {
                UploadedFile uploadedFile = this.GetBinding<UploadedFile>("UploadedFile");

                PackageManagerInstallProcess packageManagerInstallProcess = PackageManager.Install(uploadedFile.FileStream, true);

                if (packageManagerInstallProcess.PreInstallValidationResult.Count > 0)
                {
                    this.UpdateBinding("Errors", WorkflowHelper.ValidationResultToBinding(packageManagerInstallProcess.PreInstallValidationResult));
                }
                else
                {
                    List<PackageFragmentValidationResult> validationResult = packageManagerInstallProcess.Validate();

                    if (validationResult.Count > 0)
                    {
                        this.UpdateBinding("Errors", WorkflowHelper.ValidationResultToBinding(validationResult));
                    }
                    else
                    {
                        this.Bindings.Add("AddOnManagerInstallProcess", packageManagerInstallProcess);

                        this.Bindings.Add("FlushOnCompletion", packageManagerInstallProcess.FlushOnCompletion);
                        this.Bindings.Add("ReloadConsoleOnCompletion", packageManagerInstallProcess.ReloadConsoleOnCompletion);
                    }
                }
            }
            catch (Exception ex)
            {
                this.UpdateBinding("Errors", new List<List<string>> { new List<string> { ex.Message, "" } });
            }
        }



        private void step2CodeActivity_Install_ExecuteCode(object sender, EventArgs e)
        {
            try
            {
                PackageManagerInstallProcess packageManagerInstallProcess = this.GetBinding<PackageManagerInstallProcess>("AddOnManagerInstallProcess");

                List<PackageFragmentValidationResult> installResult = packageManagerInstallProcess.Install();
                if (installResult.Count > 0)
                {
                    this.UpdateBinding("Errors", WorkflowHelper.ValidationResultToBinding(installResult));
                }
            }
            catch (Exception ex)
            {
                this.UpdateBinding("Errors", new List<List<string>> { new List<string> { ex.Message, "" } });
            }
        }



        private void cleanupCodeActivity_Cleanup_ExecuteCode(object sender, EventArgs e)
        {
            PackageManagerInstallProcess packageManagerInstallProcess;
            if (this.TryGetBinding<PackageManagerInstallProcess>("AddOnManagerInstallProcess", out packageManagerInstallProcess) == true)
            {
                packageManagerInstallProcess.CancelInstallation();
            }
        }



        private void step3CodeActivity_RefreshTree_ExecuteCode(object sender, EventArgs e)
        {
            if (this.GetBinding<bool>("ReloadConsoleOnCompletion") == true)
            {
                ConsoleMessageQueueFacade.Enqueue(new RebootConsoleMessageQueueItem(), null);
            }

            if (this.GetBinding<bool>("FlushOnCompletion") == true)
            {
                GlobalEventSystemFacade.FlushTheSystem();
            }

            SpecificTreeRefresher specificTreeRefresher = this.CreateSpecificTreeRefresher();
            specificTreeRefresher.PostRefreshMesseges(new PackageElementProviderRootEntityToken());
        }



        private void showErrorCodeActivity_Initialize_ExecuteCode(object sender, EventArgs e)
        {
            List<string> rowHeader = new List<string>();
            rowHeader.Add(StringResourceSystemFacade.ParseString("${Composite.StandardPlugins.PackageElementProvider, InstallLocalAddOn.ShowError.MessageTitle}"));

            this.UpdateBinding("ErrorHeader", rowHeader);
        }
    }
}
