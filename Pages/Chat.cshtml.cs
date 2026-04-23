// -----------------------------------------------------------------------------
// File: Pages/Chat.cshtml.cs
//
// Purpose:
//   Code-behind (page model) for the /Chat Razor page. Provides a stateful
//   conversational UI backed by the shared <see cref="IAgentService"/>
//   (Azure AI Agents / Heather).
//
//   Conversation history is persisted in ASP.NET Core session state (as a
//   JSON-serialized <c>List&lt;ChatMessage&gt;</c>) so that it survives
//   standard HTTP POST-redirect-GET cycles without needing a database.
// -----------------------------------------------------------------------------

using System.Text.Json;
using HeatherDemoApp.Models;
using HeatherDemoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeatherDemoApp.Pages;

/// <summary>
/// Page model for <c>Pages/Chat.cshtml</c>. Handles GET (render current
/// conversation) and POST (append a new user turn, call the agent,
/// append the agent reply, persist history).
/// </summary>
public class ChatModel : PageModel
{
    /// <summary>Backend service used to query the Heather AI agent.</summary>
    private readonly IAgentService _agentService;

    /// <summary>
    /// Session storage key used to persist the serialized chat history.
    /// Declared as a local constant in the methods below; kept here as a
    /// single field would be fine too, but the code uses inline literals
    /// in <see cref="LoadHistory"/>/<see cref="SaveHistory"/>.
    /// </summary>
    // (intentionally no field – see LoadHistory/SaveHistory)

    /// <summary>
    /// Constructs a new <see cref="ChatModel"/>. DI supplies the agent
    /// service so the page can delegate the actual AI call.
    /// </summary>
    /// <param name="agentService">Shared singleton agent service.</param>
    public ChatModel(IAgentService agentService)
    {
        _agentService = agentService;
    }

    /// <summary>
    /// The ordered list of messages to render in the view. Populated from
    /// session on GET and updated in-memory on POST.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// Two-way model-bound text of the chat input box. Marked
    /// <see cref="BindPropertyAttribute"/> so it is populated from the
    /// posted form on POST.
    /// </summary>
    [BindProperty]
    public string? UserInput { get; set; }

    /// <summary>
    /// GET handler. Loads the existing conversation from session state so
    /// the view can re-render it. The <see langword="async"/> signature is
    /// kept for symmetry with <see cref="OnPostAsync"/>, even though no
    /// async work is performed here today.
    /// </summary>
    public async Task OnGetAsync()
    {
        Messages = LoadHistory();
        await Task.CompletedTask;
    }

    /// <summary>
    /// POST handler invoked when the user submits the chat form.
    /// Appends the new user message, asks the agent, appends the reply,
    /// and persists the updated history back to session state.
    /// </summary>
    /// <returns>The same Razor page (no redirect) so the answer is visible immediately.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        // Load the prior conversation so we can supply full context to the agent.
        var history = LoadHistory();
        var userText = UserInput?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(userText))
        {
            // Record the new user turn before calling the agent so the
            // replay logic in AgentService sees it (though AgentService
            // also separately re-asserts the final user question).
            history.Add(new ChatMessage { Role = "user", Content = userText });

            // Delegate to the shared agent service. The returned string is
            // already safe to render (Markdown, plain text, or a friendly
            // error message).
            var answer = await _agentService.AskAsync(history, userText);

            // Record the assistant turn so future rounds include this reply.
            history.Add(new ChatMessage { Role = "assistant", Content = answer });
        }

        // Persist and populate view data.
        SaveHistory(history);
        Messages = history;

        // Clear the input box so the view renders an empty field after the post.
        UserInput = string.Empty;

        return Page();
    }

    /// <summary>
    /// Reads and deserializes the chat history from session state.
    /// </summary>
    /// <returns>
    /// The persisted <see cref="ChatMessage"/> list, or an empty list if
    /// nothing was stored or deserialization failed.
    /// </returns>
    private List<ChatMessage> LoadHistory()
    {
        if (HttpContext.Session.TryGetValue("chat_history", out var bytes))
        {
            try
            {
                var msgs = JsonSerializer.Deserialize<List<ChatMessage>>(bytes);
                return msgs ?? new List<ChatMessage>();
            }
            catch
            {
                // Corrupted session payload – start fresh.
                return new List<ChatMessage>();
            }
        }
        return new List<ChatMessage>();
    }

    /// <summary>
    /// Serializes and writes the chat history to session state under the
    /// key <c>"chat_history"</c>.
    /// </summary>
    /// <param name="history">The history to persist.</param>
    private void SaveHistory(List<ChatMessage> history)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(history);
        HttpContext.Session.Set("chat_history", data);
    }
}
