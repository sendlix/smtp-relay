using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sendlix.Smpt.Relay.Configuration;
using Sendlix.Smpt.Relay.Server;


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


SmtpRelayConfig smtpConfig = config.Get<SmtpRelayConfig>() ?? new SmtpRelayConfig();
SmtpServer server = new(loggerFactory, smtpConfig);

try
{
    await server.StartAsync();
}
catch (Exception ex)
{
    ILogger logger = loggerFactory.CreateLogger("Program");
    logger.LogCritical(ex, "Server failed to start");
    return 1;
}

return 0;