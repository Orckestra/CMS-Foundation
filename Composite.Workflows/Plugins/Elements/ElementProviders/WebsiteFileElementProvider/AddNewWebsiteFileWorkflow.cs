﻿using System;
using System.IO;
using System.Workflow.Activities;
using Composite.C1Console.Actions;
using Composite.C1Console.Elements;
using Composite.C1Console.Workflow;
using Composite.Core.IO;


namespace Composite.Plugins.Elements.ElementProviders.WebsiteFileElementProvider
{
    [AllowPersistingWorkflow(WorkflowPersistingType.Idle)]
    public sealed partial class AddNewWebsiteFileWorkflow : Composite.C1Console.Workflow.Activities.FormsWorkflow
    {
        public AddNewWebsiteFileWorkflow()
        {
            InitializeComponent();
        }



        private string GetCurrentPath()
        {
            if (this.EntityToken is WebsiteFileElementProviderRootEntityToken)
            {
                string rootPath = (string)ElementFacade.GetData(new ElementProviderHandle(this.EntityToken.Source), "RootPath");

                return rootPath;
            }
            else if (this.EntityToken is WebsiteFileElementProviderEntityToken)
            {
                return (this.EntityToken as WebsiteFileElementProviderEntityToken).Path;
            }
            else
            {
                throw new NotImplementedException();
            }
        }



        private void FileExists(object sender, ConditionalEventArgs e)
        {
            string currentPath = GetCurrentPath();
            string newFileName = this.GetBinding<string>("NewFileName");

            e.Result = Composite.Core.IO.File.Exists(Path.Combine(currentPath, newFileName));
        }



        private void initializeCodeActivity_Initialize_ExecuteCode(object sender, EventArgs e)
        {
            this.Bindings.Add("NewFileName", "");
        }



        private void step1CodeActivity_ShowError_ExecuteCode(object sender, EventArgs e)
        {
            this.ShowFieldMessage("NewFileName", "${Composite.Plugins.WebsiteFileElementProvider, AddNewFile.Error.FileExist}");
        }



        private void finalizeCodeActivity_ExecuteCode(object sender, EventArgs e)
        {
            string currentPath = GetCurrentPath();
            string newFileName = this.GetBinding<string>("NewFileName");

            using (C1FileStream fs = Composite.Core.IO.File.Create(Path.Combine(currentPath, newFileName)))
            { }

            SpecificTreeRefresher specificTreeRefresher = this.CreateSpecificTreeRefresher();
            specificTreeRefresher.PostRefreshMesseges(this.EntityToken);
        }
    }
}
