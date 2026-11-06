using Sendlix.Smpt.Relay.Clients.Api;
using Sendlix.Smpt.Relay.Configuration;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    internal class MailboxFilter(SmtpRelayConfig config) : IMailboxFilter
    {
        public Task<bool> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            SendlixApiClient propertie = (SendlixApiClient)context.Properties["SendlixClient"];

            if (config.AuthorizedSenders.Length != 0 && !config.AuthorizedSenders.Contains(from.Host, StringComparer.OrdinalIgnoreCase))
                return Task.FromResult(false);
           
            if(!propertie.IsAuthenticatedToSend(from.Host))
                throw new SmtpResponseException(new SmtpResponse(SmtpReplyCode.MailboxUnavailable, $"Sender domain {from.Host} is not allowed for this api key."));

            return Task.FromResult(true);
        }

        public Task<bool> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
