using Appicket.Analytics.Loggers;
using System.ServiceModel.Description;

namespace Appicket.Analytics.Extensions
{
    public static class Extensions
    {
        public static void AddLoggingEndpointBehaviour(this ServiceEndpoint endpoint)
        {
            endpoint.EndpointBehaviors.Add(new LoggingEndpointBehaviour());
        }
    }
}