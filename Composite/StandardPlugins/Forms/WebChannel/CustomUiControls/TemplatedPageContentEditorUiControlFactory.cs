﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Compilation;
using System.Web.UI;
using Composite.Forms;
using Composite.Forms.Foundation;
using Composite.Forms.Plugins.UiControlFactory;
using Composite.Forms.WebChannel;
using Composite.StandardPlugins.Forms.WebChannel.Foundation;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration.ObjectBuilder;
using Microsoft.Practices.ObjectBuilder;


namespace Composite.StandardPlugins.Forms.WebChannel.CustomUiControls
{
    public abstract class PageContentEditorTemplateUserControlBase : UserControl
    {
        protected abstract void BindStateToProperties();

        protected abstract void InitializeViewState();

        public abstract string GetDataFieldClientName();

        internal void BindStateToControlProperties()
        {
            this.BindStateToProperties();
        }

        internal void InitializeWebViewState()
        {
            this.InitializeViewState();
        }

        public Guid TemplateId { get; set; }

        public List<KeyValuePair<Guid, string>> SelectableTemplateIds { get; set; }

        public Dictionary<string, string> NamedXhtmlFragments { get; set; }

        public string FormControlLabel { get; set; }

        public string ClassConfigurationName { get; set; }
    }

    internal sealed class TemplatedPageContentEditorUiControl : UiControl, IWebUiControl
    {
        [FormsProperty()]
        [BindableProperty()]
        public Guid TemplateId { get; set; }

        [FormsProperty()]
        [BindableProperty()]
        public List<KeyValuePair<Guid, string>> SelectableTemplateIds { get; set; }

        [FormsProperty()]
        [BindableProperty()]
        public Dictionary<string, string> NamedXhtmlFragments { get; set; }

        [FormsProperty()]
        public string ClassConfigurationName { get; set; }


        private Type _userControlType;
        private PageContentEditorTemplateUserControlBase _userControl;

        internal TemplatedPageContentEditorUiControl(Type userControlType)
        {
            _userControlType = userControlType;
            this.SelectableTemplateIds = new List<KeyValuePair<Guid, string>>();
            this.NamedXhtmlFragments = new Dictionary<string, string>();
        }

        public override void BindStateToControlProperties()
        {
            _userControl.BindStateToControlProperties();
            this.SelectableTemplateIds = new List<KeyValuePair<Guid, string>>();
            this.NamedXhtmlFragments = _userControl.NamedXhtmlFragments;
            this.TemplateId = _userControl.TemplateId;
        }

        public void InitializeViewState()
        {
            _userControl.InitializeWebViewState();
        }


        public Control BuildWebControl()
        {
            _userControl = _userControlType.ActivateAsUserControl<PageContentEditorTemplateUserControlBase>(this.UiControlID);

            _userControl.FormControlLabel = this.Label;
            _userControl.TemplateId = this.TemplateId;
            _userControl.SelectableTemplateIds = this.SelectableTemplateIds;
            _userControl.NamedXhtmlFragments = this.NamedXhtmlFragments;
            _userControl.ClassConfigurationName = this.ClassConfigurationName;

            return _userControl;
        }

        public bool IsFullWidthControl { get { return true; } }

        public string ClientName { get { return _userControl.GetDataFieldClientName(); } }
    }



    [ConfigurationElementType(typeof(TemplatedPageContentEditorUiControlFactoryData))]
    internal sealed class TemplatedPageContentEditorUiControlFactory : IUiControlFactory
    {
        private TemplatedPageContentEditorUiControlFactoryData _data;
        private Type _cachedUserControlType = null;

        public TemplatedPageContentEditorUiControlFactory(TemplatedPageContentEditorUiControlFactoryData data)
        {
            _data = data;

            if (_data.CacheCompiledUserControlType == true)
            {
                _cachedUserControlType = System.Web.Compilation.BuildManager.GetCompiledType(_data.UserControlVirtualPath);
            }
        }

        public IUiControl CreateControl()
        {
            Type userControlType = _cachedUserControlType;

            if (userControlType == null && System.Web.HttpContext.Current != null)
            {
                userControlType = BuildManager.GetCompiledType(_data.UserControlVirtualPath);
            }

            TemplatedPageContentEditorUiControl control = new TemplatedPageContentEditorUiControl(userControlType);

            control.ClassConfigurationName = _data.ClassConfigurationName;

            return control;
        }
    }

    [Assembler(typeof(TemplatedPageContentEditorUiControlFactoryAssembler))]
    internal sealed class TemplatedPageContentEditorUiControlFactoryData : UiControlFactoryData
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

        private const string _classConfigurationNamePropertyName = "ClassConfigurationName";
        [ConfigurationProperty(_classConfigurationNamePropertyName, IsRequired = false, DefaultValue = "common")]
        public string ClassConfigurationName
        {
            get { return (string)base[_classConfigurationNamePropertyName]; }
            set { base[_classConfigurationNamePropertyName] = value; }
        }

    }

    internal sealed class TemplatedPageContentEditorUiControlFactoryAssembler : IAssembler<IUiControlFactory, UiControlFactoryData>
    {
        public IUiControlFactory Assemble(IBuilderContext context, UiControlFactoryData objectConfiguration, IConfigurationSource configurationSource, ConfigurationReflectionCache reflectionCache)
        {
            return new TemplatedPageContentEditorUiControlFactory(objectConfiguration as TemplatedPageContentEditorUiControlFactoryData);
        }
    }


}
