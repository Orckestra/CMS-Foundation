using System.Workflow.ComponentModel;
using System.Workflow.Runtime;
using Composite.Actions;
using Composite.Forms.Flows;


namespace Composite.Workflow.Activities
{
    public sealed class RerenderViewActivity : Activity
    {
        public RerenderViewActivity()
        {
        }



        public RerenderViewActivity(string name)
            : base(name)
        {
        }



        protected sealed override ActivityExecutionStatus Execute(ActivityExecutionContext executionContext)
        {
            FlowControllerServicesContainer flowControllerServicesContainer = WorkflowFacade.GetFlowControllerServicesContainer(WorkflowEnvironment.WorkflowInstanceId);
                        
            IFormFlowRenderingService formFlowRenderingService = flowControllerServicesContainer.GetService<IFormFlowRenderingService>();
            
            formFlowRenderingService.RerenderView();

            return ActivityExecutionStatus.Closed;
        }
    }
}
