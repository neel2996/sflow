# Deployment Troubleshooting

## Credits showing 0 (but DB shows correct balance)

**Cause:** Your JWT token was issued by a different environment (e.g. localhost) and identifies a different user in production.

**Fix:** Log out and log in again when using the production extension:

1. Open the extension popup
2. Click **Logout**
3. Log in with your production credentials

This issues a fresh token for your production user and credits will display correctly.

**Prevention:** Use the same `Jwt__Key` on local and production so tokens work in both environments during development.

---

## Paywall opens but no purchase options

**Possible causes and fixes:**

### 1. Plans table empty

The app seeds plans on startup. If you see "No plans available. Ensure the database is seeded":

- Check Render logs for migration/seed errors
- Verify `ConnectionStrings__DefaultConnection` or `DATABASE_URL` is set in Render Environment
- Redeploy to trigger migrations and seed

### 2. Auth failure (401)

If the paywall shows an error like "Unauthorized" or "Invalid token":

- Log out and log in again in the extension (see Credits section above)
- Ensure `Jwt:Key` is set in Render Environment (same key across deploys)

### 3. Razorpay / Paddle not configured

For **India (INR)** plans you need:

- `Razorpay:KeyId`
- `Razorpay:KeySecret`
- `Razorpay:WebhookSecret` (for production webhooks)

For **USD** plans you need:

- `Paddle:ApiKey`
- `Paddle:WebhookSecret`

Set these in Render → Your Service → Environment.

### 4. Connection string on Render

Set one of:

- `ConnectionStrings__DefaultConnection` = your PostgreSQL connection string  
  (Use double underscore `__` for nested config)
- `DATABASE_URL` = `postgresql://user:pass@host:5432/dbname`  
  (If you use a Render Postgres instance, link it and use the internal URL)

---

## Required Render environment variables

| Variable | Required | Notes |
|----------|----------|------|
| `ConnectionStrings__DefaultConnection` or `DATABASE_URL` | Yes | PostgreSQL connection string |
| `Jwt:Key` | Yes | Min 32 chars, same across deploys (and same as local if switching envs) |
| `Jwt:Issuer` | Yes | e.g. `SourceFlow` |
| `Jwt:Audience` | Yes | e.g. `SourceFlow` |
| `Jwt:ExpiryDays` | No | Token lifetime in days (default: 30) |
| `Resend:ApiKey` | For forgot password | Resend.com API key (100 emails/day free) |
| `App:BaseUrl` | No | Base URL for reset links (default: request host) |
| `OpenAI:ApiKey` | Yes | For AI analysis |
| `Razorpay:KeyId` | For INR | Razorpay dashboard |
| `Razorpay:KeySecret` | For INR | Razorpay dashboard |
| `Razorpay:WebhookSecret` | For INR | Webhook verification |
| `Paddle:ApiKey` | For USD | Paddle dashboard |
| `Paddle:WebhookSecret` | For USD | Webhook verification |
