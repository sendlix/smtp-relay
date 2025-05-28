using Sendlix.Api.V1.Protos;
using Sendlix.Smpt.Relay.Handler;
using System.Buffers;
using System.Net;
using System.Text.RegularExpressions;

namespace Sendlix.Smpt.Relay.Clients.Api
{
    public partial class SendlixApiClient(IHandler handler)
    {
        private AuthResponse? response;

        public bool IsAuthenticated => response != null;

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
            }
            else throw new ArgumentException("Username must start with 'X-API-KEY'", nameof(username));

            response = await handler.Login(username, password, cancellationToken);
        }

        public bool IsAuthenticatedToSend(string host)
        {
            return response == null
                ? throw new InvalidOperationException("Client is not authenticated")
                : RetrieveAllowedDomains().Contains(host);
        }

        public string[] RetrieveAllowedDomains()
        {
            if (response == null)
            {
                throw new InvalidOperationException("Client is not authenticated");
            }

            string s = response.Token.Split(".")[1];
            s = s.Replace('-', '+').Replace('_', '/');

            int padding = 4 - (s.Length % 4);
            if (padding < 4)
            {
                s += new string('=', padding);
            }

            byte[] payload = Convert.FromBase64String(s);
            string payloadString = System.Text.Encoding.UTF8.GetString(payload);



            Match singleMatch = DomainRegex().Match(payloadString);
            if (singleMatch.Success)
            {
                return [singleMatch.Groups[1].Value];
            }

            // Case 2: Domain array
            Match arrayMatch = DomainArrayRegex().Match(payloadString);
            if (arrayMatch.Success)
            {
                string domainsValue = arrayMatch.Groups[1].Value;
                return [.. ExtractQuotedStrings().Matches(domainsValue)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)];
            }

            return [];
        }



        public async Task<bool> SendEmail(ReadOnlySequence<byte> eml, CancellationToken cancellationToken)
        {

            //Check if token is expires within 1 minute
            return response == null || response.Expires.ToDateTime() < DateTime.UtcNow.AddMinutes(1)
                ? throw new InvalidOperationException("Client is not authenticated or token is expired")
                : response == null
                ? throw new InvalidOperationException("Client is not authenticated")
                : await handler.SendEmail(eml, response.Token, cancellationToken, Category);
        }

        [GeneratedRegex(@"""([^""]+)""")]
        private static partial Regex ExtractQuotedStrings();
        [GeneratedRegex(@"^X-API-KEY(?:;category=(\w+))?$")]
        private static partial Regex UsernameApiKeyRegex();
        [GeneratedRegex(@"""domain""\s*:\s*""([^""]+)""")]
        private static partial Regex DomainRegex();
        [GeneratedRegex(@"""domain""\s*:\s*\[((?:""\s*[^""]+\s*""(?:\s*,\s*)?)+)\]")]
        private static partial Regex DomainArrayRegex();
    }
}
