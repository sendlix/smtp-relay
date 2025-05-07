using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Handler;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    internal class SSLClient(TcpClient tcpClient, X509Certificate certificate, IHandler handler, ILoggerFactory factory) : IClient 
    {
        private readonly ILogger logger = factory.CreateLogger<SSLClient>();

        private Client? client;

        public void Dispose() {            
            client?.Dispose();
        }

        public async Task HandleClient(CancellationToken cancellationToken = default)
        {
            SslStream sslStream = new(tcpClient.GetStream(), false);
            try
            {
                sslStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Tls12, true);
                logger.LogDebug("SSL handshake completed");
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error during SSL handshake");
            }

             client = new(tcpClient, sslStream, handler, factory);
            await client.HandleClient(cancellationToken);
        }
    }
}
