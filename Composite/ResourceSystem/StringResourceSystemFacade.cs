using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using Composite.Data.Caching;
using Composite.Types;
using Composite.ResourceSystem.Foundation;
using System.Linq;
using Composite.ResourceSystem.Foundation.PluginFacades;
using Composite.Logging;
using System.Globalization;
using Composite.GlobalSettings;
using System.Text;
using System.Threading;


namespace Composite.ResourceSystem
{
	public static class StringResourceSystemFacade
	{
        private static Regex _regex = new Regex(@"\$\{(?<id>.+?)\}", RegexOptions.Compiled);

        private static readonly int ResourceCacheSize = 5000;
        private static Cache<string, ExtendedNullable<string>> _resourceCache = new Cache<string, ExtendedNullable<string>>("Resource strings", ResourceCacheSize);


        public static void Initialize()
        {
            // touch the plug-in facade
            var temp = ResourceProviderRegistry.StringResourceProviderNames;
        }



        public static string GetString(string providerName, string stringName)
        {
            return GetString(providerName, stringName, true);
        }



        public static string GetString(string providerName, string stringName, bool throwOnError = true)
        {
            if (throwOnError == true)
            {
                Verify.ArgumentNotNullOrEmpty(providerName, "providerName");
                Verify.ArgumentNotNullOrEmpty(stringName, "stringName");
            }

            string cultureName = Thread.CurrentThread.CurrentCulture.Name;

            string cacheKey = cultureName + providerName + stringName;
            ExtendedNullable<string> cachedValue = _resourceCache.Get(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue.Value;
            }

            if (throwOnError == true)
            {
                Verify.ArgumentCondition(!providerName.Contains(','), "providerName", "providerName may not contain ',' symbol");
                Verify.ArgumentCondition(!stringName.Contains(','), "stringName", "stringName may not contain ',' symbol");
            }

            if (ResourceProviderRegistry.StringResourceProviderNames.Contains(providerName) == true)
            {
                string result = ResourceProviderPluginFacade.GetStringValue(providerName, stringName);
                _resourceCache.Add(cacheKey, new ExtendedNullable<string> {Value = result});

                return result;
            }

            if (throwOnError == true)
            {
                LoggingService.LogCritical("StringResourceSystemFacade", string.Format("String resource povider named '{0}' not found", providerName));

                return "*** PROVIDER NOT FOUND ***";
            }
            else
            {
                return null;
            }
        }



        public static bool TryGetString(string providerName, string stringName, out string resultString)
        {
            resultString = GetString(providerName, stringName, false);

            return resultString != null;
        }



        public static List<KeyValuePair> GetLocalization(string providerName)
        {
            if (string.IsNullOrEmpty(providerName) == true) throw new ArgumentNullException("providerName");
            if (providerName.Contains(',') == true) throw new ArgumentException("providerName may not contain ','");

            if (providerName == "XmlStringResourceProvider")
            {
                providerName = "Composite.Management";
            }

            if (ResourceProviderRegistry.StringResourceProviderNames.Contains(providerName))
            {
                IDictionary<string, string> translations = ResourceProviderPluginFacade.GetAllStrings(providerName);
                List<KeyValuePair> result = new List<KeyValuePair>();
                foreach (KeyValuePair<string, string> pair in translations)
                {
                    result.Add(new KeyValuePair(pair.Key, pair.Value));
                }

                return result;
            }
            else
            {
                Logging.LoggingService.LogCritical("Missing provider", string.Format("Provider not found: '{0}'", providerName));
                return new List<KeyValuePair>();
            }
        }


        public static IEnumerable<CultureInfo> GetSupportedCultures()
        {
            return ResourceProviderPluginFacade.GetSupportedStringCultures();
        }



        public static CultureInfo GetDefaultStringCulture()
        {
            return CultureInfo.CreateSpecificCulture("en-US");
        }


        /// <summary>
        /// Returns a (localized) list of cultures, where the value is "(region) / (language)", where (language) is the language 
        /// application users will get, if they select the specified culture.
        /// </summary>
        /// <returns>A list of (culture name, region/language label) </returns>
        public static List<KeyValuePair> GetApplicationRegionAndLanguageList()
        {
            IEnumerable<CultureInfo> cultures = GlobalSettingsFacade.ApplicationCultures;
            List<CultureInfo> supportedCultures = StringResourceSystemFacade.GetSupportedCultures().ToList();
            CultureInfo defaultCulture = StringResourceSystemFacade.GetDefaultStringCulture();

            List<KeyValuePair> options = new List<KeyValuePair>();
            List<KeyValuePair> translatedOptions = new List<KeyValuePair>();

            List<KeyValuePair> culturesLocalizedList = StringResourceSystemFacade.GetLocalization("Composite.Cultures");

            string defaultCultureDisplayName = culturesLocalizedList.Where(f => f.Key == defaultCulture.Name).Select(f => f.Value).FirstOrDefault();
            if (defaultCultureDisplayName == null) defaultCultureDisplayName = defaultCulture.EnglishName;

            foreach (CultureInfo culture in cultures)
            {
                string cultureDisplayName = culturesLocalizedList.Where(f => f.Key == culture.Name).Select(f => f.Value).FirstOrDefault();
                if (cultureDisplayName == null) cultureDisplayName = culture.EnglishName;

                if (supportedCultures.Contains(culture) == true)
                {
                    string listLabel = string.Format("{0} / {1}", cultureDisplayName, cultureDisplayName);
                    translatedOptions.Add(new KeyValuePair(culture.Name, listLabel));
                }
                else
                {
                    string listLabel = string.Format("{0} / {1}", cultureDisplayName, defaultCultureDisplayName);
                    options.Add(new KeyValuePair(culture.Name, listLabel));
                }
            }

            options = options.OrderBy(f => f.Value).ToList();

            options.InsertRange(0, translatedOptions);
//            options.Insert(translatedOptions.Count, new KeyValuePair("", " "));

            return options;
        }



        /// <summary>
        /// Parses the <paramref name="stringToParse"/> for any localization strings
        /// if there is any matches and these have a value, then the string is replaced
        /// the result string is returned.
        /// </summary>
        /// <param name="stringToParse"></param>
        /// <returns></returns>
        public static string ParseString(string stringToParse)
        {
            if (stringToParse == null) return null;

            MatchCollection matchCollection = _regex.Matches(stringToParse);

            string currentString = stringToParse;
            foreach (Match match in matchCollection)
            {
                string compositeName = match.Groups["id"].Value;

                if (string.IsNullOrWhiteSpace(compositeName) == true) continue;

                int idx = compositeName.LastIndexOf(',');
                if (idx == -1) continue;

                string resourceStoreName = compositeName.Remove(idx);
                string stringName = compositeName.Remove(0, idx + 1).TrimStart(' ');

                string replacement = GetString(resourceStoreName, stringName);

                currentString = currentString.Replace(match.Value, replacement);
            }

            return currentString;
        }


        public static IEnumerable<string> SplitParseableStrings(string stringToSplit, char separator)
        {
            if (string.IsNullOrEmpty(stringToSplit) == true)
            {
                yield break;
            }

            bool isInIgnoreSeparatorMode = false;
            StringBuilder sb = new StringBuilder();

            foreach( char c in stringToSplit)
            {
                if (isInIgnoreSeparatorMode == false && c == separator)
                {
                    yield return sb.ToString();
                    sb = new StringBuilder();
                }
                else
                {
                    if (c == '}')
                    {
                        isInIgnoreSeparatorMode = false;
                    }

                    if (c == '$')
                    {
                        isInIgnoreSeparatorMode = true;
                    }

                    sb.Append(c);
                }
            }

            yield return sb.ToString();
        }
    }
}
