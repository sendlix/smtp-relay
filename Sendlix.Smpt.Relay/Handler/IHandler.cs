

using Sendlix.Api.V1.Protos;
using System.Buffers;
using System.Threading;

namespace Sendlix.Smpt.Relay.Handler
{
    public interface IHandler
    {
        public Task<AuthResponse> Login(string username, string password, CancellationToken cancellationToken);
        public Task<bool> SendEmail(ReadOnlySequence<byte> eml, string authToken, CancellationToken cancellationToken, string? category = null);

    }
}
