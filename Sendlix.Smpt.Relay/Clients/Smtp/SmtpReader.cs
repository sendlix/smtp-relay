using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    /// <summary>
    /// Class for reading SMTP commands with timeout support
    /// </summary>
    public class SmtpReader(Stream stream, TcpClient client, ILoggerFactory logger) : IDisposable
    {
        private readonly Stream stream = stream;
        private readonly StreamReader reader = new(stream);
        private readonly ILogger logger = logger.CreateLogger<SmtpReader>();
        private bool disposed;

        /// <summary>
        /// Reads a line with a timeout
        /// </summary>
        public async Task<string> ReadLineAsync(CancellationToken token)
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(SmtpConstants.READ_TIMEOUT_MS);

            if (!client.Connected)
            {
                logger.LogTrace("Client disconnected");
                return "";

            }

            try
            {
                if (!stream.CanRead)
                    throw new SmtpProtocolException("Stream not readable");

                string? line = await reader.ReadLineAsync(cts.Token) ?? throw new SmtpProtocolException("Connection closed unexpectedly");
                logger.LogTrace("Received: {Message}", line);
                return line;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new SmtpProtocolException("Read operation timed out");
            }
        }

        /// <summary>
        /// Reads data until a line with only a dot appears
        /// </summary>
        public async Task<string> ReadDataAsync(CancellationToken token)
        {
            StringBuilder emailData = new();

            while (true)
            {
                string line = await ReadLineAsync(token);
                if (line == ".")
                {
                    break;
                }
                _ = emailData.AppendLine(line);

                if (emailData.Length > SmtpConstants.MAX_EMAIL_SIZE)
                {
                    throw new SmtpProtocolException("Email size exceeds limit");
                }
            }

            return emailData.ToString();
        }

        public void Dispose()
        {

            if (!disposed)
            {
                reader.Dispose();
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
