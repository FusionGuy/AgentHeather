// -----------------------------------------------------------------------------
// File: Pages/Index.cshtml.cs
//
// Purpose:
//   Code-behind (page model) for the home page (/Index). Implements a
//   simple, stateless "single question / single answer" interaction with
//   the Heather AI agent.
//
//   Unlike <see cref="HeatherDemoApp.Pages.ChatModel"/>, this page does NOT
//   persist conversation history – each POST is treated as an isolated
//   question, suitable for quick Q&amp;A demos.
// -----------------------------------------------------------------------------

using HeatherDemoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeatherDemoApp.Pages;

/// <summary>
/// Page model for the landing page. Renders the "Ask Agent Heather" card
/// and handles a single Ask submission via a named page handler.
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>Backend service used to query the Heather AI agent.</summary>
    private readonly IAgentService _agentService;

    /// <summary>
    /// Constructs a new <see cref="IndexModel"/> with the DI-supplied agent service.
    /// </summary>
    /// <param name="agentService">Shared singleton agent service.</param>
    public IndexModel(IAgentService agentService)
    {
        _agentService = agentService;
    }

    /// <summary>
    /// The user's question, bound from the <c>Question</c> form field on POST.
    /// </summary>
    [BindProperty]
    public string? Question { get; set; }

    /// <summary>
    /// The agent's answer to the most recent question, rendered by the view
    /// when non-empty. Populated by <see cref="OnPostAskAsync"/>.
    /// </summary>
    public string? Answer { get; set; }

    /// <summary>
    /// GET handler. Currently a no-op – the view renders with empty
    /// <see cref="Question"/>/<see cref="Answer"/> fields.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Named page handler invoked when the form posts with
    /// <c>asp-page-handler="Ask"</c>. Sends the question to the agent and
    /// stores the response in <see cref="Answer"/> for rendering.
    /// </summary>
    /// <returns>The same page (no redirect).</returns>
    public async Task<IActionResult> OnPostAskAsync()
    {
        Answer = await _agentService.AskAsync(Question ?? string.Empty);
        return Page();
    }
}
