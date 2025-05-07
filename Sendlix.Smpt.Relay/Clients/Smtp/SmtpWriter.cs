using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    /// <summary>
    /// Class for writing SMTP responses
    /// </summary>
    public class SmtpWriter(Stream stream, TcpClient client, ILoggerFactory logger)
    {
        private readonly ILogger logger = logger.CreateLogger<SmtpWriter>();

        /// <summary>
        /// Writes an SMTP response
        /// </summary>
        public async Task WriteAsync(string message, CancellationToken token)
        {
            if (!stream.CanWrite)
                return;

            if (!client.Connected)
                return;


            logger.LogTrace("Sending: {Message}", message);
            byte[] responseMessage = Encoding.UTF8.GetBytes(message + "\r\n");
            await stream.WriteAsync(responseMessage, token);
        }

        /// <summary>
        /// Writes a response without throwing exceptions
        /// </summary>
        public async Task WriteSafeAsync(string message)
        {
            try
            {
                if (stream.CanWrite)
                {
                    logger.LogTrace("Sending: {Message}", message);
                    byte[] responseMessage = Encoding.UTF8.GetBytes(message + "\r\n");
                    await stream.WriteAsync(responseMessage);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error sending message: {Message}", message);
            }
        }
    }
}
