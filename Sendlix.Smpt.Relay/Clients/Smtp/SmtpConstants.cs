namespace Sendlix.Smpt.Relay.Clients.Smtp
{
    /// <summary>
    /// SMTP constants for better readability and maintainability
    /// </summary>
    public static class SmtpConstants
    {
        public const int READ_TIMEOUT_MS = 30000; // 30 seconds timeout
        public const int MAX_EMAIL_SIZE = 200 * 1024; // 200 KB maximum, see https://docs.sendlix.com/limits

        public static class Commands
        {
            public const string MAIL_FROM = "MAIL FROM:";
            public const string RCPT_TO = "RCPT TO:";
            public const string DATA = "DATA";
            public const string AUTH_LOGIN = "AUTH LOGIN";
            public const string AUTH_PLAIN = "AUTH PLAIN";
            public const string QUIT = "QUIT";
        }

        public static class Responses
        {
            public const string GREETING = "220 smtp.sendlix.com ESMTP";
            public const string OK = "250 2.1.0 OK";
            public const string AUTH_SUCCESS = "235 2.7.0 Authentication successful";
            public const string AUTH_FAILED = "535 5.7.8 Authentication failed: ";
            public const string INVALID_COMMAND = "501 5.5.4 Invalid {0} command";
            public const string START_DATA = "354 2.0.0 Start mail input; end with <CRLF>.<CRLF>";
            public const string SIZE_EXCEEDED = "552 5.3.4 Message size exceeds fixed limit";
            public const string MAIL_ERROR = "554 5.7.1 Error sending email: ";
            public const string BYE = "221 2.0.0 Bye";
            public const string AUTH_MECHANISM_NOT_SUPPORTED = "504 5.5.4 Authentication mechanism not supported";
            public const string INVALID_AUTH_ENCODING = "501 5.5.2 Invalid AUTH {0} encoding";
            public const string NOT_AUTHENTICATED = "554 5.7.1 Sender not authenticated to send email";
        }
    }
}
