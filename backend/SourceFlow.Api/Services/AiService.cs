using System.Text.Json;
using SourceFlow.Api.Dtos;

namespace SourceFlow.Api.Services;

public class AiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public AiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["OpenAI:ApiKey"]!;
        _model = config["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    public async Task<ScanResult> AnalyzeProfile(string jobDescription, string profileText)
    {
        var userPrompt = $$"""
            Compare the following job description against this LinkedIn profile.

            --- JOB DESCRIPTION ---
            {{jobDescription}}

            --- LINKEDIN PROFILE ---
            {{profileText}}

            The profile text includes a "COMPUTED TOTAL EXPERIENCE" section with a pre-calculated total.
            Use that number exactly for total_experience_years. Do NOT recalculate it yourself.
            Identify their seniority level based on experience and roles (Junior/Mid/Senior/Lead/Principal/Executive).

            Return STRICT JSON only with this schema:
            {
              "match_score": <number 0-100>,
              "total_experience_years": <number from COMPUTED TOTAL EXPERIENCE>,
              "seniority_level": "<Junior|Mid|Senior|Lead|Principal|Executive>",
              "strengths": [<string>, ...],
              "missing_skills": [<string>, ...],
              "summary": "<short recruiter summary including experience level>",
              "outreach_message": "<personalized outreach message>"
            }

            Scoring guide:
            90-100 = excellent match
            70-89  = strong match
            50-69  = partial match
            30-49  = weak match
            0-29   = poor match
            """;

        var requestBody = new
        {
            model = _model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "Act as a senior technical recruiter. Compare job descriptions against LinkedIn profiles. Be strict and realistic. Prioritize real experience over keywords. Detect seniority level and calculate total years of experience from role durations. Ignore fluff. Return STRICT JSON only."
                },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            response_format = new { type = "json_object" }
        };

        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var content = json!.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;

        var result = JsonSerializer.Deserialize<ScanResult>(content)
            ?? throw new Exception("Failed to parse AI response");

        return result;
    }
}
