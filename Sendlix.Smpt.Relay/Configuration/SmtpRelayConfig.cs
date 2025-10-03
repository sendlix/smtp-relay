namespace Sendlix.Smpt.Relay.Configuration;

public class SmtpRelayConfig
{
    public string ListenAddress { get; set; } = "127.0.0.1";
    public int? Port { get; set; }
    public bool TestMode { get; set; } = false;
    public string? ServerCertificatePath { get; set; }
    public SendlixApiKeyConfig Auth { get; set; } = new SendlixApiKeyConfig();
    public string[] AuthorizedSenders { get; set; } = [];
}

public class SendlixApiKeyConfig
{
    public string? Username { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiKeyPath { get; set; }
    public string Header { get; set; } = "";
}
