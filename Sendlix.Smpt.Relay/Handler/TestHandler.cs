
using Sendlix.Api.V1.Protos;
using System.Buffers;
using System.Text;

namespace Sendlix.Smpt.Relay.Handler
{
    public class TestHandler : IHandler
    {
        // { "domain": "example.com" }
        public const string JWT = ".eyAiZG9tYWluIjogImV4YW1wbGUuY29tIiB9.";
        public Task<AuthResponse> Login(string username, string password, CancellationToken _)
        {
            return Task.FromResult(new AuthResponse()
            {
                Token = JWT,
                Expires = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddHours(1)),
            });
        }

        public Task<bool> SendEmail(ReadOnlySequence<byte> eml, string authToken, CancellationToken cancellationToken, string? category = null)
        {
            Console.WriteLine("-------------------- Email --------------------");
            Console.WriteLine(Encoding.UTF8.GetString(eml.ToArray()));
            Console.WriteLine("------------------ End Email ------------------");
            return Task.FromResult(true);
        }

    }
}
