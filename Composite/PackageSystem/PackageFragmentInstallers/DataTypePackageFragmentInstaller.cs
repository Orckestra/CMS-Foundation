﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Composite.Data;
using Composite.Data.DynamicTypes;
using Composite.EventSystem;
using Composite.Logging;
using Composite.ResourceSystem;
using Composite.Types;


namespace Composite.PackageSystem.PackageFragmentInstallers
{
    public sealed class DataTypePackageFragmentInstaller : BasePackageFragmentInstaller
	{
        private List<DataTypeDescriptor> _typesToInstall = null;

        public override IEnumerable<PackageFragmentValidationResult> Validate()
        {
            List<PackageFragmentValidationResult>  validationResult = new List<PackageFragmentValidationResult>();

            if (this.Configuration.Where(f => f.Name == "Types").Count() > 1)
            {
                validationResult.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "DataTypeAddOnFragmentInstaller.OnlyOneElement")));
                return validationResult;
            }

            XElement typesElement = this.Configuration.Where(f => f.Name == "Types").SingleOrDefault();
            if (typesElement == null)
            {
                validationResult.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "DataTypeAddOnFragmentInstaller.MissingElement")));
                return validationResult;
            }

            _typesToInstall = new List<DataTypeDescriptor>();

            foreach (XElement typeElement in typesElement.Elements("Type"))
            {
                XAttribute nameAttribute = typeElement.Attribute("name");
                if (nameAttribute == null)
                {
                    validationResult.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, string.Format(StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "DataTypeAddOnFragmentInstaller.MissingAttribute"), "name"), typeElement));
                    continue;
                }

                Type type = TypeManager.TryGetType(nameAttribute.Value);
                if (type == null)
                {
                    validationResult.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, string.Format(StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "DataTypeAddOnFragmentInstaller.TypeNotConfigured"), nameAttribute.Value)));
                }
                else if (typeof(IData).IsAssignableFrom(type) == false)
                {
                    validationResult.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, string.Format(StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "DataTypeAddOnFragmentInstaller.TypeNotInheriting"), type, typeof(IData))));
                }
                else if (DataFacade.GetAllKnownInterfaces().Contains(type) == true)
                {
                    validationResult.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, string.Format(StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "DataTypeAddOnFragmentInstaller.TypeExists"), type)));
                }
                else
                {
                    DataTypeDescriptor dataTypeDescriptor = null;
                    try
                    {
                        dataTypeDescriptor = DynamicTypeManager.GetDataTypeDescriptor(type);
                        dataTypeDescriptor.Validate();
                    }
                    catch
                    {
                        validationResult.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, string.Format(StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "DataTypeAddOnFragmentInstaller.InterfaceCodeError"), type)));
                    }

                    if (dataTypeDescriptor != null)
                    {
                        _typesToInstall.Add(dataTypeDescriptor);
                        this.AddOnInstallerContex.AddPendingDataType(type);
                    }
                }
            }

            if (validationResult.Count > 0)
            {
                _typesToInstall = null;
            }

            return validationResult;
        }



        public override IEnumerable<XElement> Install()
        {
            if (_typesToInstall == null) throw new InvalidOperationException("Has not been validated");

            List<XElement> typeElements = new List<XElement>();
            foreach (DataTypeDescriptor dataTypeDescriptor in _typesToInstall)
            {
                LoggingService.LogVerbose("DataTypeAddOnFragmentInstaller", string.Format("Installing the type '{0}'", dataTypeDescriptor));

                DynamicTypeManager.CreateStore(dataTypeDescriptor, false);

                XElement typeElement = new XElement("Type", new XAttribute("typeId", dataTypeDescriptor.DataTypeId));
                typeElements.Add(typeElement);
            }

            GlobalEventSystemFacade.FlushTheSystem(true);

            yield return new XElement("Types", typeElements);
        }
    }
}
