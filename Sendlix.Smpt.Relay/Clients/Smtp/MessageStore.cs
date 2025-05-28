using Sendlix.Smpt.Relay.Clients.Api;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    internal class MessageStore : IMessageStore
    {
        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            SendlixApiClient client = (SendlixApiClient)context.Properties["SendlixClient"] ?? throw new InvalidOperationException("Client is not authenticated");
            _ = await client.SendEmail(buffer, cancellationToken);
            return new SmtpResponse(SmtpReplyCode.Ok);
        }
    }
}
