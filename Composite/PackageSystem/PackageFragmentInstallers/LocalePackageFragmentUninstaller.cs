﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Composite.ResourceSystem;
using System.Globalization;
using Composite.Localization;
using Composite.Logging;


namespace Composite.PackageSystem.PackageFragmentInstallers
{
    public sealed class LocalePackageFragmentUninstaller : BasePackageFragmentUninstaller
    {
        private List<CultureInfo> _culturesToUninstall = null;
        private CultureInfo _oldDefaultCultureInfo = null;

        public override IEnumerable<PackageFragmentValidationResult> Validate()
        {
            List<PackageFragmentValidationResult> validationResults = new List<PackageFragmentValidationResult>();

            if (this.Configuration.Where(f => f.Name == "Locales").Count() > 1)
            {
                validationResults.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "VirtualElementProviderNodeAddOnFragmentUninstaller.OnlyOneElement")));
                return validationResults;
            }

            XElement localesElement = this.Configuration.Where(f => f.Name == "Locales").SingleOrDefault();
            if (localesElement == null)
            {
                return validationResults;
            }

            _culturesToUninstall = new List<CultureInfo>();

            XAttribute oldDefaultAttribute = localesElement.Attribute("oldDefault");
            if (oldDefaultAttribute != null)
            {
                _oldDefaultCultureInfo = CultureInfo.CreateSpecificCulture(oldDefaultAttribute.Value);
            }

            foreach (XElement localeElement in localesElement.Elements("Locale").Reverse())
            {
                CultureInfo locale = CultureInfo.CreateSpecificCulture(localeElement.Attribute("name").Value);

                if ((_oldDefaultCultureInfo == null) && (LocalizationFacade.IsDefaultLocale(locale) == true))
                {
                    // Locale is default -> not possible to unintall
                    validationResults.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "VirtualElementProviderNodeAddOnFragmentUninstaller.OnlyOneElement")));
                    continue;
                }

                if (LocalizationFacade.IsOnlyActiveLocaleForSomeUsers(locale) == true)
                {
                    // only active for the a user
                    validationResults.Add(new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, StringResourceSystemFacade.GetString("Composite.PackageSystem.PackageFragmentInstallers", "VirtualElementProviderNodeAddOnFragmentUninstaller.OnlyOneElement")));
                    continue;
                }

                if (LocalizationFacade.IsLocaleInstalled(locale) == true)
                {
                    _culturesToUninstall.Add(locale);
                }
            }            

            return validationResults;
        }



        public override void Uninstall()
        {
            if (_oldDefaultCultureInfo != null)
            {
                LoggingService.LogVerbose("LocalePackageFragmentUninstaller", string.Format("Restoring default locale to '{0}'", _oldDefaultCultureInfo));

                LocalizationFacade.SetDefaultLocale(_oldDefaultCultureInfo);
            }


            foreach (CultureInfo locale in _culturesToUninstall.Reverse<CultureInfo>())
            {
                LoggingService.LogVerbose("LocalePackageFragmentUninstaller", string.Format("Removing the locale '{0}'", locale));

                LocalizationFacade.RemoveLocale(locale, false);
            }
        }
    }
}
