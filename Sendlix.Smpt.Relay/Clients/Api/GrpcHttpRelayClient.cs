using System.Net.Http.Headers;
using System.Reflection;

namespace Sendlix.Smpt.Relay.Clients.Api
{
    internal class GrpcHttpRelayClient : HttpMessageHandler
    {

        private readonly HttpMessageInvoker invoker;
        public string UserAgent { get; }
        public GrpcHttpRelayClient(string userAgent)
        {
            HttpMessageHandler primaryHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };
            invoker = new HttpMessageInvoker(primaryHandler, false);
            UserAgent = userAgent;
        }


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ProductHeaderValue header = new("sendlix-smtp-relay", Assembly.GetExecutingAssembly().GetName()?.Version?.ToString());
            var userAgent = new ProductInfoHeaderValue(header);
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(userAgent);

            return invoker.SendAsync(request, cancellationToken);

        }
    }
}
