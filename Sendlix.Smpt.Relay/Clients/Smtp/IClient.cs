using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    internal interface IClient : IDisposable
    {
        /// <summary>
        /// Processes an SMTP client.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Task to monitor the asynchronous operation.</returns>
        Task HandleClient(CancellationToken cancellationToken = default);
    }
}
