namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    /// <summary>
    /// Exception for SMTP protocol errors
    /// </summary>
    public class SmtpProtocolException : Exception
    {
        public SmtpProtocolException(string message) : base(message) { }
        public SmtpProtocolException(string message, Exception innerException) : base(message, innerException) { }
    }
}
