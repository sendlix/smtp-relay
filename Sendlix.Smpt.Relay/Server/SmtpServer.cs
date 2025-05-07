using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Clients.Api;
using Sendlix.Smpt.Relay.Clients.Smtp;
using Sendlix.Smpt.Relay.Configuration;
using Sendlix.Smpt.Relay.Handler;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Sendlix.Smpt.Relay.Server;

public class SmtpServer
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly SmtpRelayConfig _config;
    private readonly TcpListener _listener;
    private readonly IHandler _handler;
    private readonly X509Certificate? _certificate;
    private readonly NetworkCredential? _credential;

    public SmtpServer(ILoggerFactory loggerFactory, SmtpRelayConfig config)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<SmtpServer>();
        _config = config;
        _listener = new TcpListener(IPAddress.Parse(config.ListenAddress), config.Port);
        _handler = InitializeHandler();
        _certificate = LoadCertificate();
        _credential = LoadCredentials();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        _logger.LogInformation("Listening on {Ip}", _listener.LocalEndpoint);

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
            _ = Task.Run(() => HandleClientAsync(tcpClient, default), cancellationToken);

        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, CancellationToken cancellationToken = default)
    {
        try
        {
            IClient client = await CreateClient(tcpClient);

            await client.HandleClient(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client");
        }
    }

    private async Task<IClient> CreateClient(TcpClient tcpClient)
    {
        if (_certificate is null)
        {
            if (_credential is null)
            {
                return new Client(tcpClient, _handler, _loggerFactory);
            }

            var apiClient = new SendlixApiClient(_handler);
            await apiClient.Login(_credential);
            return new Client(tcpClient, apiClient, _loggerFactory);
        }

        return new SSLClient(tcpClient, _certificate, _handler, _loggerFactory);
    }

    private IHandler InitializeHandler()
    {
        if (_config.TestMode)
        {
            _logger.LogInformation("Running in test mode");
            return new TestHandler();
        }

        _logger.LogInformation("Running in production mode");
        return SendlixHandler.Build("https://api.sendlix.com", _loggerFactory, _config.Auth);
    }

    private X509Certificate? LoadCertificate()
    {
        if (string.IsNullOrEmpty(_config.ServerCertificatePath))
            return null;

        if (!File.Exists(_config.ServerCertificatePath))
        {
            _logger.LogError("SSL certificate path {SslPath} does not exist", _config.ServerCertificatePath);
            return null;
        }

        var cert = X509CertificateLoader.LoadPkcs12FromFile(_config.ServerCertificatePath, "");
        _logger.LogInformation("Loaded SSL certificate from {SslPath}", _config.ServerCertificatePath);
        return cert;
    }

    private NetworkCredential? LoadCredentials()
    {
        if (_config.Auth == null)
            return null;

        var apiConfig = _config.Auth;

        if (string.IsNullOrEmpty(apiConfig.Username))
        {
            return null;
        }

        string? apiKey = apiConfig.ApiKey;
        if (string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiConfig.ApiKeyPath))
        {
            if (!File.Exists(apiConfig.ApiKeyPath))
            {
                _logger.LogCritical("ApiKeyPath {ApiKeyPath} does not exist", apiConfig.ApiKeyPath);
                throw new InvalidOperationException("API key file not found");
            }
            apiKey = File.ReadAllText(apiConfig.ApiKeyPath);
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogCritical("ApiKey or ApiKeyPath is not set in the SendlixApiKey section");
            throw new InvalidOperationException("API key is required");
        }

        try
        {
            ValidateApiKey(apiConfig.Username, apiKey).Wait();
            return new NetworkCredential(apiConfig.Username, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error logging in with API key");
            throw;
        }
    }

    private async Task ValidateApiKey(string username, string apiKey)
    {
        var client = new SendlixApiClient(_handler);
        await client.Login(username, apiKey);
        if (!client.IsAuthenticated)
        {
            _logger.LogCritical("API key is not valid");
            throw new InvalidOperationException("Invalid API key");
        }
        _logger.LogInformation("API key is valid and will be used for authentication of all incoming connections");
    }
}
