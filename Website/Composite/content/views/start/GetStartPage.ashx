﻿<%@ WebHandler Language="C#" Class="FetchPage" %>

using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Web;
using Composite.GlobalSettings;
using Composite;
using Composite.Users;
using Composite.WebClient;

public class FetchPage : IHttpHandler 
{
    public void ProcessRequest (HttpContext context) 
    {
        /// When public URL is known, remove this:
//        context.Response.Redirect( UrlUtils.ResolveAdminUrl( "content/views/start/compositestart/CompositeStart.aspx" ));
//        return;
        
        
        string userCultureName = UserSettings.CultureInfo.Name;
        string productVersion = RuntimeInformation.ProductVersion.ToString(4);
        string installationId = InstallationInformationFacade.InstallationId.ToString();
        string browser = context.Request.UserAgent.IndexOf("Gecko") > -1 ? "mozilla" : "explorer";
        string platform = context.Request.Browser.Platform;

        string baseUriString = System.Configuration.ConfigurationSettings.AppSettings["Composite.StartPage.Url"];
        string sourceUriString = string.Format("{0}?culture={1}&version={2}&installation={3}&browser={4}&platform={5}", 
            baseUriString,
            HttpUtility.UrlEncodeUnicode(userCultureName), 
            HttpUtility.UrlEncodeUnicode(productVersion), 
            HttpUtility.UrlEncodeUnicode(installationId),
            HttpUtility.UrlEncodeUnicode(browser),
            HttpUtility.UrlEncodeUnicode(platform));
       
        Uri sourceUri = new Uri(sourceUriString);

        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(sourceUri);
        request.UserAgent = context.Request.Headers["User-Agent"];
        request.Timeout = 2000;
        
        try
        {
            WebResponse webResponse = request.GetResponse();
            
            string contentType = webResponse.ContentType;
            
            using (Stream responseStream = webResponse.GetResponseStream())
            {
                StreamReader sr = new StreamReader(responseStream);
                string content = sr.ReadToEnd();

                context.Response.ContentType = contentType;
                context.Response.Write(content);
            }
        }
        catch( Exception )
        {
            context.Response.Redirect( UrlUtils.ResolveAdminUrl( "blank.aspx" ));
        }

    }
 
    public bool IsReusable 
    {
        get 
        {
            return false;
        }
    }

}