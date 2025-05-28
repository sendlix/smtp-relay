using Sendlix.Smpt.Relay.Clients.Api;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    internal class MailboxFilter : IMailboxFilter
    {
        public Task<bool> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            var propertie = (SendlixApiClient)context.Properties["SendlixClient"];
            return Task.FromResult(propertie.IsAuthenticatedToSend(from.Host));
        }

        public Task<bool> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
