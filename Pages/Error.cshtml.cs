// -----------------------------------------------------------------------------
// File: Pages/Error.cshtml.cs
//
// Purpose:
//   Code-behind (page model) for the global /Error Razor page. Used by the
//   production exception handler middleware (see Program.cs
//   <c>UseExceptionHandler("/Error")</c>) to render a friendly error page.
//
// Attributes:
//   * [ResponseCache(...)]       – ensures the error page is never cached.
//   * [IgnoreAntiforgeryToken]   – exception pages can be reached outside
//                                  a normal POST flow, so skip anti-forgery.
// -----------------------------------------------------------------------------

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HeatherDemoApp.Pages;

/// <summary>
/// Page model for <c>Pages/Error.cshtml</c>. Surfaces a request-correlation
/// identifier so operators can tie an end-user error to server-side logs.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    /// <summary>
    /// Correlation identifier associated with the failing request.
    /// Populated in <see cref="OnGet"/>.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Convenience flag consumed by the view to decide whether to render
    /// the request id (only when one is available).
    /// </summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>
    /// GET handler. Captures the current <see cref="Activity"/> id (or
    /// falls back to <see cref="HttpContext.TraceIdentifier"/>) so the
    /// user can quote it when reporting the error.
    /// </summary>
    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
