//#warning REMARK THIS!!!
//#define NO_SECURITY
using System;
using System.Collections.Generic;
using Composite.Actions.Foundation;
using Composite.Actions.Workflows;
using Composite.ConsoleEventSystem;
using Composite.Logging;
using Composite.Security;
using Composite.Tasks;
using Composite.Workflow;


namespace Composite.Actions
{
    public static class ActionExecutorFacade
    {
        public static FlowToken Execute(EntityToken entityToken, ActionToken actionToken, FlowControllerServicesContainer flowControllerServicesContainer)
        {
            return Execute(entityToken, actionToken, flowControllerServicesContainer, null);
        }



        public static FlowToken Execute(EntityToken entityToken, ActionToken actionToken, FlowControllerServicesContainer flowControllerServicesContainer, TaskManagerEvent taskManagerEvent)
        {
            if (entityToken == null) throw new ArgumentNullException("entityToken");
            if (actionToken == null) throw new ArgumentNullException("actionToken");


            string username = UserValidationFacade.GetUsername();
#if NO_SECURITY
#else
            HookingFacade.EnsureInitialization();

            IEnumerable<UserPermissionDefinition> userPermissionDefinitions = PermissionTypeFacade.GetUserPermissionDefinitions(username);
            IEnumerable<UserGroupPermissionDefinition> userGroupPermissionDefinitions = PermissionTypeFacade.GetUserGroupPermissionDefinitions(username);
            SecurityResult securityResult = SecurityResolver.Resolve(UserValidationFacade.GetUserToken(), actionToken, entityToken, userPermissionDefinitions, userGroupPermissionDefinitions);
            if ((securityResult != SecurityResult.Allowed) && (entityToken.GetType() != typeof(SecurityViolationWorkflowEntityToken)))
            {
                return ExecuteSecurityViolation(actionToken, entityToken, flowControllerServicesContainer);
            }
#endif

            bool ignoreLocking = actionToken.IsIgnoreEntityTokenLocking();

            if ((ignoreLocking == true) ||
                (ActionLockingFacade.IsLocked(entityToken) == false))
            {
                IActionExecutor actionExecutor = ActionExecutorCache.GetActionExecutor(actionToken);

                ActionEventSystemFacade.FireOnBeforeActionExecution(entityToken, actionToken);

                FlowToken flowToken;
                using (TaskContainer taskContainer = TaskManagerFacade.CreateNewTasks(entityToken, actionToken, taskManagerEvent))
                {
                    ITaskManagerFlowControllerService taskManagerService = null;
                    if (flowControllerServicesContainer.GetService(typeof(ITaskManagerFlowControllerService)) == null)
                    {
                        taskManagerService = new TaskManagerFlowControllerService(taskContainer);
                        flowControllerServicesContainer.AddService(taskManagerService);
                    }

                    try
                    {
                        if ((actionExecutor is IActionExecutorSerializedParameters) == true)
                        {
                            string serializedEntityToken = EntityTokenSerializer.Serialize(entityToken);
                            string serializedActionToken = ActionTokenSerializer.Serialize(actionToken);

                            flowToken = Execute(actionExecutor as IActionExecutorSerializedParameters,
                                                serializedEntityToken, serializedActionToken, actionToken,
                                                flowControllerServicesContainer);
                        }
                        else
                        {
                            flowToken = Execute(actionExecutor, entityToken, actionToken,
                                                flowControllerServicesContainer);
                        }
                    }
                    finally
                    {
                        if (taskManagerService != null)
                        {
                            flowControllerServicesContainer.RemoveService(taskManagerService);
                        }
                    }

                    taskContainer.SetOnIdleTaskManagerEvent(new FlowTaskManagerEvent(flowToken));
                    taskContainer.UpdateTasksWithFlowToken(flowToken);
                }

                ActionEventSystemFacade.FireOnAfterActionExecution(entityToken, actionToken, flowToken);

                IManagementConsoleMessageService managementConsoleMessageService = flowControllerServicesContainer.GetService<IManagementConsoleMessageService>();
                if (managementConsoleMessageService != null)
                {
                    FlowControllerFacade.RegisterNewFlowInformation(flowToken, entityToken, actionToken, managementConsoleMessageService.CurrentConsoleId);
                }
                else
                {
                    LoggingService.LogWarning("ActionExecutorFacade", "Missing ManagementConsoleMessageService, can not register the flow");
                }

                return flowToken;
            }
            else
            {
                return ExecuteEntityTokenLocked(actionToken, entityToken, flowControllerServicesContainer);
            }
        }



        public static FlowToken ExecuteEntityTokenLocked(ActionToken lockedActionToken, EntityToken lockedEntityToken, FlowControllerServicesContainer flowControllerServicesContainer)
        {
            EntityToken entityToken = new EntityTokenLockedEntityToken(
                    ActionLockingFacade.LockedBy(lockedEntityToken),
                    ActionTokenSerializer.Serialize(lockedActionToken),
                    EntityTokenSerializer.Serialize(lockedEntityToken)
                );

            WorkflowActionToken actionToken = new WorkflowActionToken(WorkflowFacade.GetWorkflowType("Composite.Actions.Workflows.EntityTokenLockedWorkflow"));

            return Execute(entityToken, actionToken, flowControllerServicesContainer);
        }



        public static FlowToken ExecuteSecurityViolation(ActionToken actionToken, EntityToken entityToken, FlowControllerServicesContainer flowControllerServicesContainer)
        {
            EntityToken newEntityToken = new SecurityViolationWorkflowEntityToken();

            WorkflowActionToken newActionToken = new WorkflowActionToken(WorkflowFacade.GetWorkflowType("Composite.Actions.Workflows.SecurityViolationWorkflow"));

            return Execute(newEntityToken, newActionToken, flowControllerServicesContainer);
        }



        private static FlowToken Execute(IActionExecutorSerializedParameters actionExecutor, string serializedEntityToken, string serializedActionToken, ActionToken actionToken, FlowControllerServicesContainer flowControllerServicesContainer)
        {
            FlowToken result = actionExecutor.Execute(serializedEntityToken, serializedActionToken, actionToken, flowControllerServicesContainer);

            if (result == null)
            {
                result = new NullFlowToken();
            }

            return result;
        }



        private static FlowToken Execute(IActionExecutor actionExecutor, EntityToken entityToken, ActionToken actionToken, FlowControllerServicesContainer flowControllerServicesContainer)
        {
            FlowToken result = actionExecutor.Execute(entityToken, actionToken, flowControllerServicesContainer);

            if (result == null)
            {
                result = new NullFlowToken();
            }

            return result;
        }
    }
}
