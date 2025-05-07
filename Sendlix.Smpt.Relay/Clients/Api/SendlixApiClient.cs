using Sendlix.Api.V1.Protos;
using Sendlix.Smpt.Relay.Handler;
using System.Net;
using System.Text.RegularExpressions;

namespace Sendlix.Smpt.Relay.Clients.Api
{
    public partial class SendlixApiClient(IHandler handler)
    {
        private AuthResponse? response;

        public bool IsAuthenticated => response != null;

        public string? Category { get; private set; }

        public Task Login(NetworkCredential authCredential)
        {
            ArgumentNullException.ThrowIfNull(authCredential, nameof(authCredential));
            return Login(authCredential.UserName, authCredential.Password);

        }

        public async Task Login(string username, string password)
        {
            string regex = @"^X-API-KEY(?:;category=(\w+))?$";

            Match match = Regex.Match(username, regex);
            if (match.Success)
            {
                if (match.Groups[1].Success)
                {
                    Category = match.Groups[1].Value;
                }
            }
            else throw new ArgumentException("Username must start with 'X-API-KEY'", nameof(username));

            response = await handler.Login(username, password);
        }

        public bool IsAuthenticatedToSend(string email)
        {
            if (response == null)
            {
                throw new InvalidOperationException("Client is not authenticated");
            }
            string[] split = email.Split("@");
            if (split.Length != 2)
            {
                return false;
            }
            string domain = split[1].Trim();
            return RetrieveAllowedDomains().Contains(domain);
        }

        public string[] RetrieveAllowedDomains()
        {
            if (response == null)
            {
                throw new InvalidOperationException("Client is not authenticated");
            }

            string s = response.Token.Split(".")[1];
            s = s.Replace('-', '+').Replace('_', '/');

            int padding = 4 - s.Length % 4;
            if (padding < 4)
            {
                s += new string('=', padding);
            }

            byte[] payload = Convert.FromBase64String(s);
            string payloadString = System.Text.Encoding.UTF8.GetString(payload);

            string singleDomainPattern = @"""domain""\s*:\s*""([^""]+)""";
            string arrayDomainPattern = @"""domain""\s*:\s*\[((?:""\s*[^""]+\s*""(?:\s*,\s*)?)+)\]";

            Match singleMatch = Regex.Match(payloadString, singleDomainPattern);
            if (singleMatch.Success)
            {
                return [singleMatch.Groups[1].Value];
            }

            // Case 2: Domain array
            Match arrayMatch = Regex.Match(payloadString, arrayDomainPattern);
            if (arrayMatch.Success)
            {
                string domainsValue = arrayMatch.Groups[1].Value;
                return [.. ExtractQuotedStrings().Matches(domainsValue)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)];
            }

            return [];
        }

        public async Task<bool> SendEmail(string eml)
        {
            return response == null
                ? throw new InvalidOperationException("Client is not authenticated")
                : await handler.SendEmail(eml, response.Token, Category);
        }

        [GeneratedRegex(@"""([^""]+)""")]
        private static partial Regex ExtractQuotedStrings();
    }
}
