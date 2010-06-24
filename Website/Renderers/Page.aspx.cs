﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;

using Composite.Data;
using Composite.Data.Types;
using Composite.Renderings;
using Composite.Renderings.Page;
using Composite.Security;
using Composite.WebClient;

using TidyNet;
using System.Collections;


public partial class Renderers_Page : System.Web.UI.Page
{
    private IDisposable _dataScope;

    private PageUrlOptions _urlOptions;
    private NameValueCollection _foreignQueryStringParameters;
    private string _cacheUrl = null;


    public Renderers_Page()
    {
        string query = HttpContext.Current.Request.Url.OriginalString;
        NameValueCollection queryParameters = new UrlString(query).GetQueryParameters();

        _urlOptions = PageUrlHelper.ParseQueryString(queryParameters, out _foreignQueryStringParameters);

        if (_urlOptions.DataScopeIdentifierName != DataScopeIdentifier.PublicName)
        {
            if (UserValidationFacade.IsLoggedIn() == false)
            {
                HttpContext.Current.Response.Redirect(string.Format("/Composite/Login.aspx?ReturnUrl={0}", HttpUtility.UrlEncodeUnicode(HttpContext.Current.Request.Url.OriginalString)));
                HttpContext.Current.Response.End();
                return;
            }
        }

       _cacheUrl = HttpContext.Current.Request.Url.PathAndQuery;

        using (new DataScope(_urlOptions.DataScopeIdentifier, _urlOptions.Locale))
        {
            RewritePath();
        }
    }



    protected void Page_Init(object sender, EventArgs e)
    {
        if (_urlOptions.DataScopeIdentifierName != DataScopeIdentifier.PublicName)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
        }

        _dataScope = new DataScope(_urlOptions.DataScopeIdentifier, _urlOptions.Locale); // IDisposable, Disposed in OnUnload

        IPage page = PageManager.GetPageById(_urlOptions.PageId);

        if (page != null)
        {
            RenderingResponseHandlerResult responseHandling = RenderingResponseHandlerFacade.GetDataResponseHandling(page.GetDataEntityToken());

            if ((responseHandling != null) && (responseHandling.PreventPublicCaching == true))
            {
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            if ((responseHandling != null) &&
                (responseHandling.EndRequest || responseHandling.RedirectRequesterTo != null))
            {
                if (responseHandling.RedirectRequesterTo != null)
                {
                    Response.Redirect(responseHandling.RedirectRequesterTo.AbsoluteUri, false);
                }

                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            IEnumerable<IPagePlaceholderContent> contents = PageManager.GetPlaceholderContent(page.Id);
            Control renderedPage = PageRenderer.Render(page, contents);
            this.Controls.Add(renderedPage);
            if (this.Form != null) this.Form.Action = Request.RawUrl;
        }
        else
        {
            // GUID not found in lookup
            this.Controls.Add(new LiteralControl("<div>Unknown page id - either this page has not been published yet or it has been deleted.</div>"));
        }
    }


    private void RewritePath()
    {
        UrlString structuredUrl = PageUrlHelper.BuildUrl(UrlType.Public, _urlOptions);

        if (structuredUrl == null)
        {
            return;
        }

        structuredUrl.AddQueryParameters(_foreignQueryStringParameters);

        HttpContext.Current.RewritePath(structuredUrl.FilePath, structuredUrl.PathInfo, structuredUrl.QueryString);
    }


    protected override void Render(HtmlTextWriter writer)
    {
        ScriptManager scriptManager = ScriptManager.GetCurrent(this);
        bool isUpdatePanelPostback = scriptManager != null && scriptManager.IsInAsyncPostBack;

        if (isUpdatePanelPostback == true)
        {
            base.Render(writer);
            return;
        }

        StringBuilder markupBuilder = new StringBuilder();
        StringWriter sw = new StringWriter(markupBuilder);
        base.Render(new HtmlTextWriter(sw));

        bool tidyOutput = true;
        if (tidyOutput)
        {
            markupBuilder = markupBuilder.Replace("</SCRIPT>", "</script>"); // Fix for a TidyNet issue with parsing of <SCRIPT></SCRIPT> tags
        }

        string xhtml = PageUrlHelper.ChangeRenderingPageUrlsToPublic(markupBuilder.ToString());

        if (tidyOutput)
        {
            string declarations = "";
            int htmlElementStart = xhtml.IndexOf("<html", StringComparison.InvariantCultureIgnoreCase);
            if (htmlElementStart > 0) declarations = xhtml.Substring(0, htmlElementStart) + "\n";

            byte[] htmlByteArray = Encoding.UTF8.GetBytes(xhtml);
            using (MemoryStream inputStream = new MemoryStream(htmlByteArray))
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    Tidy tidy = GetXhtmlConfiguredTidy();
                    TidyMessageCollection tidyMessages = new TidyMessageCollection();

                    try
                    {
                        tidy.Parse(inputStream, outputStream, tidyMessages);
                        if (tidyMessages.Errors == 0)
                        {
                            outputStream.Position = 0;
                            StreamReader sr = new StreamReader(outputStream);
                            xhtml = declarations + sr.ReadToEnd();
                        }
                    }
                    catch (Exception) { } // Tidy clean up failures supressed
                }
            }
        }

        // Request.UserAgent is null when the request is called from custom code
        if (Composite.RuntimeInformation.IsDebugBuild == true 
            && Request.UserAgent != null 
            && Request.UserAgent.Contains("Gecko"))
        {
            Response.ContentType = "application/xhtml+xml";
        }

        writer.Write(xhtml);
    }

    protected override void OnUnload(EventArgs e)
    {
        base.OnUnload(e);

        if (_dataScope != null)
            _dataScope.Dispose();

        // Rewrite path to what it was when this page was constructed. This ensure full page caching can work.
        HttpContext.Current.RewritePath(_cacheUrl);
    }



    private static Tidy GetXhtmlConfiguredTidy()
    {
        Tidy t = new Tidy();

        t.Options.RawOut = true;
        t.Options.TidyMark = false;

        t.Options.CharEncoding = CharEncoding.UTF8;
        t.Options.DocType = DocType.Omit;
        t.Options.AllowElementPruning = false;
        t.Options.WrapLen = 0;
        t.Options.TabSize = 2;
        t.Options.Spaces = 4;
        t.Options.SmartIndent = true;

        t.Options.BreakBeforeBR = false;
        t.Options.DropEmptyParas = false;
        t.Options.Word2000 = false;
        t.Options.MakeClean = false;
        t.Options.Xhtml = true;
        t.Options.XmlOut = false;
        t.Options.XmlTags = false;

        t.Options.QuoteNbsp = false;
        t.Options.NumEntities = true;

        return t;
    }
}
