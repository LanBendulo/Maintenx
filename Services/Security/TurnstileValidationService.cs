using IT15_Project.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IT15_Project.Services.Security
{
    /// <summary>
    /// Implementation of Cloudflare Turnstile CAPTCHA validation service.
    /// Provides server-side verification to prevent automated abuse of authentication flows.
    /// </summary>
    public class TurnstileValidationService : ITurnstileValidationService
    {
        private readonly TurnstileSettings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TurnstileValidationService> _logger;

        public TurnstileValidationService(
            IOptions<TurnstileSettings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<TurnstileValidationService> logger)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public bool IsEnabled()
        {
            return _settings.Enabled;
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateTokenAsync(string token, string? remoteIp = null)
        {
            var result = await ValidateTokenDetailedAsync(token, remoteIp);
            return result.Success;
        }

        /// <inheritdoc/>
        public async Task<TurnstileValidationResult> ValidateTokenDetailedAsync(string token, string? remoteIp = null)
        {
            // If Turnstile is disabled (e.g., development), bypass validation
            if (!_settings.Enabled)
            {
                _logger.LogWarning("Turnstile validation is disabled. Bypassing CAPTCHA check.");
                return new TurnstileValidationResult { Success = true };
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Turnstile validation failed: Missing token");
                return new TurnstileValidationResult
                {
                    Success = false,
                    ErrorCodes = new List<string> { "missing-input-response" },
                    ErrorMessage = "CAPTCHA verification is required."
                };
            }

            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                _logger.LogError("Turnstile validation failed: Secret key not configured");
                return new TurnstileValidationResult
                {
                    Success = false,
                    ErrorCodes = new List<string> { "missing-input-secret" },
                    ErrorMessage = "CAPTCHA service is not properly configured."
                };
            }

            try
            {
                // Prepare validation request
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

                var requestData = new Dictionary<string, string>
                {
                    { "secret", _settings.SecretKey },
                    { "response", token }
                };

                // Include remote IP if provided (optional but recommended)
                if (!string.IsNullOrWhiteSpace(remoteIp))
                {
                    requestData.Add("remoteip", remoteIp);
                }

                var content = new FormUrlEncodedContent(requestData);

                // Call Cloudflare Turnstile verification API
                var response = await httpClient.PostAsync(_settings.VerificationEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Turnstile API returned error status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return new TurnstileValidationResult
                    {
                        Success = false,
                        ErrorCodes = new List<string> { "api-error" },
                        ErrorMessage = "CAPTCHA verification service is temporarily unavailable."
                    };
                }

                // Parse response
                var validationResponse = JsonSerializer.Deserialize<TurnstileApiResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (validationResponse == null)
                {
                    _logger.LogError("Failed to parse Turnstile API response: {Response}", responseContent);
                    return new TurnstileValidationResult
                    {
                        Success = false,
                        ErrorCodes = new List<string> { "parse-error" },
                        ErrorMessage = "CAPTCHA verification failed."
                    };
                }

                // Log validation result
                if (validationResponse.Success)
                {
                    _logger.LogInformation("Turnstile validation succeeded for hostname: {Hostname}",
                        validationResponse.Hostname);
                }
                else
                {
                    _logger.LogWarning("Turnstile validation failed. Error codes: {ErrorCodes}",
                        string.Join(", ", validationResponse.ErrorCodes ?? new List<string>()));
                }

                return new TurnstileValidationResult
                {
                    Success = validationResponse.Success,
                    ChallengeTimestamp = validationResponse.ChallengeTs,
                    Hostname = validationResponse.Hostname,
                    ErrorCodes = validationResponse.ErrorCodes ?? new List<string>(),
                    ErrorMessage = validationResponse.Success ? null : GetUserFriendlyErrorMessage(validationResponse.ErrorCodes)
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Turnstile validation timed out");
                return new TurnstileValidationResult
                {
                    Success = false,
                    ErrorCodes = new List<string> { "timeout" },
                    ErrorMessage = "CAPTCHA verification timed out. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Turnstile validation");
                return new TurnstileValidationResult
                {
                    Success = false,
                    ErrorCodes = new List<string> { "internal-error" },
                    ErrorMessage = "CAPTCHA verification failed. Please try again."
                };
            }
        }

        /// <summary>
        /// Converts Turnstile API error codes to user-friendly messages.
        /// </summary>
        private string GetUserFriendlyErrorMessage(List<string>? errorCodes)
        {
            if (errorCodes == null || !errorCodes.Any())
            {
                return "CAPTCHA verification failed. Please try again.";
            }

            var firstError = errorCodes.First();
            return firstError switch
            {
                "missing-input-secret" => "CAPTCHA service configuration error.",
                "invalid-input-secret" => "CAPTCHA service configuration error.",
                "missing-input-response" => "Please complete the CAPTCHA verification.",
                "invalid-input-response" => "CAPTCHA verification failed. Please try again.",
                "bad-request" => "Invalid CAPTCHA request. Please refresh and try again.",
                "timeout-or-duplicate" => "CAPTCHA verification expired or already used. Please try again.",
                _ => "CAPTCHA verification failed. Please try again."
            };
        }

        /// <summary>
        /// Internal model for Cloudflare Turnstile API response.
        /// </summary>
        private class TurnstileApiResponse
        {
            public bool Success { get; set; }
            public string? ChallengeTs { get; set; }
            public string? Hostname { get; set; }
            public List<string>? ErrorCodes { get; set; }
        }
    }
}
