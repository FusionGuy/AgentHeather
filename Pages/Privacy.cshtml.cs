// -----------------------------------------------------------------------------
// File: Pages/Privacy.cshtml.cs
//
// Purpose:
//   Code-behind (page model) for the static /Privacy Razor page. The page
//   is intentionally minimal – it renders a placeholder privacy policy
//   from the template. No model data is required.
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeatherDemoApp.Pages;

/// <summary>
/// Page model for <c>Pages/Privacy.cshtml</c>. Acts as the MVC binding
/// target for the privacy policy page and performs no server-side work.
/// </summary>
public class PrivacyModel : PageModel
{
    /// <summary>
    /// GET handler. Intentionally empty – the view is fully static.
    /// </summary>
    public void OnGet()
    {
    }
}
