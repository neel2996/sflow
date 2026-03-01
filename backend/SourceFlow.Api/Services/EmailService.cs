namespace SourceFlow.Api.Services;

/// <summary>
/// Sends transactional emails via Brevo.
/// Set Brevo:ApiKey in config.
/// </summary>
public class EmailService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IHttpClientFactory http, IConfiguration config, ILogger<EmailService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetAsync(string toEmail, string resetLink)
    {
        return await SendEmailAsync(
            toEmail,
            "Reset your SourceFlow password",
            $@"
<h2>Reset your password</h2>
<p>Click the link below to reset your SourceFlow password. This link expires in 1 hour.</p>
<p><a href=""{resetLink}"" style=""color:#0073b1"">{resetLink}</a></p>
<p>If you didn't request this, you can ignore this email.</p>
");
    }

    public async Task<bool> SendEmailVerificationAsync(string toEmail, string verificationLink)
    {
        return await SendEmailAsync(
            toEmail,
            "Verify your SourceFlow email",
            $@"
<h2>Verify your email</h2>
<p>Use this one-time code to verify your SourceFlow account:</p>
<p style=""font-size:24px;font-weight:700;letter-spacing:2px"">{verificationLink}</p>
<p>This code expires in 10 minutes.</p>
<p>If you didn't create this account, you can ignore this email.</p>
");
    }

    public async Task<bool> SendEmailVerificationOtpAsync(string toEmail, string otpCode)
    {
        return await SendEmailAsync(
            toEmail,
            "Verify your SourceFlow email",
            $@"
<h2>Verify your email</h2>
<p>Use this one-time code to verify your SourceFlow account:</p>
<p style=""font-size:24px;font-weight:700;letter-spacing:2px"">{otpCode}</p>
<p>This code expires in 10 minutes.</p>
<p>If you didn't create this account, you can ignore this email.</p>
");
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string html)
    {
        var apiKey = _config["Brevo:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return false;

        var fromEmail = _config["Brevo:FromEmail"] ?? "no-reply@example.com";
        var fromName = _config["Brevo:FromName"] ?? "SourceFlow";

        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("api-key", apiKey);

        var body = new
        {
            sender = new
            {
                name = fromName,
                email = fromEmail
            },
            to = new[]
            {
                new { email = toEmail }
            },
            subject = subject,
            htmlContent = html
        };

        var res = await client.PostAsJsonAsync("https://api.brevo.com/v3/smtp/email", body);
        if (!res.IsSuccessStatusCode)
        {
            var errorBody = await res.Content.ReadAsStringAsync();
            _logger.LogWarning("Brevo send failed. Status={Status} To={ToEmail} Body={Body}", (int)res.StatusCode, toEmail, errorBody);
            return false;
        }

        var successBody = await res.Content.ReadAsStringAsync();
        _logger.LogInformation("Brevo email sent successfully. To={ToEmail} Response={Response}", toEmail, successBody);
        return true;
    }
}
