using System;
using Composite.StandardPlugins.Forms.WebChannel.UiControlFactories;


namespace CompositeFunctionParameterDesigner
{
    public partial class FunctionParameterDesigner : FunctionParameterDesignerTemplateUserControlBase
    {
        public override string SessionStateProvider
        {
            get { return ViewState["SessionStateProvider"] as string; }
            set { ViewState["SessionStateProvider"] = value; }
        }

        public override Guid SessionStateId
        {
            get { return (Guid)ViewState["SessionStateId"]; }
            set { ViewState["SessionStateId"] = value; }
        }
    }
}