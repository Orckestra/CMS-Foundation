﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Composite.Data;
using Composite.Data.Types;
using Composite.IO;


namespace Composite.PackageSystem
{
    public enum ServerUrlValidationResult
    {
        Http,
        Https,
        Invalid
    }



    public static class PackageServerFacade 
	{
        private static IPackageServerFacade _packageServerFacade;


        static PackageServerFacade()
        {
            string testFilePath = Path.Combine(PathUtil.Resolve(PathUtil.BaseDirectory), "App_Data/Composite/AddOnDescriptions.xml");
            if (File.Exists(testFilePath) == true)
            {
                _packageServerFacade = new PackageServerFacadeLocalMock(testFilePath);
            }
            else
            {
                _packageServerFacade = new PackageServerFacadeImpl();
            }
        }



        internal static IPackageServerFacade Implementation { get { return _packageServerFacade; } set { _packageServerFacade = value; } }

        /// <summary>
        /// Connects to the server using either https or http and 
        /// checks to see if it supports the needed services
        /// </summary>
        /// <param name="packageServerUrl">
        /// Cleaned url (ex: www.composite.net)
        /// </param>
        /// <returns>
        /// </returns>
        public static ServerUrlValidationResult ValidateServerUrl(string packageServerUrl)
        {
            return _packageServerFacade.ValidateServerUrl(packageServerUrl);
        }



        /// <summary>
        /// Given a cleaned url, returns a series of AddOnDescriptions <see cref="AddOnDescription"/>
        /// </summary>
        /// <param name="packageServerUrl">
        /// Cleaned url (ex: www.composite.net)
        /// </param>
        /// <returns></returns>
        public static IEnumerable<PackageDescription> GetPackageDescriptions(string packageServerUrl, Guid installationId, CultureInfo userCulture)
        {
            return _packageServerFacade.GetAddOnDescriptions(packageServerUrl, installationId, userCulture);
        }



        // Overload
        public static IEnumerable<PackageDescription> GetAllPackageDescriptions(Guid installationId, CultureInfo userCulture)
        {
            List<IPackageServerSource> packageServerSources = DataFacade.GetData<IPackageServerSource>().ToList();

            foreach (IPackageServerSource packageServerSource in packageServerSources)
            {
                foreach (PackageDescription packageDescription in GetPackageDescriptions(packageServerSource.Url, installationId, userCulture))
                {
                    yield return packageDescription;
                }
            }
        }



        public static string GetEulaText(string packageServerUrl, Guid eulaId, CultureInfo userCulture)
        {
            return _packageServerFacade.GetEulaText(packageServerUrl, eulaId, userCulture);
        }



        public static Stream GetInstallFileStream(string packageFileDownloadUrl)
        {
            return _packageServerFacade.GetInstallFileStream(packageFileDownloadUrl);
        }



        public static bool RequestLicenseUpdate(string packageServerUrl, Guid installationId, Guid packageId, string localUserName, string localUserIp)
        {
            return _packageServerFacade.RequestLicenseUpdate(packageServerUrl, installationId, packageId, localUserName, localUserIp);
        }



        public static void RegisterAddonInstallationCompletion(string packageServerUrl, Guid installationId, Guid packageId, string localUserName, string localUserIp)
        {
            _packageServerFacade.RegisterAddonInstallationCompletion(packageServerUrl, installationId, packageId, localUserName, localUserIp);
        }



        public static void RegisterAddonInstallationFailure(string packageServerUrl, Guid installationId, Guid packageId, string localUserName, string localUserIp, string exceptionString)
        {
            _packageServerFacade.RegisterAddonInstallationFailure(packageServerUrl, installationId, packageId, localUserName, localUserIp, exceptionString);
        }



        public static void RegisterAddOnUninstall(string packageServerUrl, Guid installationId, Guid packageId, string localUserName, string localUserIp)
        {
            _packageServerFacade.RegisterAddOnUninstall(packageServerUrl, installationId, packageId, localUserName, localUserIp);
        }



        public static void ClearServerCache()
        {
            _packageServerFacade.ClearCache();
        }
	}
}
