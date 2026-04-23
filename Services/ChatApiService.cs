// -----------------------------------------------------------------------------
// File: Services/ChatApiService.cs
//
// Purpose:
//   Provides an HTTP client wrapper around a separate Azure Function chat API
//   that exposes a site-scoped knowledge base endpoint. This service is an
//   alternative (or supplementary) backend to <see cref="AgentService"/>:
//   where AgentService talks directly to Azure AI Foundry Agents, this
//   service POSTs a JSON body to an external HTTP endpoint and extracts the
//   human-readable <c>response</c> field from the returned JSON.
//
// Notes:
//   * The service is intentionally HttpClient-factory based so that
//     connection pooling, DNS refresh, and Polly-style policies can be
//     attached via the DI registration of the named client <c>"ChatApi"</c>.
//   * Only the <c>response</c> string from the upstream payload is surfaced
//     to callers; other metadata fields (site id, model, token counts) are
//     stripped to keep the UI layer simple.
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HeatherDemoApp.Services;

/// <summary>
/// Abstraction over the upstream Azure Function chat API. Consumers depend
/// on this interface so that the concrete <see cref="ChatApiService"/>
/// implementation can be swapped (e.g. for tests or alternative backends).
/// </summary>
public interface IChatApiService
{
    /// <summary>
    /// Sends a user message to the configured Azure Function chat API and
    /// returns the textual response extracted from the JSON body.
    /// </summary>
    /// <param name="message">The raw user message to forward upstream.</param>
    /// <returns>
    /// The upstream assistant's response text, or a human-readable error
    /// message if the call fails.
    /// </returns>
    Task<string> SendMessageAsync(string message);
}

/// <summary>
/// Strongly-typed model for the Azure Function chat API response body.
/// Only the <see cref="Response"/> field is surfaced to callers; the
/// remaining fields are captured for logging/telemetry purposes only.
/// </summary>
/// <remarks>
/// JSON property names are explicitly mapped via
/// <see cref="JsonPropertyNameAttribute"/> to decouple the upstream wire
/// format from our internal .NET naming conventions.
/// </remarks>
internal class ChatApiResponse
{
    /// <summary>The main assistant response text (the only field shown to users).</summary>
    [JsonPropertyName("response")]
    public string? Response { get; set; }

    /// <summary>Identifier of the knowledge-base "site" that produced the answer.</summary>
    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    /// <summary>Name of the underlying LLM used to generate the response.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    /// Token-usage metadata returned by the upstream service. Kept as a raw
    /// <see cref="JsonElement"/> because the shape may evolve and this
    /// service doesn't need to interpret it.
    /// </summary>
    [JsonPropertyName("tokensUsed")]
    public JsonElement? TokensUsed { get; set; }
}

/// <summary>
/// Default implementation of <see cref="IChatApiService"/> that issues an
/// HTTP POST to the configured chat API endpoint and returns only the
/// user-facing response text.
/// </summary>
public class ChatApiService : IChatApiService
{
    /// <summary>
    /// Factory used to obtain a pooled <see cref="HttpClient"/> named
    /// <c>"ChatApi"</c>. Using the factory avoids socket exhaustion and
    /// allows centralized policy configuration.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Application configuration used to resolve the endpoint URL.</summary>
    private readonly IConfiguration _configuration;

    /// <summary>Logger for diagnostic and error telemetry.</summary>
    private readonly ILogger<ChatApiService> _logger;

    /// <summary>
    /// JSON deserializer options shared by all calls. Case-insensitive
    /// matching lets us tolerate small property-name variations in the
    /// upstream payload.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a new <see cref="ChatApiService"/>.
    /// </summary>
    /// <param name="httpClientFactory">DI-supplied HTTP client factory.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public ChatApiService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ChatApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> SendMessageAsync(string message)
    {
        // Resolve the endpoint from configuration; fall back to a known dev
        // default so local runs work without extra setup.
        var endpoint = _configuration["ChatApi:Endpoint"]
            ?? "https://playwright-scraper-func.azurewebsites.net/api/sites/80af2781f16de622/chat";

        // Build the upstream JSON payload: { "message": "<text>" }.
        var payload = new { message };
        var jsonContent = JsonSerializer.Serialize(payload);

        _logger.LogInformation("Sending message to Chat API: {Endpoint}", endpoint);

        // Use a named client so HttpClientFactory can apply DI-configured policies.
        var client = _httpClientFactory.CreateClient("ChatApi");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log the server's body so we can diagnose upstream failures,
                // but return only a sanitized message to end users.
                _logger.LogError("Chat API returned {StatusCode}: {Body}", response.StatusCode, responseBody);
                return $"Sorry, I encountered an error communicating with the knowledge base. (HTTP {(int)response.StatusCode})";
            }

            // Extract only the human-readable "response" field from the API JSON.
            return ExtractResponseText(responseBody);
        }
        catch (TaskCanceledException)
        {
            // Thrown when HttpClient's timeout elapses (or caller cancels).
            _logger.LogError("Chat API request timed out for endpoint {Endpoint}", endpoint);
            return "Sorry, the request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            // Network-level failure (DNS, TCP, TLS, etc.).
            _logger.LogError(ex, "Chat API request failed for endpoint {Endpoint}", endpoint);
            return "Sorry, I was unable to reach the knowledge base. Please try again later.";
        }
    }

    /// <summary>
    /// Extracts only the <c>"response"</c> text from the upstream API JSON,
    /// discarding metadata fields like <c>SiteId</c>, <c>Model</c>, and
    /// <c>TokensUsed</c>.
    /// </summary>
    /// <param name="responseBody">Raw response body (expected to be JSON).</param>
    /// <returns>
    /// The extracted assistant text, or, if extraction fails, the raw body
    /// (so the caller at least sees something useful).
    /// </returns>
    private string ExtractResponseText(string responseBody)
    {
        try
        {
            // Fast path: strongly-typed deserialize with case-insensitive matching.
            var apiResponse = JsonSerializer.Deserialize<ChatApiResponse>(responseBody, JsonOptions);

            if (apiResponse != null && !string.IsNullOrWhiteSpace(apiResponse.Response))
            {
                return apiResponse.Response;
            }

            // Fallback: walk the JSON DOM to find a plausible "response" field
            // even if the upstream shape changes slightly.
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Handle the case where the entire body is just a JSON string.
            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString() ?? responseBody;
            }

            // Scan top-level properties for any string named "response"
            // regardless of case.
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String &&
                    prop.Name.Equals("response", StringComparison.OrdinalIgnoreCase))
                {
                    var text = prop.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            // Last-resort: log and hand the raw body back to the caller.
            _logger.LogWarning("Could not extract 'response' field from Chat API response, returning raw body");
            return responseBody;
        }
        catch (JsonException)
        {
            // Body wasn't JSON at all — treat it as plain text.
            return responseBody;
        }
    }
}
