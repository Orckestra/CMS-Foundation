﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.UI;
using Composite.Forms;
using Composite.Forms.CoreUiControls;
using Composite.Forms.Plugins.UiControlFactory;
using Composite.Forms.WebChannel;
using Composite.StandardPlugins.Forms.WebChannel.Foundation;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.ObjectBuilder;
using Microsoft.Practices.ObjectBuilder;


namespace Composite.StandardPlugins.Forms.WebChannel.UiControlFactories
{
    public abstract class InfoTableTemplateUserControlBase : UserControl
    {
        protected abstract void InitializeViewState();

        internal void InitializeWebViewState()
        {
            this.InitializeViewState();
        }

        public List<string> Headers { get; set; }

        public List<List<string>> Rows { get; set; }

        public string Caption { get; set; }

        public bool Border { get; set; }

        public string FormControlLabel { get; set; }
        
    }

    internal sealed class TemplatedInfoTableUiControl : InfoTableUiControl, IWebUiControl
    {
        private Type _userControlType;
        private InfoTableTemplateUserControlBase _userControl;

        internal TemplatedInfoTableUiControl(Type userControlType)
        {
            _userControlType = userControlType;
        }

        public override void BindStateToControlProperties()
        {
        }

        public void InitializeViewState()
        {
            _userControl.InitializeWebViewState();
        }


        public Control BuildWebControl()
        {
            _userControl = _userControlType.ActivateAsUserControl<InfoTableTemplateUserControlBase>(this.UiControlID);

            _userControl.FormControlLabel = this.Label;
            _userControl.Headers = this.Headers;
            _userControl.Rows = this.Rows;
            _userControl.Caption = this.Caption;
            _userControl.Border = this.Border;

            return _userControl;
        }

        public bool IsFullWidthControl { get { return false; } }

        public string ClientName { get { return null; } }
    }


    [ConfigurationElementType(typeof(TemplatedInfoTableUiControlFactoryData))]
    internal sealed class TemplatedInfoTableUiControlFactory : Base.BaseTemplatedUiControlFactory
    {
        public TemplatedInfoTableUiControlFactory(TemplatedInfoTableUiControlFactoryData data)
            : base(data)
        { }

        public override IUiControl CreateControl()
        {
            TemplatedInfoTableUiControl control = new TemplatedInfoTableUiControl(this.UserControlType);

            return control;
        }
    }


    [Assembler(typeof(TemplatedInfoTableUiControlFactoryAssembler))]
    internal sealed class TemplatedInfoTableUiControlFactoryData : UiControlFactoryData, Base.ITemplatedUiControlFactoryData
    {
        private const string _userControlVirtualPathPropertyName = "userControlVirtualPath";
        private const string _cacheCompiledUserControlTypePropertyName = "cacheCompiledUserControlType";

        [ConfigurationProperty(_userControlVirtualPathPropertyName, IsRequired = true)]
        public string UserControlVirtualPath
        {
            get { return (string)base[_userControlVirtualPathPropertyName]; }
            set { base[_userControlVirtualPathPropertyName] = value; }
        }

        [ConfigurationProperty(_cacheCompiledUserControlTypePropertyName, IsRequired = false, DefaultValue = true)]
        public bool CacheCompiledUserControlType
        {
            get { return (bool)base[_cacheCompiledUserControlTypePropertyName]; }
            set { base[_cacheCompiledUserControlTypePropertyName] = value; }
        }
    }

    internal sealed class TemplatedInfoTableUiControlFactoryAssembler : IAssembler<IUiControlFactory, UiControlFactoryData>
    {
        public IUiControlFactory Assemble(IBuilderContext context, UiControlFactoryData objectConfiguration, IConfigurationSource configurationSource, ConfigurationReflectionCache reflectionCache)
        {
            return new TemplatedInfoTableUiControlFactory(objectConfiguration as TemplatedInfoTableUiControlFactoryData);
        }
    }
}
