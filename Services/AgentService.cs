using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using HeatherDemoApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HeatherDemoApp.Services;

public interface IAgentService
{
    Task<string> AskAsync(string question);
    Task<string> AskAsync(List<ChatMessage> history, string question);
}

public class AgentService : IAgentService
{
    private readonly IPdfService _pdfService;
    private readonly IRetrievalService _retrieval;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IPdfService pdfService,
        IRetrievalService retrieval,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AgentService> logger)
    {
        _pdfService = pdfService;
        _retrieval = retrieval;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> AskAsync(string question)
    {
        return await AskAsync(new List<ChatMessage>(), question);
    }

    public async Task<string> AskAsync(List<ChatMessage> history, string question)
    {
        var endpoint = _configuration["AzureAI:Endpoint"];
        var model = _configuration["AzureAI:Model"] ?? "gpt-5.2-chat";
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
                    ?? _configuration["AzureAI:ApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            return "Agent is not configured. Missing AzureAI Endpoint or API Key.";
        }

        // Semantic retrieve top chunks using TF-IDF over ingested documents
        var topChunks = await _retrieval.GetTopChunksAsync(string.IsNullOrWhiteSpace(question) ? (history.LastOrDefault()?.Content ?? "") : question, 5);
        if (topChunks.Count == 0)
        {
            // Fallback: use first documents if retrieval not ready
            var fallbackDocs = (await _pdfService.GetAllDocumentsAsync()).Take(2).ToList();
            var ctx = new StringBuilder();
            int idx = 0;
            foreach (var d in fallbackDocs)
            {
                idx++;
                var excerpt = d.Content.Length > 2000 ? d.Content[..2000] : d.Content;
                ctx.AppendLine($"[{idx}] Title: {d.Title} (File: {d.FileName})");
                ctx.AppendLine(excerpt);
                ctx.AppendLine();
            }
            topChunks = new List<TextChunk>{ new TextChunk(fallbackDocs.First().Id, fallbackDocs.First().Title, fallbackDocs.First().FileName, ctx.ToString()) };
        }

        var targetName = _configuration["TargetSite:Name"] ?? "Planet Technologies";
        string contextText = BuildContextFromChunks(topChunks);

        var systemPrompt =
            $"You are a helpful chat assistant for {targetName}. Answer using only the provided context from the ingested documents. " +
            $"If the answer is not in the context, say you don't have enough information. Be concise and include citations in parentheses with the document title.";

        // Build conversation messages
        var messages = new List<object>
        {
            new { role = "system", content = new object[] { new { type = "input_text", text = systemPrompt } } }
        };
        foreach (var m in history)
        {
            messages.Add(new { role = m.Role, content = new object[] { new { type = "input_text", text = m.Content } } });
        }
        messages.Add(new {
            role = "user",
            content = new object[] { new { type = "input_text", text = $"Question: {question}\n\nContext:\n{contextText}" } }
        });

        var body = new
        {
            model,
            input = messages,
            max_output_tokens = 800
        };

        var client = _httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Add("api-key", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var res = await client.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(json);

            // Prefer the new Responses API shape: output[] -> message -> content[] -> { type: "output_text", text: "..." }
            if (doc.RootElement.TryGetProperty("output", out var outputEl) && outputEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in outputEl.EnumerateArray())
                {
                    if (item.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var c in contentEl.EnumerateArray())
                        {
                            if (c.TryGetProperty("type", out var t) && t.GetString() == "output_text"
                                && c.TryGetProperty("text", out var txt))
                            {
                                var s = txt.GetString();
                                if (!string.IsNullOrEmpty(s)) return s!;
                            }
                        }
                    }
                }
            }

            // Fallbacks for alternate shapes
            if (doc.RootElement.TryGetProperty("output_text", out var outputText))
            {
                return outputText.GetString() ?? string.Empty;
            }
            if (doc.RootElement.TryGetProperty("output", out var outputObj)
                && outputObj.ValueKind == JsonValueKind.Object
                && outputObj.TryGetProperty("text", out var textProp))
            {
                return textProp.GetString() ?? string.Empty;
            }

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("AzureAI response error {Status}: {Body}", res.StatusCode, json);
                return $"AzureAI error {res.StatusCode}: {json}";
            }
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AzureAI response: {Body}", json);
            return "Failed to parse response from Azure AI.";
        }
    }

    private static string BuildContextFromChunks(List<TextChunk> chunks)
    {
        var sb = new StringBuilder();
        int count = 0;
        foreach (var c in chunks)
        {
            count++;
            var excerpt = c.Text.Length > 1800 ? c.Text[..1800] : c.Text;
            sb.AppendLine($"[{count}] Title: {c.Title}");
            sb.AppendLine($"File: {c.FileName}");
            sb.AppendLine(excerpt);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
