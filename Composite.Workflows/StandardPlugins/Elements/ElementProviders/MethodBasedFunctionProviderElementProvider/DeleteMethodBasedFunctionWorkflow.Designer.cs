using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Workflow.ComponentModel.Compiler;
using System.Workflow.ComponentModel.Serialization;
using System.Workflow.ComponentModel;
using System.Workflow.ComponentModel.Design;
using System.Workflow.Runtime;
using System.Workflow.Activities;
using System.Workflow.Activities.Rules;

namespace Composite.StandardPlugins.Elements.ElementProviders.MethodBasedFunctionProviderElementProvider
{
    partial class DeleteMethodBasedFunctionWorkflow
    {
        #region Designer generated code
        
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode]
        private void InitializeComponent()
        {
            this.CanModifyActivities = true;
            this.setStateActivity5 = new System.Workflow.Activities.SetStateActivity();
            this.closeCurrentViewActivity1 = new Composite.Workflow.Activities.CloseCurrentViewActivity();
            this.finalizeCodeActivity = new System.Workflow.Activities.CodeActivity();
            this.setStateActivity1 = new System.Workflow.Activities.SetStateActivity();
            this.cancelHandleExternalEventActivity2 = new Composite.Workflow.Activities.CancelHandleExternalEventActivity();
            this.setStateActivity3 = new System.Workflow.Activities.SetStateActivity();
            this.finishHandleExternalEventActivity1 = new Composite.Workflow.Activities.FinishHandleExternalEventActivity();
            this.confirmDialogFormActivity1 = new Composite.Workflow.Activities.ConfirmDialogFormActivity();
            this.setStateActivity4 = new System.Workflow.Activities.SetStateActivity();
            this.finalizeStateInitializationActivity = new System.Workflow.Activities.StateInitializationActivity();
            this.step1EventDrivenActivity_Cancel = new System.Workflow.Activities.EventDrivenActivity();
            this.step1EventDrivenActivity_Finish = new System.Workflow.Activities.EventDrivenActivity();
            this.step1StateInitializationActivity = new System.Workflow.Activities.StateInitializationActivity();
            this.setStateActivity2 = new System.Workflow.Activities.SetStateActivity();
            this.cancelHandleExternalEventActivity1 = new Composite.Workflow.Activities.CancelHandleExternalEventActivity();
            this.deleteStateInitializationActivity = new System.Workflow.Activities.StateInitializationActivity();
            this.finalizeStateActivity = new System.Workflow.Activities.StateActivity();
            this.step1StateActivity = new System.Workflow.Activities.StateActivity();
            this.eventDrivenActivity_GlobalCancel = new System.Workflow.Activities.EventDrivenActivity();
            this.finalStateActivity = new System.Workflow.Activities.StateActivity();
            this.deleteStateActivity = new System.Workflow.Activities.StateActivity();
            // 
            // setStateActivity5
            // 
            this.setStateActivity5.Name = "setStateActivity5";
            this.setStateActivity5.TargetStateName = "finalStateActivity";
            // 
            // closeCurrentViewActivity1
            // 
            this.closeCurrentViewActivity1.Name = "closeCurrentViewActivity1";
            // 
            // finalizeCodeActivity
            // 
            this.finalizeCodeActivity.Name = "finalizeCodeActivity";
            this.finalizeCodeActivity.ExecuteCode += new System.EventHandler(this.finalizeCodeActivity_ExecuteCode);
            // 
            // setStateActivity1
            // 
            this.setStateActivity1.Name = "setStateActivity1";
            this.setStateActivity1.TargetStateName = "finalStateActivity";
            // 
            // cancelHandleExternalEventActivity2
            // 
            this.cancelHandleExternalEventActivity2.EventName = "Cancel";
            this.cancelHandleExternalEventActivity2.InterfaceType = typeof(Composite.Workflow.IFormsWorkflowEventService);
            this.cancelHandleExternalEventActivity2.Name = "cancelHandleExternalEventActivity2";
            // 
            // setStateActivity3
            // 
            this.setStateActivity3.Name = "setStateActivity3";
            this.setStateActivity3.TargetStateName = "finalizeStateActivity";
            // 
            // finishHandleExternalEventActivity1
            // 
            this.finishHandleExternalEventActivity1.EventName = "Finish";
            this.finishHandleExternalEventActivity1.InterfaceType = typeof(Composite.Workflow.IFormsWorkflowEventService);
            this.finishHandleExternalEventActivity1.Name = "finishHandleExternalEventActivity1";
            // 
            // confirmDialogFormActivity1
            // 
            this.confirmDialogFormActivity1.ContainerLabel = null;
            this.confirmDialogFormActivity1.FormDefinitionFileName = "/Administrative/MethodBasedFunctionProviderElementProviderDeleteStep1.xml";
            this.confirmDialogFormActivity1.Name = "confirmDialogFormActivity1";
            // 
            // setStateActivity4
            // 
            this.setStateActivity4.Name = "setStateActivity4";
            this.setStateActivity4.TargetStateName = "step1StateActivity";
            // 
            // finalizeStateInitializationActivity
            // 
            this.finalizeStateInitializationActivity.Activities.Add(this.finalizeCodeActivity);
            this.finalizeStateInitializationActivity.Activities.Add(this.closeCurrentViewActivity1);
            this.finalizeStateInitializationActivity.Activities.Add(this.setStateActivity5);
            this.finalizeStateInitializationActivity.Name = "finalizeStateInitializationActivity";
            // 
            // step1EventDrivenActivity_Cancel
            // 
            this.step1EventDrivenActivity_Cancel.Activities.Add(this.cancelHandleExternalEventActivity2);
            this.step1EventDrivenActivity_Cancel.Activities.Add(this.setStateActivity1);
            this.step1EventDrivenActivity_Cancel.Name = "step1EventDrivenActivity_Cancel";
            // 
            // step1EventDrivenActivity_Finish
            // 
            this.step1EventDrivenActivity_Finish.Activities.Add(this.finishHandleExternalEventActivity1);
            this.step1EventDrivenActivity_Finish.Activities.Add(this.setStateActivity3);
            this.step1EventDrivenActivity_Finish.Name = "step1EventDrivenActivity_Finish";
            // 
            // step1StateInitializationActivity
            // 
            this.step1StateInitializationActivity.Activities.Add(this.confirmDialogFormActivity1);
            this.step1StateInitializationActivity.Name = "step1StateInitializationActivity";
            // 
            // setStateActivity2
            // 
            this.setStateActivity2.Name = "setStateActivity2";
            this.setStateActivity2.TargetStateName = "finalStateActivity";
            // 
            // cancelHandleExternalEventActivity1
            // 
            this.cancelHandleExternalEventActivity1.EventName = "Cancel";
            this.cancelHandleExternalEventActivity1.InterfaceType = typeof(Composite.Workflow.IFormsWorkflowEventService);
            this.cancelHandleExternalEventActivity1.Name = "cancelHandleExternalEventActivity1";
            // 
            // deleteStateInitializationActivity
            // 
            this.deleteStateInitializationActivity.Activities.Add(this.setStateActivity4);
            this.deleteStateInitializationActivity.Name = "deleteStateInitializationActivity";
            // 
            // finalizeStateActivity
            // 
            this.finalizeStateActivity.Activities.Add(this.finalizeStateInitializationActivity);
            this.finalizeStateActivity.Name = "finalizeStateActivity";
            // 
            // step1StateActivity
            // 
            this.step1StateActivity.Activities.Add(this.step1StateInitializationActivity);
            this.step1StateActivity.Activities.Add(this.step1EventDrivenActivity_Finish);
            this.step1StateActivity.Activities.Add(this.step1EventDrivenActivity_Cancel);
            this.step1StateActivity.Name = "step1StateActivity";
            // 
            // eventDrivenActivity_GlobalCancel
            // 
            this.eventDrivenActivity_GlobalCancel.Activities.Add(this.cancelHandleExternalEventActivity1);
            this.eventDrivenActivity_GlobalCancel.Activities.Add(this.setStateActivity2);
            this.eventDrivenActivity_GlobalCancel.Name = "eventDrivenActivity_GlobalCancel";
            // 
            // finalStateActivity
            // 
            this.finalStateActivity.Name = "finalStateActivity";
            // 
            // deleteStateActivity
            // 
            this.deleteStateActivity.Activities.Add(this.deleteStateInitializationActivity);
            this.deleteStateActivity.Name = "deleteStateActivity";
            // 
            // DeleteMethodBasedFunctionWorkflow
            // 
            this.Activities.Add(this.deleteStateActivity);
            this.Activities.Add(this.finalStateActivity);
            this.Activities.Add(this.eventDrivenActivity_GlobalCancel);
            this.Activities.Add(this.step1StateActivity);
            this.Activities.Add(this.finalizeStateActivity);
            this.CompletedStateName = "finalStateActivity";
            this.DynamicUpdateCondition = null;
            this.InitialStateName = "deleteStateActivity";
            this.Name = "DeleteMethodBasedFunctionWorkflow";
            this.CanModifyActivities = false;

        }

        #endregion

        private StateInitializationActivity deleteStateInitializationActivity;
        private StateActivity finalStateActivity;
        private SetStateActivity setStateActivity2;
        private Composite.Workflow.Activities.CancelHandleExternalEventActivity cancelHandleExternalEventActivity1;
        private EventDrivenActivity eventDrivenActivity_GlobalCancel;
        private CodeActivity finalizeCodeActivity;
        private StateInitializationActivity finalizeStateInitializationActivity;
        private EventDrivenActivity step1EventDrivenActivity_Cancel;
        private EventDrivenActivity step1EventDrivenActivity_Finish;
        private StateInitializationActivity step1StateInitializationActivity;
        private StateActivity finalizeStateActivity;
        private StateActivity step1StateActivity;
        private SetStateActivity setStateActivity1;
        private Composite.Workflow.Activities.CancelHandleExternalEventActivity cancelHandleExternalEventActivity2;
        private SetStateActivity setStateActivity3;
        private Composite.Workflow.Activities.FinishHandleExternalEventActivity finishHandleExternalEventActivity1;
        private Composite.Workflow.Activities.ConfirmDialogFormActivity confirmDialogFormActivity1;
        private SetStateActivity setStateActivity4;
        private SetStateActivity setStateActivity5;
        private Composite.Workflow.Activities.CloseCurrentViewActivity closeCurrentViewActivity1;
        private StateActivity deleteStateActivity;










    }
}
