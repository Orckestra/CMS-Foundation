﻿using System;
using System.IO;
using System.Xml.Linq;
using Composite.IO.Zip;


namespace Composite.PackageSystem.Foundation
{
	internal static class XmlHelper
	{
        public static PackageFragmentValidationResult LoadInstallXml(string zipFilename, out XElement installElement)
        {
            installElement = null;

            ZipFileSystem zipFileSystem = null;
            try
            {
                zipFileSystem = new ZipFileSystem(zipFilename);
            }
            catch (Exception ex)
            {
                return new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, ex);
            }

            string filename = string.Format("~/{0}", PackageSystemSettings.InstallFilename);
            if (zipFileSystem.ContainsFile(filename) == false)
            {
                return new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, string.Format("Installation file '{0}' is missing from the zip file", filename));
            }

            try
            {
                using (StreamReader streamReader = new StreamReader(zipFileSystem.GetFileStream(filename)))
                {
                    string fileContent = streamReader.ReadToEnd();
                    installElement = XElement.Parse(fileContent);
                }
            }
            catch (Exception ex)
            {
                return new PackageFragmentValidationResult(PackageFragmentValidationResultType.Fatal, ex);
            }

            return null;
        }
	}
}
