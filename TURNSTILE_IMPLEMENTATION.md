# Cloudflare Turnstile CAPTCHA Implementation Guide

## Overview

MaintenX now includes enterprise-grade bot protection using **Cloudflare Turnstile** CAPTCHA. This implementation provides:

- ✅ Server-side validation for all authentication flows
- ✅ Rate limiting on login, registration, and password reset endpoints
- ✅ Clean integration with ASP.NET Core Identity
- ✅ Privacy-friendly alternative to Google reCAPTCHA
- ✅ Production-ready anti-abuse architecture

---

## Architecture

### Components

1. **TurnstileSettings** (`Configuration/TurnstileSettings.cs`)
   - Configuration model for Turnstile keys and settings
   - Supports environment-based configuration

2. **ITurnstileValidationService** (`Services/Security/ITurnstileValidationService.cs`)
   - Service interface for CAPTCHA validation
   - Provides both simple and detailed validation methods

3. **TurnstileValidationService** (`Services/Security/TurnstileValidationService.cs`)
   - Implementation of Turnstile validation logic
   - Handles API calls to Cloudflare verification endpoint
   - Includes error handling, logging, and user-friendly messages

4. **Rate Limiting Middleware**
   - Configured in `Program.cs`
   - Protects login, registration, and forgot password endpoints
   - Configurable limits via `appsettings.json`

---

## Configuration

### 1. Get Cloudflare Turnstile Keys

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. Navigate to **Turnstile** section
3. Create a new site
4. Copy your **Site Key** (public) and **Secret Key** (private)

### 2. Configure Development Environment

Update `appsettings.json`:

```json
{
  "Turnstile": {
    "SiteKey": "YOUR_DEVELOPMENT_SITE_KEY",
    "SecretKey": "YOUR_DEVELOPMENT_SECRET_KEY",
    "Enabled": false,
    "VerificationEndpoint": "https://challenges.cloudflare.com/turnstile/v0/siteverify",
    "TimeoutSeconds": 10
  }
}
```

**Note:** Set `Enabled: false` in development to bypass CAPTCHA during testing.

### 3. Configure Production Environment

Update `appsettings.Production.json`:

```json
{
  "Turnstile": {
    "SiteKey": "YOUR_PRODUCTION_SITE_KEY",
    "SecretKey": "YOUR_PRODUCTION_SECRET_KEY",
    "Enabled": true,
    "VerificationEndpoint": "https://challenges.cloudflare.com/turnstile/v0/siteverify",
    "TimeoutSeconds": 10
  }
}
```

**Security:** Never commit production keys to source control. Use environment variables or Azure Key Vault.

### 4. Environment Variables (Recommended for Production)

```bash
export Turnstile__SiteKey="your_production_site_key"
export Turnstile__SecretKey="your_production_secret_key"
export Turnstile__Enabled="true"
```

---

## Rate Limiting Configuration

Configure rate limits in `appsettings.json`:

```json
{
  "RateLimiting": {
    "Login": {
      "PermitLimit": 5,
      "WindowSeconds": 60
    },
    "ForgotPassword": {
      "PermitLimit": 3,
      "WindowSeconds": 300
    },
    "Registration": {
      "PermitLimit": 3,
      "WindowSeconds": 3600
    }
  }
}
```

**Defaults:**
- **Login:** 5 attempts per 60 seconds (1 minute)
- **Forgot Password:** 3 attempts per 300 seconds (5 minutes)
- **Registration:** 3 attempts per 3600 seconds (1 hour)

---

## Protected Endpoints

### ✅ Currently Protected

1. **Login** (`/Identity/Account/Login`)
   - Turnstile validation: ✅ **IMPLEMENTED**
   - Rate limiting: ✅ (5 per minute)
   - Preserves Google Sign-In flow

2. **Registration** (`/Identity/Account/Register`)
   - Turnstile validation: ✅ **IMPLEMENTED**
   - Rate limiting: ✅ (3 per hour)
   - Multi-tenant company creation protected

3. **Forgot Password** (`/Identity/Account/ForgotPassword`)
   - Turnstile validation: ✅ **IMPLEMENTED**
   - Rate limiting: ✅ (3 per 5 minutes)
   - Email spam prevention

4. **Admin User Creation** (Ready for future implementation)
   - Turnstile validation: 🔄 (Add to UserManagementController)
   - Rate limiting: Optional

---

## Implementation Pattern

### Backend (Razor Page Model)

```csharp
using IT15_Project.Services.Security;
using Microsoft.AspNetCore.RateLimiting;

public class YourPageModel : PageModel
{
    private readonly ITurnstileValidationService _turnstileService;

    public YourPageModel(ITurnstileValidationService turnstileService)
    {
        _turnstileService = turnstileService;
    }

    public class InputModel
    {
        // ... other properties ...
        
        public string TurnstileToken { get; set; }
    }

    [EnableRateLimiting("yourEndpoint")]
    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            // Validate Turnstile token
            if (_turnstileService.IsEnabled())
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _turnstileService.ValidateTokenDetailedAsync(
                    Input.TurnstileToken, remoteIp);

                if (!result.Success)
                {
                    ModelState.AddModelError(string.Empty, 
                        result.ErrorMessage ?? "CAPTCHA verification failed.");
                    return Page();
                }
            }

            // Continue with your logic...
        }

        return Page();
    }
}
```

### Frontend (Razor View)

```html
<!-- Add Turnstile widget -->
<div class="mx-field">
    <div id="turnstile-widget" class="cf-turnstile" 
         data-sitekey="@Configuration["Turnstile:SiteKey"]"
         data-callback="onTurnstileSuccess"
         data-error-callback="onTurnstileError"
         data-theme="light"
         data-size="normal">
    </div>
    <input type="hidden" asp-for="Input.TurnstileToken" id="turnstile-token" />
    <span asp-validation-for="Input.TurnstileToken" class="field-error"></span>
</div>

@section Scripts {
    <!-- Load Turnstile script -->
    <script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
    
    <script>
        function onTurnstileSuccess(token) {
            document.getElementById('turnstile-token').value = token;
        }

        function onTurnstileError(error) {
            console.error('Turnstile error:', error);
            document.getElementById('turnstile-token').value = '';
        }

        // Validate before form submission
        document.getElementById('your-form').addEventListener('submit', function (e) {
            const token = document.getElementById('turnstile-token').value;
            if (!token || token.trim() === '') {
                e.preventDefault();
                alert('Please complete the CAPTCHA verification.');
                return false;
            }
        });
    </script>
}
```

---

## Testing

### Development Testing (CAPTCHA Disabled)

1. Set `Turnstile:Enabled` to `false` in `appsettings.json`
2. CAPTCHA validation will be bypassed
3. Rate limiting still applies

### Production Testing (CAPTCHA Enabled)

1. Use Cloudflare's test keys for staging:
   - **Site Key:** `1x00000000000000000000AA`
   - **Secret Key:** `1x0000000000000000000000000000000AA`
2. These keys always pass validation
3. Replace with real keys before production deployment

### Rate Limiting Testing

Test rate limits by making rapid requests:

```bash
# Test login rate limit (5 per minute)
for i in {1..10}; do
  curl -X POST https://your-domain/Identity/Account/Login \
    -d "Input.Email=test@example.com&Input.Password=test"
done
```

Expected: First 5 succeed, remaining return `429 Too Many Requests`

---

## Security Best Practices

### ✅ DO

- Always validate CAPTCHA server-side
- Use environment variables for production keys
- Enable rate limiting on all public endpoints
- Log failed CAPTCHA attempts for security monitoring
- Use HTTPS in production
- Rotate keys periodically

### ❌ DON'T

- Don't rely only on client-side validation
- Don't commit secret keys to source control
- Don't disable CAPTCHA in production
- Don't expose detailed error messages to users
- Don't use development keys in production

---

## Troubleshooting

### CAPTCHA Widget Not Loading

**Problem:** Turnstile widget doesn't appear on page

**Solutions:**
1. Check browser console for JavaScript errors
2. Verify Turnstile script is loaded: `https://challenges.cloudflare.com/turnstile/v0/api.js`
3. Ensure Site Key is correct in configuration
4. Check for Content Security Policy (CSP) blocking Cloudflare domains

### Validation Always Fails

**Problem:** Server-side validation returns error

**Solutions:**
1. Verify Secret Key is correct
2. Check `Turnstile:Enabled` is `true`
3. Ensure token is being passed from client to server
4. Check server logs for detailed error messages
5. Verify server can reach Cloudflare API (firewall/proxy issues)

### Rate Limiting Too Strict

**Problem:** Legitimate users getting blocked

**Solutions:**
1. Adjust rate limits in `appsettings.json`
2. Increase `PermitLimit` or `WindowSeconds`
3. Consider IP-based exemptions for trusted networks
4. Monitor rate limit logs to identify patterns

---

## Monitoring & Logging

### Log Events

The implementation logs the following events:

- ✅ Successful CAPTCHA validations
- ⚠️ Failed CAPTCHA validations (with error codes)
- ⚠️ Rate limit violations
- ❌ API communication errors
- ℹ️ CAPTCHA bypass (when disabled)

### Log Levels

- **Information:** Successful validations
- **Warning:** Failed validations, rate limits
- **Error:** API errors, configuration issues

### Example Log Queries

```csharp
// Find failed CAPTCHA attempts
_logger.LogWarning(
    "Turnstile validation failed. Email: {Email}, IP: {IP}, Errors: {Errors}",
    email, remoteIp, string.Join(", ", errorCodes));

// Find rate limit violations
_logger.LogWarning(
    "Rate limit exceeded for endpoint: {Endpoint}, IP: {IP}",
    endpoint, remoteIp);
```

---

## Next Steps

### ✅ Completed

1. ✅ Login page protected with Turnstile + rate limiting
2. ✅ Registration page protected with Turnstile + rate limiting
3. ✅ Forgot Password page protected with Turnstile + rate limiting
4. ✅ Server-side validation implemented
5. ✅ Rate limiting middleware configured
6. ✅ Build compiles successfully

### 🚀 Ready for Deployment

**Status:** Production Ready

**Pre-Deployment Checklist:**
- [ ] Obtain production Cloudflare Turnstile keys for `maintenx.runasp.net`
- [ ] Update `appsettings.Production.json` with real keys
- [ ] Verify `appsettings.Production.json` is in `.gitignore`
- [ ] Test build: `dotnet build --configuration Release`
- [ ] Publish: `dotnet publish --configuration Release`
- [ ] Deploy to maintenx.runasp.net
- [ ] Test all authentication flows in production
- [ ] Monitor server logs for errors
- [ ] Verify rate limiting works correctly

### Future Enhancements

- [ ] Add Turnstile to Admin User Creation (UserManagementController)
- [ ] Add Turnstile to public contact forms
- [ ] Implement IP-based exemptions for trusted networks
- [ ] Add CAPTCHA analytics dashboard
- [ ] Integrate with SIEM for security monitoring
- [ ] Add honeypot fields for additional bot detection

---

## Support

### Cloudflare Turnstile Documentation

- [Official Docs](https://developers.cloudflare.com/turnstile/)
- [API Reference](https://developers.cloudflare.com/turnstile/get-started/server-side-validation/)
- [Error Codes](https://developers.cloudflare.com/turnstile/troubleshooting/error-codes/)

### MaintenX Support

For implementation questions or issues:
1. Check server logs for detailed error messages
2. Review this documentation
3. Contact development team

---

## License

This implementation is part of the MaintenX CMMS platform.
Cloudflare Turnstile is a service provided by Cloudflare, Inc.
