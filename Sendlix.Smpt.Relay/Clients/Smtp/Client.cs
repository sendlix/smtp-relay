using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Clients.Api;
using Sendlix.Smpt.Relay.Handler;
using System.Net.Sockets;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    /// <summary>
    /// Main class for SMTP communication
    /// </summary>
    public class Client : IClient, IDisposable
    {
        private readonly TcpClient tcpClient;
        private readonly SendlixApiClient sendlixClient;
        private readonly ILogger logger;
        private readonly SmtpReader smtpReader;
        private readonly SmtpWriter smtpWriter;
        private readonly SmtpAuthHandler authHandler;
        private readonly SmtpMailHandler mailHandler;
        private bool disposed;

        public Client(TcpClient tcpClient, Stream stream, SendlixApiClient client, ILoggerFactory factory)
        {
            this.tcpClient = tcpClient;
            sendlixClient = client;
            logger = factory.CreateLogger<Client>();

            smtpReader = new SmtpReader(stream, tcpClient, factory);
            smtpWriter = new SmtpWriter(stream, tcpClient, factory);
            authHandler = new SmtpAuthHandler(sendlixClient, smtpReader, smtpWriter, factory);
            mailHandler = new SmtpMailHandler(sendlixClient, smtpReader, smtpWriter, factory);
        }

        public Client(TcpClient tcpClient, IHandler handler, ILoggerFactory factory)
            : this(tcpClient, tcpClient.GetStream(), new SendlixApiClient(handler), factory) { }

        public Client(TcpClient tcpClient, SendlixApiClient client, ILoggerFactory factory)
            : this(tcpClient, tcpClient.GetStream(), client, factory) { }

        public Client(TcpClient tcpClient, Stream stream, IHandler handler, ILoggerFactory factory)
            : this(tcpClient, stream, new SendlixApiClient(handler), factory) { }

        public async Task HandleClient(CancellationToken cancellationToken = default)
        {
            using (logger.BeginScope("Client {uuid}", Guid.NewGuid().ToString()))
            {
                try
                {
                    logger.LogInformation("New SMTP connection established");
                    await smtpWriter.WriteAsync(SmtpConstants.Responses.GREETING, cancellationToken);
                    _ = await smtpReader.ReadLineAsync(cancellationToken);

                    // Authentication and email processing
                    if (await ProcessSession(cancellationToken))
                    {
                        await smtpWriter.WriteAsync(SmtpConstants.Responses.BYE, cancellationToken);
                        logger.LogInformation("SMTP connection successfully closed");
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("SMTP session timed out or was cancelled");
                    await smtpWriter.WriteSafeAsync("421 4.4.2 Connection timed out");
                }
                catch (SmtpProtocolException ex)
                {
                    logger.LogWarning(ex, "SMTP protocol error: {Message}", ex.Message);
                    // Error response already sent in most cases
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in SMTP session");
                    await smtpWriter.WriteSafeAsync("421 4.3.0 Internal server error");
                }
                finally
                {
                    CloseConnection();
                }
            }
        }

        private async Task<bool> ProcessSession(CancellationToken cancellationToken)
        {
            // Perform authentication
            if (!sendlixClient.IsAuthenticated && !await authHandler.AuthenticateClient(cancellationToken))
            {
                logger.LogDebug("Authentication failed for client");
                return false;
            }

            // Process email
            if (!await mailHandler.ProcessMail(cancellationToken))
            {
                logger.LogDebug("Mail processing failed for client");
                return false;
            }

            return true;
        }

        private void CloseConnection()
        {
            try
            {
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error closing TCP connection");
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                smtpReader.Dispose();
                // Do not close Stream and TcpClient here if they come from outside
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
