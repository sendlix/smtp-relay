using Grpc.Core;
using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Clients.Api;
using System.Text.RegularExpressions;

namespace Sendlix.Smpt.Relay.Clients.Smtp;

/// <summary>
/// Handler for processing emails
/// </summary>
public partial class SmtpMailHandler(SendlixApiClient sendlixClient, SmtpReader smtpReader, SmtpWriter smtpWriter, ILoggerFactory logger)
{
    private readonly ILogger logger = logger.CreateLogger<SmtpMailHandler>();

    /// <summary>
    /// Processes the complete email flow
    /// </summary>
    public async Task<bool> ProcessMail(CancellationToken token)
    {
        if (!await ProcessMailFromCommand(token))
        {
            logger.LogDebug("MAIL FROM processing failed");
            return false;
        }

        logger.LogDebug("MAIL FROM successfully processed");

        // Read the RCPT TO command
        _ = await smtpReader.ReadLineAsync(token);
        await smtpWriter.WriteAsync(SmtpConstants.Responses.OK, token);

        // Process DATA command
        string dataCommand = await smtpReader.ReadLineAsync(token);
        if (!dataCommand.StartsWith(SmtpConstants.Commands.DATA))
        {
            logger.LogDebug("Invalid DATA command received: {Command}", dataCommand);
            await smtpWriter.WriteAsync(string.Format(SmtpConstants.Responses.INVALID_COMMAND, "DATA"), token);
            return false;
        }

        return await ProcessEmailContent(token);
    }

    private async Task<bool> ProcessEmailContent(CancellationToken token)
    {
        await smtpWriter.WriteAsync(SmtpConstants.Responses.START_DATA, token);
        logger.LogDebug("DATA command received, collecting email content");

        try
        {
            string emailData = await smtpReader.ReadDataAsync(token);
            if (!await SendEmailToService(emailData, token))
            {
                return false;
            }

            await smtpWriter.WriteAsync(SmtpConstants.Responses.OK, token);  
            return true;
        }
        catch (SmtpProtocolException ex) when (ex.Message.Contains("size exceeds limit"))
        {
            logger.LogWarning("Email size limit exceeded");
            await smtpWriter.WriteAsync(SmtpConstants.Responses.SIZE_EXCEEDED, token);
            return false;
        }
    }

    private async Task<bool> SendEmailToService(string emailData, CancellationToken token)
    {
        try
        {
            logger.LogDebug("Sending email to Sendlix service");
            await sendlixClient.SendEmail(emailData);
            logger.LogInformation("Email successfully sent");
            return true;
        }
        catch (RpcException ex)
        {
            logger.LogDebug(ex, "RPC error sending email: {Error}", ex.Status.Detail);
            await smtpWriter.WriteAsync(SmtpConstants.Responses.MAIL_ERROR + ex.Status.Detail, token);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "General error sending email: {Error}", ex.Message);
            await smtpWriter.WriteAsync(SmtpConstants.Responses.MAIL_ERROR + ex.Message, token);
            return false;
        }
    }

    private async Task<bool> ProcessMailFromCommand(CancellationToken token)
    {
        string from = await smtpReader.ReadLineAsync(token);
        if (!from.StartsWith(SmtpConstants.Commands.MAIL_FROM))
        {
            logger.LogDebug("Invalid MAIL FROM command received: {Command}", from);
            await smtpWriter.WriteAsync(string.Format(SmtpConstants.Responses.INVALID_COMMAND, "MAIL FROM"), token);
            return false;
        }

        Match match = ExtractEmailRegex().Match(from);

        if (!match.Success)
        {
            logger.LogWarning("Invalid email address in MAIL FROM command: {Command}", from);
            await smtpWriter.WriteAsync(string.Format(SmtpConstants.Responses.INVALID_COMMAND, "email address"), token);
            return false;
        }

        string senderEmail = match.Groups[1].Value;
        logger.LogDebug("Checking authorization for sender: {SenderEmail}", senderEmail);

        if (!sendlixClient.IsAuthenticatedToSend(senderEmail))
        {
            logger.LogDebug("Sender not authorized: {SenderEmail}", senderEmail);
            await smtpWriter.WriteAsync(SmtpConstants.Responses.NOT_AUTHENTICATED, token);
            return false;
        }

        logger.LogDebug("Sender authorized: {SenderEmail}", senderEmail);
        await smtpWriter.WriteAsync(SmtpConstants.Responses.OK, token);
        return true;
    }

    [GeneratedRegex(@"<([^>]+)>")]
    private static partial Regex ExtractEmailRegex();
}
