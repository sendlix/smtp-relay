using Sendlix.Api.V1.Protos;
using Sendlix.Smpt.Relay.Handler;
using System.Buffers;
using System.Net;
using System.Text.RegularExpressions;

namespace Sendlix.Smpt.Relay.Clients.Api
{
    public partial class SendlixApiClient(IHandler handler)
    {
        private AuthResponse? authenticationResponse;

        public bool IsAuthenticated => authenticationResponse != null;

        public EmailVerification EmailVerification { get; private set; } = EmailVerification.Async;

        public string? Category { get; private set; }


        public Task Login(NetworkCredential authCredential, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(authCredential, nameof(authCredential));
            return Login(authCredential.UserName, authCredential.Password, cancellationToken);

        }

        public async Task Login(string username, string password, CancellationToken cancellationToken)
        {

            Match match = UsernameApiKeyRegex().Match(username);
            if (match.Success)
            {
                if (match.Groups[1].Success)
                {
                    Category = match.Groups[1].Value;
                }
                if (match.Groups[2].Success)
                {
                    EmailVerification = match.Groups[2].Value == "sync" ? EmailVerification.Sync : EmailVerification.Async;
                }
            }
            else throw new ArgumentException("Unable to parse username, must be in the format 'X-API-KEY[;category=\"<category>\"][;verification=(sync|async)]'", nameof(username));

            authenticationResponse = await handler.Login(username, password, cancellationToken);
        }

        public bool IsAuthenticatedToSend(string host)
        {
            return authenticationResponse == null
                ? throw new InvalidOperationException("Client is not authenticated")
                : authenticationResponse.Domains.Contains(host);
        }



        public async Task<bool> SendEmail(ReadOnlySequence<byte> eml, CancellationToken cancellationToken)
        {            
            //Check if token is expires within 1 minute
            if (authenticationResponse == null || authenticationResponse.Expires.ToDateTime() < DateTime.UtcNow.AddMinutes(1))            
                throw new InvalidOperationException("Client is not authenticated or token is expired");
            
            return await handler.SendEmail(eml, authenticationResponse.Token, cancellationToken, Category, EmailVerification);
                
        }

        [GeneratedRegex(@"^X-API-KEY(?:;category=""([\w-]+)"")?(?:;verification=(sync|async))?$")]
        private static partial Regex UsernameApiKeyRegex();

    }
}
