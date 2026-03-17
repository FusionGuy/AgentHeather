using HeatherDemoApp.Models;
using HeatherDemoApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
 
namespace HeatherDemoApp.Pages;

public class IndexModel : PageModel
{
    private readonly IPdfService _pdfService;
    private readonly IAgentService _agentService;
 
    public IndexModel(IPdfService pdfService, IAgentService agentService)
    {
        _pdfService = pdfService;
        _agentService = agentService;
    }
 
    public List<PdfDocument> Documents { get; set; } = new();
    public string? SearchTerm { get; set; }
 
    [BindProperty]
    public string? Question { get; set; }
    public string? Answer { get; set; }
 
    public async Task OnGetAsync(string? searchTerm)
    {
        SearchTerm = searchTerm;
        
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            Documents = await _pdfService.GetAllDocumentsAsync();
        }
        else
        {
            Documents = await _pdfService.SearchAsync(searchTerm);
        }
    }
 
    public async Task<IActionResult> OnPostAskAsync()
    {
        Answer = await _agentService.AskAsync(Question ?? string.Empty);
        Documents = await _pdfService.GetAllDocumentsAsync();
        return Page();
    }
}
