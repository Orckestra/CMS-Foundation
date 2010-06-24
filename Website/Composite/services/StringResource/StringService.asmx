﻿<%@ WebService Language="C#" CodeBehind="~/App_Code/StringService.cs" Class="StringService" %>

using System;
using System.Linq;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Linq;
using System.Collections.Generic;
using Composite.Types;

[WebService(Namespace = "http://www.composite.net/ns/management")]
[SoapDocumentService(RoutingStyle = SoapServiceRoutingStyle.RequestElement)]
public class StringService : System.Web.Services.WebService
{

    public StringService()
    {

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }



    [WebMethod]
    public List<KeyValuePair> GetLocalisation(string resourceStoreName)
    {
        return Composite.ResourceSystem.StringResourceSystemFacade.GetLocalization(resourceStoreName);
    }
}