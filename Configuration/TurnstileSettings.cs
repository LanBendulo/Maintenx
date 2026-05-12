namespace IT15_Project.Configuration
{
    /// <summary>
    /// Configuration settings for Cloudflare Turnstile CAPTCHA integration.
    /// Provides enterprise-grade bot protection for authentication flows.
    /// </summary>
    public class TurnstileSettings
    {
        /// <summary>
        /// Cloudflare Turnstile site key (public key).
        /// Used in client-side widget rendering.
        /// </summary>
        public string SiteKey { get; set; } = string.Empty;

        /// <summary>
        /// Cloudflare Turnstile secret key (private key).
        /// Used for server-side token validation.
        /// NEVER expose this to client-side code.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Enable or disable Turnstile validation globally.
        /// Useful for development/testing environments.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Cloudflare Turnstile verification endpoint.
        /// Default: https://challenges.cloudflare.com/turnstile/v0/siteverify
        /// </summary>
        public string VerificationEndpoint { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        /// <summary>
        /// Timeout for Turnstile API calls (in seconds).
        /// Default: 10 seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;
    }
}
