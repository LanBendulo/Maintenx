namespace IT15_Project.Services.Security
{
    /// <summary>
    /// Service interface for Cloudflare Turnstile CAPTCHA validation.
    /// Provides server-side verification of Turnstile tokens to prevent automated abuse.
    /// </summary>
    public interface ITurnstileValidationService
    {
        /// <summary>
        /// Validates a Cloudflare Turnstile token against the Turnstile API.
        /// </summary>
        /// <param name="token">The Turnstile response token from the client</param>
        /// <param name="remoteIp">Optional: The user's IP address for additional validation</param>
        /// <returns>True if validation succeeds, false otherwise</returns>
        Task<bool> ValidateTokenAsync(string token, string? remoteIp = null);

        /// <summary>
        /// Validates a Turnstile token and returns detailed validation result.
        /// </summary>
        /// <param name="token">The Turnstile response token from the client</param>
        /// <param name="remoteIp">Optional: The user's IP address for additional validation</param>
        /// <returns>Detailed validation result with error codes if applicable</returns>
        Task<TurnstileValidationResult> ValidateTokenDetailedAsync(string token, string? remoteIp = null);

        /// <summary>
        /// Checks if Turnstile validation is enabled in configuration.
        /// </summary>
        /// <returns>True if enabled, false if disabled (e.g., in development)</returns>
        bool IsEnabled();
    }

    /// <summary>
    /// Detailed result of Turnstile token validation.
    /// </summary>
    public class TurnstileValidationResult
    {
        /// <summary>
        /// Indicates whether the validation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Timestamp of the challenge (ISO 8601 format).
        /// </summary>
        public string? ChallengeTimestamp { get; set; }

        /// <summary>
        /// Hostname for which the challenge was served.
        /// </summary>
        public string? Hostname { get; set; }

        /// <summary>
        /// Error codes returned by Turnstile API (if any).
        /// Common codes: missing-input-secret, invalid-input-secret, missing-input-response, 
        /// invalid-input-response, bad-request, timeout-or-duplicate
        /// </summary>
        public List<string> ErrorCodes { get; set; } = new List<string>();

        /// <summary>
        /// User-friendly error message for display.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
