using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Clients.Api;
using Sendlix.Smpt.Relay.Handler;
using SmtpServer;
using SmtpServer.Authentication;
using System.Net;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    internal class UserAuthenticator : IUserAuthenticator
    {
        private readonly IHandler _handler;
        private readonly ILogger _logger;
        private UserAuthenticator(IHandler handler, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(handler, nameof(handler));
            _handler = handler;
            _logger = logger;
        }

        public static UserAuthenticator Build(Configuration.SmtpRelayConfig config, ILoggerFactory loggerFactory)
        {
            return config.TestMode
                ? new UserAuthenticator(new TestHandler(), loggerFactory.CreateLogger<UserAuthenticator>())
                : new UserAuthenticator(SendlixHandler.Build("https://api.sendlix.com", loggerFactory, config.Auth), loggerFactory.CreateLogger<UserAuthenticator>());
        }


        public async Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
        {

            try
            {
                SendlixApiClient client = await Login(new NetworkCredential(user, password), cancellationToken);
                context.Properties["SendlixClient"] = client;

                return client.IsAuthenticated;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "User authentication failed for user {User}", user);
                return false;
            }



        }

        public async Task<SendlixApiClient> Login(NetworkCredential credential, CancellationToken cancellationToken)
        {
            SendlixApiClient client = new(_handler);

            await client.Login(credential, cancellationToken);

            return client;

        }
    }
}
