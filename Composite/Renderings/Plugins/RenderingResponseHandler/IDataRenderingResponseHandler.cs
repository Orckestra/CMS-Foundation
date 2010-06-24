﻿using Composite.Data;


namespace Composite.Renderings.Plugins.RenderingResponseHandler
{
    public interface IDataRenderingResponseHandler : IRenderingResponseHandler
	{
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestedItemEntityToken"></param>
        /// <returns>Returning null if no special handling should be done.</returns>
        RenderingResponseHandlerResult GetDataResponseHandling(DataEntityToken requestedItemEntityToken);
	}
}
