using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Sendlix.Api.V1.Protos;
using Sendlix.Smpt.Relay.Clients.Api;
using Sendlix.Smpt.Relay.Configuration;
using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Sendlix.Smpt.Relay.Handler
{
    internal class SendlixHandler : IHandler
    {
        private readonly Auth.AuthClient _authClient;
        private readonly Email.EmailClient _EmailClient;
        private static readonly ConcurrentDictionary<string, AuthResponse> authCache = new();
        private readonly Metadata _metadata;
        private SHA256 sHA = SHA256.Create();

        private SendlixHandler(Auth.AuthClient authClient, Email.EmailClient EmailClient, Metadata metadata)
        {
            _authClient = authClient;
            _EmailClient = EmailClient;
            _metadata = metadata;
        }

        public static SendlixHandler Build(string url, ILoggerFactory factory, SendlixApiKeyConfig configuration)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions()
            {
                LoggerFactory = factory,
                HttpHandler = new GrpcHttpRelayClient("Sendlix.Smtp.Relay")
            });

            Auth.AuthClient authClient = new(channel);
            Email.EmailClient EmailClient = new(channel);

            string[] s = configuration.Header.Split(":");
            Metadata metadata = s.Length == 2 ? new() { { s[0], s[1] } } : [];
            return new SendlixHandler(authClient, EmailClient, metadata);
        }

        public async Task<AuthResponse> Login(string username, string password, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(username, nameof(username));
            ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

            string[] split = password.Split(".");

            if (split.Length != 2)
            {
                throw new ArgumentException("Password must be in the format 'secret.keyId'", nameof(password));
            }

            string secret = split[0];
            string keyId = split[1].Trim();

            string hashedSecret = Convert.ToBase64String(sHA.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secret + keyId)));

            if (!long.TryParse(keyId, out long keyIdLong))
            {
                throw new ArgumentException("KeyId must be a valid long", nameof(password));
            }

            if (!authCache.ContainsKey(hashedSecret))
            {
                _ = authCache.TryAdd(hashedSecret, await RetrieveJwtToken(keyIdLong, secret, cancellationToken));
            }

            AuthResponse authResponse = authCache[hashedSecret];

            if (authResponse.Expires.ToDateTime() < DateTime.UtcNow.AddMinutes(1))
            {
                _ = authCache.Remove(hashedSecret, out _);
                authResponse = await RetrieveJwtToken(keyIdLong, secret, cancellationToken);
                _ = authCache.TryAdd(hashedSecret, authResponse);
            }

            return authResponse;
        }


        private async Task<AuthResponse> RetrieveJwtToken(long keyId, string secret, CancellationToken token)
        {
            ApiKey apiKey = new()
            {
                KeyID = keyId,
                Secret = secret
            };
            AuthRequest request = new()
            {
                ApiKey = apiKey,
            };



            AuthResponse res = await _authClient.GetJwtTokenAsync(request, _metadata, null, token);
            return res;
        }

        public async Task<bool> SendEmail(ReadOnlySequence<byte> eml, string authToken, CancellationToken cancellationToken, string? category)
        {
            EmlMailRequest mail = new()
            {
                Mail = ByteString.CopyFrom(eml.ToArray()),
            };

            if (category != null)
            {
                mail.AdditionalInfos = new AdditionalInfos() { Category = category };
            }

            Metadata entries = new()
            {
                { "Authorization", $"Bearer {authToken}" }
            };

            _ = await _EmailClient.SendEmlEmailAsync(mail, entries, null, cancellationToken);
            return true;

        }
    }
}
