using System.Text.Json;
using HeatherDemoApp.Models;
using HeatherDemoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeatherDemoApp.Pages;

public class ChatModel : PageModel
{
    private readonly IAgentService _agentService;

    public ChatModel(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public List<ChatMessage> Messages { get; set; } = new();

    [BindProperty]
    public string? UserInput { get; set; }

    public async Task OnGetAsync()
    {
        Messages = LoadHistory();
        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var history = LoadHistory();
        var userText = UserInput?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(userText))
        {
            history.Add(new ChatMessage { Role = "user", Content = userText });
            var answer = await _agentService.AskAsync(history, userText);
            history.Add(new ChatMessage { Role = "assistant", Content = answer });
        }
        SaveHistory(history);
        Messages = history;
        UserInput = string.Empty;
        return Page();
    }

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
                return new List<ChatMessage>();
            }
        }
        return new List<ChatMessage>();
    }

    private void SaveHistory(List<ChatMessage> history)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(history);
        HttpContext.Session.Set("chat_history", data);
    }
}
