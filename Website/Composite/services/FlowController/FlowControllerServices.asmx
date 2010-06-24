<%@ WebService Language="C#" Class="FlowControllerServices" %>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Services;
using System.Web.Services.Protocols;

using Composite.WebClient.FlowMediators;
using Composite.WebClient.Services.TreeServiceObjects;
using Composite.WebClient.Services.ConsoleMessageService;
using Composite.ConsoleEventSystem;
using Composite.Actions;


[WebService(Namespace = "http://www.composite.net/ns/management")]
[SoapDocumentService(RoutingStyle = SoapServiceRoutingStyle.RequestElement)]
public class FlowControllerServices  : System.Web.Services.WebService {

    [WebMethod]
    public bool CancelFlow(string serializedFlowHandle) 
    {
        FlowHandle flowHandle = FlowHandle.Deserialize(serializedFlowHandle);
        try
        {
            FlowControllerFacade.CancelFlow(flowHandle.FlowToken);
        }
        catch (Exception)
        {
            return false;
        }
        
        return true;
    }

    
    
    [WebMethod]
    public bool ReleaseAllConsoleResources(string consoleId)
    {
        if (string.IsNullOrEmpty(consoleId)) throw new ArgumentNullException("consoleId");

        Composite.Logging.LoggingService.LogVerbose("FlowControllerServices.asmx", "ReleaseAllConsoleResources for " + consoleId);
        ConsoleFacade.CloseConsole(consoleId);        

        return true;
    }
    
}

