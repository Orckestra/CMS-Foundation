using System;
using System.ComponentModel;
using System.Workflow.Activities;
using System.Workflow.ComponentModel.Compiler;




namespace Composite.C1Console.Workflow.Activities
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
    [DefaultEvent("Invoked")]
    [ActivityValidator(typeof(HandleExternalEventActivityValidator))]
    public sealed class FinishHandleExternalEventActivity : HandleExternalEventActivity
    {
        /// <exclude />
        public FinishHandleExternalEventActivity()
            : base()
        {
            Initialize();
        }


        /// <exclude />
        public FinishHandleExternalEventActivity(string name)
            : base(name)
        {
            Initialize();
        }


        /// <exclude />
        [Browsable(false)]
        public override string EventName
        {
            get { return base.EventName; }
            set { base.EventName = value; }
        }


        /// <exclude />
        [Browsable(false)]
        public override Type InterfaceType
        {
            get { return base.InterfaceType; }
            set { base.InterfaceType = value; }
        }


        private void Initialize()
        {
            this.InterfaceType = typeof(IFormsWorkflowEventService);
            this.EventName = "Finish";
        }
    }
}
