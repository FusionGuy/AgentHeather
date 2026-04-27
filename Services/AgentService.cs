// -----------------------------------------------------------------------------
// File: Services/AgentService.cs
//
// Purpose:
//   Encapsulates all communication with the Azure AI Foundry "Persistent
//   Agents" service used to power Heather, SWBC's HR policy assistant.
//
//   The service is registered as a singleton in Program.cs and consumed by:
//     * The Razor Pages UI (Pages/Index.cshtml.cs and Pages/Chat.cshtml.cs)
//     * The minimal /api/chat HTTP endpoint used by the SharePoint SPFx
//       web part.
//
// High-level flow of a chat turn:
//   1. Resolve the configured agent by its unique agent id.
//   2. Create a fresh persistent thread for this request.
//   3. Replay prior conversation messages (if any) onto the thread.
//   4. Post the new user question to the thread.
//   5. Start a run with additional grounding instructions that constrain
//      the agent to SWBC HR topics only.
//   6. Poll the run until it reaches a terminal state.
//   7. Read back the latest assistant message and return its text.
//   8. Delete the thread (best-effort) to avoid resource leaks.
//
// Configuration keys (appsettings.json / environment):
//   * AzureAIAgent:Endpoint – the AI project endpoint URL.
//   * AzureAIAgent:AgentId  – the id of the deployed persistent agent.
//
// Authentication:
//   Uses DefaultAzureCredential so the same code works locally (via Azure
//   CLI / Managed Identity for VS) and when deployed to Azure App Service
//   (via the site's Managed Identity).
// -----------------------------------------------------------------------------

using Azure;
using Azure.Identity;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;
using HeatherDemoApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HeatherDemoApp.Services;

/// <summary>
/// Defines the contract for a service that can answer questions by invoking
/// the Azure AI "Heather" agent. Consumers depend on this interface rather
/// than the concrete <see cref="AgentService"/> to keep the code testable
/// and to hide Azure-SDK types from the UI layer.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Asks the agent a single, stand-alone question (no prior history).
    /// </summary>
    /// <param name="question">The free-text question to send to the agent.</param>
    /// <returns>
    /// The agent's human-readable response. Returns a friendly fallback
    /// message if the call fails or the input is empty.
    /// </returns>
    Task<string> AskAsync(string question);

    /// <summary>
    /// Asks the agent a question while providing prior conversational
    /// history so the agent can respond in context.
    /// </summary>
    /// <param name="history">
    /// Ordered list of previously-exchanged <see cref="ChatMessage"/>
    /// instances. <c>user</c> messages are replayed as user turns and
    /// <c>assistant</c> messages are replayed as agent turns.
    /// </param>
    /// <param name="question">The new user question.</param>
    /// <returns>The agent's textual response.</returns>
    Task<string> AskAsync(List<ChatMessage> history, string question);
}

/// <summary>
/// Default implementation of <see cref="IAgentService"/> that talks to an
/// Azure AI Foundry Persistent Agents project.
/// </summary>
/// <remarks>
/// This class is safe to register as a singleton: the underlying
/// <see cref="PersistentAgentsClient"/> is thread-safe and designed to be
/// reused for the lifetime of the application.
/// </remarks>
public class AgentService : IAgentService
{
    /// <summary>
    /// Cached client used to create threads, post messages, and run the agent.
    /// Created once in the constructor and reused for every request.
    /// </summary>
    private readonly PersistentAgentsClient _agentsClient;

    /// <summary>
    /// The identifier of the specific deployed agent to invoke. Read from
    /// configuration key <c>AzureAIAgent:AgentId</c> at construction time.
    /// </summary>
    private readonly string _agentId;

    /// <summary>
    /// Logger used for diagnostic and error telemetry.
    /// </summary>
    private readonly ILogger<AgentService> _logger;

    /// <summary>
    /// Initializes a new <see cref="AgentService"/> by reading configuration,
    /// creating an Azure credential, and building a <see cref="PersistentAgentsClient"/>.
    /// </summary>
    /// <param name="configuration">Application configuration (appsettings.json / env vars).</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AgentService(
        IConfiguration configuration,
        ILogger<AgentService> logger)
    {
        _logger = logger;

        // Resolve the AI Project endpoint from configuration. Required – we
        // intentionally do NOT provide a fallback so a missing/typo'd config
        // key fails fast at startup instead of silently routing traffic to
        // the wrong Foundry project.
        var endpoint = configuration["AzureAIAgent:Endpoint"]
            ?? throw new InvalidOperationException(
                "Configuration value 'AzureAIAgent:Endpoint' is required but was not provided. " +
                "Set it in appsettings.json or as an App Service application setting.");

        // Resolve the agent id similarly; this is the id of the deployed
        // Heather agent in Azure AI Foundry. Required – no fallback.
        _agentId = configuration["AzureAIAgent:AgentId"]
            ?? throw new InvalidOperationException(
                "Configuration value 'AzureAIAgent:AgentId' is required but was not provided. " +
                "Set it in appsettings.json or as an App Service application setting.");

        _logger.LogInformation("Initializing AgentService with endpoint: {Endpoint}", endpoint);

        // Configure DefaultAzureCredential to skip credential types that are
        // unavailable in Azure App Service, reducing startup noise and latency.
        // - ExcludeVisualStudioCodeCredential: broken/deprecated VS Code auth flow.
        // - ExcludeInteractiveBrowserCredential: cannot pop a browser on a server.
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCodeCredential = true,
            ExcludeInteractiveBrowserCredential = true,
        };

        var credential = new DefaultAzureCredential(credentialOptions);

        // The high-level AIProjectClient exposes the sub-client we actually
        // need (persistent agents). We only keep a reference to the latter.
        var projectClient = new AIProjectClient(new Uri(endpoint), credential);
        _agentsClient = projectClient.GetPersistentAgentsClient();

        _logger.LogInformation("AgentService initialized successfully");
    }

    /// <summary>
    /// Convenience overload that forwards to
    /// <see cref="AskAsync(List{ChatMessage}, string)"/> with an empty history.
    /// </summary>
    /// <param name="question">The user's question.</param>
    /// <returns>The agent's response.</returns>
    public async Task<string> AskAsync(string question)
    {
        return await AskAsync(new List<ChatMessage>(), question);
    }

    /// <summary>
    /// Core implementation that sends a question to the agent while
    /// replaying the supplied conversation history for full context.
    /// </summary>
    /// <param name="history">Prior messages in the conversation (may be empty).</param>
    /// <param name="question">The new user question.</param>
    /// <returns>The agent's textual response, or a friendly error message.</returns>
    public async Task<string> AskAsync(List<ChatMessage> history, string question)
    {
        // Short-circuit on empty/whitespace input so we don't bill an agent run
        // for nothing useful.
        if (string.IsNullOrWhiteSpace(question))
        {
            return "Please ask a question and I'll do my best to help!";
        }

        _logger.LogInformation("Processing question via Azure AI Agent: {Question}", question);

        try
        {
            // ── Step 1: Look up the agent ─────────────────────────────────
            // Fetching the agent also validates the id and that we have
            // permission to invoke it.
            PersistentAgent agent = _agentsClient.Administration.GetAgent(_agentId).Value;

            // ── Step 2: Create an isolated thread for this request ────────
            // We create a new thread per-request to keep turns independent
            // and to avoid cross-user leakage.
            PersistentAgentThread thread = _agentsClient.Threads.CreateThread().Value;
            _logger.LogInformation("Created agent thread, ID: {ThreadId}", thread.Id);

            try
            {
                // ── Step 3: Replay prior conversation ─────────────────────
                // The agent needs earlier turns in the thread for contextual
                // responses (e.g. pronoun resolution, follow-up questions).
                foreach (var msg in history)
                {
                    // Skip blanks – they'd be rejected or add noise.
                    if (string.IsNullOrWhiteSpace(msg.Content)) continue;

                    // Map our simple role string to the SDK enum. Anything
                    // that isn't "assistant" is treated as a user message.
                    var role = msg.Role?.ToLowerInvariant() == "assistant"
                        ? MessageRole.Agent
                        : MessageRole.User;

                    _agentsClient.Messages.CreateMessage(thread.Id, role, msg.Content);
                }

                // ── Step 4: Post the current user question ────────────────
                _agentsClient.Messages.CreateMessage(
                    thread.Id,
                    MessageRole.User,
                    question);

                // ── Step 5: Start the run with grounding instructions ─────
                // These additional instructions are re-asserted on every run
                // to keep Heather on-topic and to prevent her from using
                // general training knowledge. This is a defense-in-depth
                // measure in addition to the agent's own system prompt.
                const string groundingInstructions =
                    "IMPORTANT: You are Heather, SWBC's HR assistant. You must ONLY answer using information " +
                    "from your knowledge source files containing SWBC HR policies and procedures. " +
                    "If the user's question is not covered by the knowledge source (e.g. recipes, code, trivia, " +
                    "or anything unrelated to SWBC HR), politely decline and say you can only help with SWBC HR topics. " +
                    "Do NOT use general training knowledge. Do NOT fabricate information.";

                ThreadRun run = _agentsClient.Runs.CreateRun(
                    thread.Id,
                    agent.Id,
                    overrideInstructions: null,
                    additionalInstructions: groundingInstructions).Value;

                // ── Step 6: Poll for completion ───────────────────────────
                // The SDK's async completion helper isn't used here so we can
                // retain full control over the polling cadence and diagnostics.
                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    run = _agentsClient.Runs.GetRun(thread.Id, run.Id).Value;
                }
                while (run.Status == RunStatus.Queued
                    || run.Status == RunStatus.InProgress);

                // Any status other than Completed (Failed, Cancelled, Expired,
                // RequiresAction, etc.) means we don't have a usable answer.
                if (run.Status != RunStatus.Completed)
                {
                    _logger.LogError("Agent run failed with status {Status}: {Error}",
                        run.Status, run.LastError?.Message);
                    return "Sorry, I encountered an error processing your request. Please try again.";
                }

                // ── Step 7: Read back the assistant's response ────────────
                // We request descending order so the freshly produced agent
                // reply is the first message we encounter.
                Pageable<PersistentThreadMessage> messages = _agentsClient.Messages.GetMessages(
                    thread.Id, order: ListSortOrder.Descending);

                foreach (PersistentThreadMessage threadMessage in messages)
                {
                    if (threadMessage.Role == MessageRole.Agent)
                    {
                        // An agent message may contain multiple content items
                        // (text, image, tool-call output, etc.). We concatenate
                        // every textual part to preserve the whole reply.
                        var responseText = new System.Text.StringBuilder();
                        foreach (MessageContent contentItem in threadMessage.ContentItems)
                        {
                            if (contentItem is MessageTextContent textItem)
                            {
                                responseText.Append(textItem.Text);
                            }
                        }
                        if (responseText.Length > 0)
                        {
                            return responseText.ToString();
                        }
                    }
                }

                // Reached if the run completed but no agent-authored text was
                // found (extremely rare, but handled gracefully).
                return "Sorry, I didn't receive a response. Please try again.";
            }
            finally
            {
                // ── Step 8: Best-effort thread cleanup ────────────────────
                // Threads are persistent by nature; we delete the one we
                // created to avoid accumulating single-use threads on the
                // AI project.
                try
                {
                    _agentsClient.Threads.DeleteThread(thread.Id);
                }
                catch (Exception ex)
                {
                    // A failed delete isn't fatal – log and move on.
                    _logger.LogWarning(ex, "Failed to delete agent thread {ThreadId}", thread.Id);
                }
            }
        }
        catch (Exception ex)
        {
            // Catch-all: network issues, auth failures, transient service
            // errors – surface a friendly message to the user while logging
            // full details for the operator.
            _logger.LogError(ex, "Error communicating with Azure AI Agent");
            return "Sorry, I was unable to reach the AI assistant. Please try again later.";
        }
    }
}
