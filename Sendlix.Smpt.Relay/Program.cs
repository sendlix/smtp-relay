using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Clients.Smtp;
using Sendlix.Smpt.Relay.Configuration;
using SmtpServer;
using SmtpServer.ComponentModel;
using System.Net;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConfiguration(config)
        .AddSimpleConsole(options =>
           {
               options.IncludeScopes = true;
               options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
               options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
           }));

ILogger logger = loggerFactory.CreateLogger("Sendlix.Smtp.Relay");

SmtpRelayConfig smtpConfig = config.Get<SmtpRelayConfig>() ?? new SmtpRelayConfig();

SmtpServerOptionsBuilder options = new SmtpServerOptionsBuilder()
    .ServerName(smtpConfig.ListenAddress)
    .MaxMessageSize(10 * 1024 * 1024);


if (smtpConfig.AuthorizedSenders.Length != 0)
    logger.LogInformation("Email can only send from the following addresses: {AuthorizedSenders}", string.Join(", ", smtpConfig.AuthorizedSenders));

NetworkCredential? credentials = LoadCredentials();
SmtpCertificateProvider? cert = SmtpCertificateProvider.Build(smtpConfig, loggerFactory);

int[] ports = smtpConfig.Port.HasValue ? [smtpConfig.Port.Value] : [587, 465];

foreach (int port in ports)
{
    EndpointDefinitionBuilder endpoint = new EndpointDefinitionBuilder()
        .Port(port);

    _ = endpoint.AllowUnsecureAuthentication(true);
    if (cert != null)
    {
        _ = endpoint.Certificate(cert);
        _ = endpoint.AllowUnsecureAuthentication(false);
        _ = endpoint.IsSecure(port == 465);

    }

    _ = endpoint.AuthenticationRequired(credentials == null);
    _ = endpoint.SessionTimeout(TimeSpan.FromSeconds(60));
    _ = options.Endpoint(endpoint.Build());

    logger.LogInformation("Listening on port {Port} with secure {Secure}", port, cert != null);
}

NetworkCredential? LoadCredentials()
{
    if (smtpConfig.Auth == null)
        return null;

    SendlixApiKeyConfig apiConfig = smtpConfig.Auth;

    if (string.IsNullOrEmpty(apiConfig.Username))
    {
        return null;
    }

    string? apiKey = apiConfig.ApiKey;
    if (string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiConfig.ApiKeyPath))
    {
        if (!File.Exists(apiConfig.ApiKeyPath))
        {
            logger.LogCritical("ApiKeyPath {ApiKeyPath} does not exist", apiConfig.ApiKeyPath);
            throw new InvalidOperationException("API key file not found");
        }
        apiKey = File.ReadAllText(apiConfig.ApiKeyPath);
    }

    if (string.IsNullOrEmpty(apiKey))
    {
        logger.LogCritical("ApiKey or ApiKeyPath is not set in the SendlixApiKey section");
        throw new InvalidOperationException("API key is required");
    }

    try
    {

        return new NetworkCredential(apiConfig.Username, apiKey);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Error logging in with API key");
        throw;
    }
}

ServiceProvider serviceProvider = new();

UserAuthenticator user = UserAuthenticator.Build(smtpConfig, loggerFactory);

MailboxFilter mailboxFilter = new(smtpConfig);
MessageStore store = new();

serviceProvider.Add(user);
serviceProvider.Add(mailboxFilter);
serviceProvider.Add(store);

SmtpServer.SmtpServer smtpServer = new(options.Build(), serviceProvider);

smtpServer.SessionCreated += async (sender, e) =>
{
    e.Context.Properties["scope"] = logger.BeginScope("Client {ClientId}", e.Context.SessionId);
    logger.LogInformation("Session created");
    if (credentials is not null)
        e.Context.Properties["SendlixClient"] = await user.Login(credentials, CancellationToken.None);
};

smtpServer.SessionCompleted += (sender, e) =>
{
    if (e.Context.Properties.TryGetValue("scope", out object? scope) && scope is IDisposable disposableScope)
    {
        logger.LogInformation("Session completed");
        disposableScope.Dispose();
    }
};

AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
{
    logger.LogInformation("Shutting down SMTP server gracefully...");
    smtpServer.Shutdown();
    logger.LogInformation("SMTP server stopped");
};


smtpServer.StartAsync(CancellationToken.None).GetAwaiter().GetResult();