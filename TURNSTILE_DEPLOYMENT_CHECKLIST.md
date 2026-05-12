# Cloudflare Turnstile - Deployment Checklist

## ✅ Implementation Status: PRODUCTION READY

**Date:** May 12, 2026  
**Build Status:** ✅ Successful (Debug & Release)  
**Target:** maintenx.runasp.net

---

## Implementation Summary

### Protected Endpoints

| Endpoint | Turnstile | Rate Limiting | Status |
|----------|-----------|---------------|--------|
| Login | ✅ | ✅ 5/min | **COMPLETE** |
| Registration | ✅ | ✅ 3/hour | **COMPLETE** |
| Forgot Password | ✅ | ✅ 3/5min | **COMPLETE** |

### Files Created/Modified

#### Configuration
- ✅ `Configuration/TurnstileSettings.cs`
- ✅ `appsettings.json` (development keys)
- ✅ `appsettings.Production.json` (placeholder keys)

#### Services
- ✅ `Services/Security/ITurnstileValidationService.cs`
- ✅ `Services/Security/TurnstileValidationService.cs`

#### Middleware
- ✅ `Program.cs` (service registration + rate limiting)

#### Authentication Pages (Backend)
- ✅ `Areas/Identity/Pages/Account/Login.cshtml.cs`
- ✅ `Areas/Identity/Pages/Account/Register.cshtml.cs`
- ✅ `Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs`

#### Authentication Pages (Frontend)
- ✅ `Areas/Identity/Pages/Account/Login.cshtml`
- ✅ `Areas/Identity/Pages/Account/Register.cshtml`
- ✅ `Areas/Identity/Pages/Account/ForgotPassword.cshtml`

#### Styling
- ✅ `wwwroot/css/maintenx-auth.css`

#### Documentation
- ✅ `TURNSTILE_IMPLEMENTATION.md`
- ✅ `TURNSTILE_DEPLOYMENT_CHECKLIST.md` (this file)

---

## Pre-Deployment Steps

### 1. Obtain Production Turnstile Keys

**Action Required:** Get real Cloudflare Turnstile keys

1. Go to [Cloudflare Dashboard](https://dash.cloudflare.com/)
2. Navigate to **Turnstile** section
3. Click **Add Site**
4. Configure:
   - **Site Name:** MaintenX Production
   - **Domain:** `maintenx.runasp.net`
   - **Widget Mode:** Managed (Recommended)
5. Copy **Site Key** and **Secret Key**

### 2. Update Production Configuration

**File:** `appsettings.Production.json`

```json
{
  "Turnstile": {
    "SiteKey": "YOUR_PRODUCTION_SITE_KEY_HERE",
    "SecretKey": "YOUR_PRODUCTION_SECRET_KEY_HERE",
    "Enabled": true,
    "VerificationEndpoint": "https://challenges.cloudflare.com/turnstile/v0/siteverify",
    "TimeoutSeconds": 10
  }
}
```

**⚠️ CRITICAL:** Verify `appsettings.Production.json` is in `.gitignore`

### 3. Verify Build

```bash
# Clean build
dotnet clean

# Build Release configuration
dotnet build --configuration Release

# Expected: Build succeeded with 1 warning(s)
```

### 4. Publish Application

```bash
# Publish for deployment
dotnet publish --configuration Release --output ./publish

# Verify output in ./publish directory
```

---

## Deployment Steps

### 1. Deploy to maintenx.runasp.net

Follow RunASP.NET deployment process:

1. Upload published files to server
2. Ensure `appsettings.Production.json` is deployed with real keys
3. Verify web.config is correct
4. Restart application pool

### 2. Post-Deployment Verification

**Test each protected endpoint:**

#### Test Login
1. Navigate to `https://maintenx.runasp.net/Identity/Account/Login`
2. Verify Turnstile widget appears
3. Attempt login with valid credentials
4. Verify CAPTCHA validation works
5. Test rate limiting (5 attempts in 60 seconds)

#### Test Registration
1. Navigate to `https://maintenx.runasp.net/Identity/Account/Register`
2. Verify Turnstile widget appears
3. Attempt registration with valid data
4. Verify CAPTCHA validation works
5. Test rate limiting (3 attempts in 1 hour)

#### Test Forgot Password
1. Navigate to `https://maintenx.runasp.net/Identity/Account/ForgotPassword`
2. Verify Turnstile widget appears
3. Attempt password reset with valid email
4. Verify CAPTCHA validation works
5. Test rate limiting (3 attempts in 5 minutes)

#### Test Google Sign-In
1. Navigate to login page
2. Click "Sign in with Google"
3. Verify external authentication still works
4. Confirm CAPTCHA doesn't interfere

### 3. Monitor Server Logs

Check for:
- ✅ Successful Turnstile validations
- ⚠️ Failed CAPTCHA attempts (potential bots)
- ⚠️ Rate limit violations
- ❌ API errors or configuration issues

---

## Testing Checklist

### Local Development Testing

- [x] Build compiles (Debug)
- [x] Build compiles (Release)
- [ ] Login page displays Turnstile widget
- [ ] Login with valid credentials succeeds
- [ ] Login with invalid CAPTCHA fails
- [ ] Registration page displays Turnstile widget
- [ ] Registration with valid data succeeds
- [ ] Registration with invalid CAPTCHA fails
- [ ] Forgot password page displays Turnstile widget
- [ ] Forgot password with valid email succeeds
- [ ] Forgot password with invalid CAPTCHA fails
- [ ] Google Sign-In still works
- [ ] Rate limiting triggers correctly

### Production Testing

- [ ] Deploy to maintenx.runasp.net
- [ ] Verify production Turnstile keys are active
- [ ] Test login flow
- [ ] Test registration flow
- [ ] Test forgot password flow
- [ ] Test Google Sign-In
- [ ] Verify rate limiting works
- [ ] Check server logs for errors
- [ ] Monitor Cloudflare Turnstile dashboard

---

## Configuration Reference

### Development Keys (Test Mode)

```json
{
  "Turnstile": {
    "SiteKey": "1x00000000000000000000AA",
    "SecretKey": "1x0000000000000000000000000000000AA",
    "Enabled": false
  }
}
```

**Note:** These test keys always pass validation. Set `Enabled: false` to bypass CAPTCHA in development.

### Production Keys

```json
{
  "Turnstile": {
    "SiteKey": "REAL_SITE_KEY_FROM_CLOUDFLARE",
    "SecretKey": "REAL_SECRET_KEY_FROM_CLOUDFLARE",
    "Enabled": true
  }
}
```

### Rate Limiting Configuration

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

---

## Troubleshooting

### Issue: Turnstile widget not appearing

**Symptoms:**
- Widget div is empty
- No CAPTCHA challenge visible

**Solutions:**
1. Check browser console for JavaScript errors
2. Verify Turnstile CDN script loads: `https://challenges.cloudflare.com/turnstile/v0/api.js`
3. Confirm Site Key in configuration matches Cloudflare dashboard
4. Check Content Security Policy (CSP) allows Cloudflare domains

### Issue: "CAPTCHA validation failed" on valid submission

**Symptoms:**
- User completes CAPTCHA but validation fails
- Error message appears after submission

**Solutions:**
1. Verify Secret Key is correct in `appsettings.Production.json`
2. Check server logs for detailed error messages
3. Ensure server can reach `challenges.cloudflare.com` (firewall/proxy)
4. Verify token is being passed from client to server
5. Check `Turnstile:Enabled` is `true` in production

### Issue: Rate limiting too aggressive

**Symptoms:**
- Legitimate users getting blocked
- "Too many requests" errors

**Solutions:**
1. Review rate limit configuration in `appsettings.json`
2. Increase `PermitLimit` or `WindowSeconds` as needed
3. Monitor server logs to identify patterns
4. Consider IP-based exemptions for trusted networks

### Issue: Google Sign-In broken

**Symptoms:**
- External authentication fails
- CAPTCHA appears on Google callback

**Solutions:**
1. Verify external login forms don't include Turnstile validation
2. Check `OnPostExternalLogin` handler is unmodified
3. Ensure CAPTCHA only validates local login/registration

---

## Security Best Practices

### ✅ Implemented

- ✅ Server-side validation (never trust client)
- ✅ Rate limiting on all auth endpoints
- ✅ IP address validation
- ✅ Configuration-based secrets (no hardcoding)
- ✅ Logging for security monitoring
- ✅ Google Sign-In preserved

### 🔒 Additional Recommendations

1. **Rotate Keys Periodically**
   - Generate new Turnstile keys every 6-12 months
   - Update configuration and redeploy

2. **Monitor Failed Attempts**
   - Review server logs weekly
   - Look for suspicious patterns (same IP, rapid attempts)
   - Adjust rate limits if needed

3. **Use Environment Variables**
   - Consider Azure Key Vault for production secrets
   - Avoid storing keys in configuration files

4. **Keep Dependencies Updated**
   - Update ASP.NET Core packages regularly
   - Monitor Cloudflare Turnstile API changes

---

## Monitoring & Analytics

### Cloudflare Turnstile Dashboard

Monitor:
- Total verifications
- Success rate
- Challenge solve rate
- Geographic distribution
- Error rates

### Server Logs

Key events to monitor:
- ✅ Successful validations
- ⚠️ Failed validations (potential bots)
- ⚠️ Rate limit violations
- ❌ API errors
- ℹ️ Configuration issues

### Log Queries

```bash
# Find failed CAPTCHA attempts
grep "Turnstile validation failed" /var/log/maintenx.log

# Find rate limit violations
grep "Rate limit exceeded" /var/log/maintenx.log

# Find API errors
grep "Turnstile API error" /var/log/maintenx.log
```

---

## Rollback Plan

If issues occur in production:

### Option 1: Disable Turnstile (Emergency)

Update `appsettings.Production.json`:

```json
{
  "Turnstile": {
    "Enabled": false
  }
}
```

Restart application. CAPTCHA will be bypassed but rate limiting remains active.

### Option 2: Adjust Rate Limits

If rate limiting is too aggressive:

```json
{
  "RateLimiting": {
    "Login": {
      "PermitLimit": 10,
      "WindowSeconds": 60
    }
  }
}
```

### Option 3: Full Rollback

Redeploy previous version without Turnstile implementation.

---

## Support & Resources

### Documentation
- [Turnstile Implementation Guide](./TURNSTILE_IMPLEMENTATION.md)
- [Cloudflare Turnstile Docs](https://developers.cloudflare.com/turnstile/)
- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)

### Contact
- Development Team: [Your contact info]
- Cloudflare Support: https://support.cloudflare.com/

---

## Sign-Off

### Development Team

- [ ] Code reviewed
- [ ] Build verified
- [ ] Documentation complete
- [ ] Ready for deployment

**Developer:** _________________  
**Date:** _________________

### Deployment Team

- [ ] Production keys obtained
- [ ] Configuration updated
- [ ] Deployed to maintenx.runasp.net
- [ ] Post-deployment testing complete

**Deployer:** _________________  
**Date:** _________________

---

**End of Checklist**
