
using Sendlix.Api.V1.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sendlix.Smpt.Relay.Handler
{
    public class TestHandler : IHandler
    {
        // { "domain": "example.com" }
        public const string JWT = ".eyAiZG9tYWluIjogImV4YW1wbGUuY29tIiB9.";
        public Task<AuthResponse> Login(string username, string password)
        {
           return Task.FromResult(  new AuthResponse()
            {
                Token = JWT,
                Expires = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow.AddHours(1)),
            });
        }

        public Task<bool> SendEmail(string eml, string authToken, string? category = null)
        {
            Console.WriteLine("-------------------- Email --------------------");
            Console.WriteLine(eml);
            Console.WriteLine("------------------ End Email ------------------");
            return Task.FromResult(true);
        }
    }
}
