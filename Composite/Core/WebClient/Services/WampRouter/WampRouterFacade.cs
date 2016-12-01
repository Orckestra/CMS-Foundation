using Composite.Core.Application;

namespace Composite.Core.WebClient.Services.WampRouter
{
    /// <summary>
    /// Wamp Router Facade for registering clients
    /// </summary>
    public static class WampRouterFacade
    {
        /// <summary>
        /// Method for registering callee
        /// </summary>
        /// <param name="realmName"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static bool RegisterCallee(string realmName, IRpcService instance)
        {
            var wampRouter = ServiceLocator.GetRequiredService<WampRouter>();
            if (wampRouter == null)
                return false;
            wampRouter.RegisterCallee(realmName,instance);
            return true;
        }

        /// <summary>
        /// Method for registering publisher
        /// </summary>
        /// <param name="realmName"></param>
        /// <param name="topicName"></param>
        /// <param name="eventObservable"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool RegisterPublisher<T1,T2>(string realmName, string topicName, IWampEventHandler<T1,T2> eventObservable)
        {
            var wampRouter = ServiceLocator.GetRequiredService<WampRouter>();
            if (wampRouter == null)
                return false;
            wampRouter.RegisterPublisher(realmName, topicName, eventObservable);
            return true;
        }
    }
}