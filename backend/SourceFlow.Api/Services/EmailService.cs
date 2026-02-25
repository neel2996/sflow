namespace SourceFlow.Api.Services;

/// <summary>
/// Sends transactional emails via Resend (100/day free).
/// Set Resend:ApiKey in config. From address: onboarding@resend.dev (free tier).
/// </summary>
public class EmailService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public EmailService(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<bool> SendPasswordResetAsync(string toEmail, string resetLink)
    {
        var apiKey = _config["Resend:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return false;

        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

        var body = new
        {
            from = "SourceFlow <onboarding@resend.dev>",
            to = new[] { toEmail },
            subject = "Reset your SourceFlow password",
            html = $@"
<h2>Reset your password</h2>
<p>Click the link below to reset your SourceFlow password. This link expires in 1 hour.</p>
<p><a href=""{resetLink}"" style=""color:#0073b1"">{resetLink}</a></p>
<p>If you didn't request this, you can ignore this email.</p>
"
        };

        var res = await client.PostAsJsonAsync("https://api.resend.com/emails", body);
        return res.IsSuccessStatusCode;
    }
}
