using Grpc.Core;
using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Clients.Api;

namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    /// <summary>
    /// Handler for SMTP authentication
    /// </summary>
    public class SmtpAuthHandler(
            SendlixApiClient sendlixClient, SmtpReader smtpReader, SmtpWriter smtpWriter, ILoggerFactory logger)
    {
        private readonly ILogger logger = logger.CreateLogger<SmtpAuthHandler>();

        /// <summary>
        /// Execute the SMTP authentication
        /// </summary>
        public async Task<bool> AuthenticateClient(CancellationToken token)
        {
            logger.LogDebug("Starting client authentication");
            try
            {
                if (await HandleAuth(token))
                {
                    logger.LogInformation("Client successfully authenticated");
                    await smtpWriter.WriteAsync(SmtpConstants.Responses.AUTH_SUCCESS, token);
                    return true;
                }
                else
                {
                    logger.LogDebug("Authentication failed: Invalid credentials");
                    await smtpWriter.WriteAsync(SmtpConstants.Responses.AUTH_FAILED + "Invalid credentials", token);
                }
            }
            catch (RpcException ex)
            {
                logger.LogDebug(ex, "RPC error during authentication: {Error}", ex.Status.Detail);
                await smtpWriter.WriteAsync(SmtpConstants.Responses.AUTH_FAILED + ex.Status.Detail, token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unexpected error during authentication: {Error}", ex.Message);
                await smtpWriter.WriteAsync(SmtpConstants.Responses.AUTH_FAILED + "Invalid credentials", token);
            }

            return false;
        }

        private async Task<bool> HandleAuth(CancellationToken token)
        {
            logger.LogDebug("Sending authentication options to client");
            await smtpWriter.WriteAsync("250-smtp.sendlix.com", token);
            await smtpWriter.WriteAsync("250 AUTH LOGIN PLAIN", token);

            string authCommand = await smtpReader.ReadLineAsync(token);
            logger.LogDebug("Authentication method received: {AuthMethod}", authCommand.Split(' ')[0]);

            if (authCommand.StartsWith(SmtpConstants.Commands.AUTH_LOGIN))
            {
                return await HandleLoginAuth(token);
            }
            else if (authCommand.StartsWith(SmtpConstants.Commands.AUTH_PLAIN))
            {
                return await HandlePlainAuth(authCommand, token);
            }
            else
            {
                logger.LogDebug("Unsupported authentication method: {AuthCommand}", authCommand);
                await smtpWriter.WriteAsync(SmtpConstants.Responses.AUTH_MECHANISM_NOT_SUPPORTED, token);
            }
            return false;
        }

        private async Task<bool> HandleLoginAuth(CancellationToken token)
        {
            logger.LogDebug("Processing AUTH LOGIN");
            try
            {
                // Base64 for "Username:"
                await smtpWriter.WriteAsync("334 VXNlcm5hbWU6", token);
                string usernameBase64 = await smtpReader.ReadLineAsync(token);
                string username = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(usernameBase64));
                logger.LogDebug("Username for AUTH LOGIN received: {Username}", username);

                // Base64 for "Password:"
                await smtpWriter.WriteAsync("334 UGFzc3dvcmQ6", token);
                string passwordBase64 = await smtpReader.ReadLineAsync(token);
                string password = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(passwordBase64));
                logger.LogDebug("Password for AUTH LOGIN received");

                await sendlixClient.Login(username, password);
                return true;
            }
            catch (FormatException ex)
            {
                logger.LogDebug(ex, "Base64 decoding error in AUTH LOGIN");
                await smtpWriter.WriteAsync(string.Format(SmtpConstants.Responses.INVALID_AUTH_ENCODING, "LOGIN"), token);
                return false;
            }
        }

        private async Task<bool> HandlePlainAuth(string authCommand, CancellationToken token)
        {
            logger.LogDebug("Processing AUTH PLAIN");

            string[] parts = authCommand.Split(' ');
            if (parts.Length > 2)
            {
                // Authentication data already included in command
                string authData = parts[2];
                logger.LogDebug("AUTH PLAIN data already included in command");
                return await ProcessPlainAuth(authData, token);
            }
            else
            {
                // Requesting authentication data
                logger.LogDebug("Requesting AUTH PLAIN data");
                await smtpWriter.WriteAsync("334 ", token);
                string authData = await smtpReader.ReadLineAsync(token);
                return await ProcessPlainAuth(authData, token);
            }
        }

        private async Task<bool> ProcessPlainAuth(string authData, CancellationToken token)
        {
            logger.LogDebug("Processing AUTH PLAIN data");
            try
            {
                byte[] decodedData = Convert.FromBase64String(authData);
                string decodedStr = System.Text.Encoding.UTF8.GetString(decodedData);

                // AUTH PLAIN format: [authzid]\0username\0password
                string[] credentials = decodedStr.Split('\0');

                // credentials[0] is typically empty
                // credentials[1] is the username
                // credentials[2] is the password
                if (credentials.Length >= 3)
                {
                    string username = credentials[1];
                    logger.LogDebug("Authenticating user: {Username}", username);
                    string password = credentials[2];

                    await sendlixClient.Login(username, password);
                    return true;
                }
                else
                {
                    logger.LogDebug("Invalid AUTH PLAIN encoding: Missing credentials");
                    await smtpWriter.WriteAsync(string.Format(SmtpConstants.Responses.INVALID_AUTH_ENCODING, "PLAIN"), token);
                }
            }
            catch (FormatException ex)
            {
                logger.LogDebug(ex, "Base64 decoding error in AUTH PLAIN: {Error}", ex.Message);
                await smtpWriter.WriteAsync(string.Format(SmtpConstants.Responses.INVALID_AUTH_ENCODING, "PLAIN"), token);
            }

            return false;
        }
    }
}
