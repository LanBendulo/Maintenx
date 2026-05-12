namespace IT15_Project.Configuration
{
    /// <summary>
    /// SMTP Email Configuration Settings
    /// Used for: Forgot Password, Email Verification, Notifications
    /// SECURITY: Production credentials must come from environment variables or user secrets
    /// </summary>
    public class EmailSettings
    {
        /// <summary>SMTP server hostname (e.g., smtp.gmail.com, smtp.office365.com)</summary>
        public string SmtpHost { get; set; } = "";

        /// <summary>SMTP server port (587 for TLS, 465 for SSL, 25 for unencrypted)</summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>Display name for outgoing emails (e.g., "MaintenX Support")</summary>
        public string SenderName { get; set; } = "MaintenX";

        /// <summary>Email address for outgoing emails (e.g., noreply@maintenx.com)</summary>
        public string SenderEmail { get; set; } = "";

        /// <summary>SMTP authentication username (often same as SenderEmail)</summary>
        public string Username { get; set; } = "";

        /// <summary>SMTP authentication password (NEVER commit to source control)</summary>
        public string Password { get; set; } = "";

        /// <summary>Use SSL/TLS encryption (recommended: true)</summary>
        public bool UseSSL { get; set; } = true;
    }
}
