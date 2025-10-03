using Microsoft.Extensions.Logging;
using SmtpServer;
using System.Security.Cryptography.X509Certificates;

namespace Sendlix.Smpt.Relay.Configuration
{
    internal class SmtpCertificateProvider : ICertificateFactory
    {

        private X509Certificate2? certificate = null;

        private readonly string certPath = string.Empty;

        private readonly ILogger<SmtpCertificateProvider> logger;

        private SmtpCertificateProvider(string certPath, ILogger<SmtpCertificateProvider> logger)
        {
            this.certPath = certPath;
            this.logger = logger;
        }

        public static SmtpCertificateProvider? Build(SmtpRelayConfig smtpConfig, ILoggerFactory factory)
        {

            ILogger<SmtpCertificateProvider> logger = factory.CreateLogger<SmtpCertificateProvider>();

            if (string.IsNullOrEmpty(smtpConfig.ServerCertificatePath))
                return null;

            if (!File.Exists(smtpConfig.ServerCertificatePath))
            {
                logger.LogError("SSL certificate path {SslPath} does not exist", smtpConfig.ServerCertificatePath);
                return null;
            }

            return new SmtpCertificateProvider(smtpConfig.ServerCertificatePath, logger);
        }

        public X509Certificate GetServerCertificate(ISessionContext sessionContext)
        {

            if (certificate == null || DateTime.UtcNow >= certificate.NotAfter)
            {
                certificate = X509CertificateLoader.LoadPkcs12FromFile(certPath, "");

                if (DateTime.UtcNow >= certificate.NotAfter)
                {
                    logger.LogCritical("SSL certificate is expired");
                    throw new InvalidOperationException("SSL certificate is expired");
                }               
                logger.LogInformation("Loaded SSL certificate");
            }
            return certificate;
        }
    }
}


