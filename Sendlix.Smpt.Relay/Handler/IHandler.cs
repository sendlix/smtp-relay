

using Sendlix.Api.V1.Protos;

namespace Sendlix.Smpt.Relay.Handler
{
    public interface IHandler
    {
        public Task<AuthResponse> Login(string username, string password);
        public Task<bool> SendEmail(string eml, string authToken, string? category = null);

    }
}
