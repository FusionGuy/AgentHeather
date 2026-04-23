// -----------------------------------------------------------------------------
// File: Program.cs
//
// Purpose:
//   Application entry point and composition root for the HeatherDemoApp
//   ASP.NET Core Razor Pages web application.
//
//   This file performs three main jobs:
//     1. Configures the host / Kestrel (binding URL, ports).
//     2. Registers services into the DI container (Razor Pages, session,
//        anti-forgery, CORS, the IAgentService used by the UI and API).
//     3. Builds the HTTP request pipeline (middleware order) and maps
//        the Razor Pages and the minimal /api/chat endpoint used by the
//        SharePoint SPFx web part.
//
// Deployment notes:
//   * Runs in Azure App Service (Linux container or Windows plan). The
//     hosting environment exposes WEBSITES_PORT (or PORT) which we honor
//     in UseUrls.
//   * The site is embedded inside a SharePoint Online iframe via an SPFx
//     web part, so we relax cookie SameSite policies and replace the
//     default X-Frame-Options with a Content-Security-Policy
//     frame-ancestors directive that whitelists SharePoint origins.
// -----------------------------------------------------------------------------

using HeatherDemoApp.Services;

// Create the WebApplication builder (host, config, DI, logging).
var builder = WebApplication.CreateBuilder(args);

// ─── Kestrel URL binding ────────────────────────────────────────────────
// Ensure the app binds to the port provided by the hosting environment
// (Azure App Service sets WEBSITES_PORT). Fall back to PORT (used by some
// platforms) and finally to 80 for local / container defaults.
var portEnv = Environment.GetEnvironmentVariable("WEBSITES_PORT")
           ?? Environment.GetEnvironmentVariable("PORT")
           ?? "80";
builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");

// ─── Service registrations ──────────────────────────────────────────────
// Add services to the container.
builder.Services.AddRazorPages();

// Session state is backed by an in-memory distributed cache in this demo.
// In a multi-instance deployment, swap this for Redis or SQL Server.
builder.Services.AddDistributedMemoryCache();

// Configure session cookies for cross-origin iframe embedding (SharePoint).
// SameSite=None + Secure is required so the session cookie is sent on
// cross-origin POST requests made from inside the SharePoint iframe.
builder.Services.AddSession(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Configure anti-forgery cookies for cross-origin iframe embedding.
// Without SameSite=None the browser will not attach the anti-forgery cookie
// on form POSTs from the SharePoint iframe, causing a 400 Bad Request.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// CORS policy named "SharePointSPFx" – applied selectively to the
// /api/chat endpoint so the SPFx web part can call it from *.sharepoint.com
// (and from the SPFx local workbench at https://localhost:4321).
builder.Services.AddCors(options =>
{
    options.AddPolicy("SharePointSPFx", policy =>
    {
        policy.SetIsOriginAllowedToAllowWildcardSubdomains()
              .WithOrigins(
                  "https://*.sharepoint.com",
                  "https://*.sharepoint.us",
                  "https://localhost:4321"   // SPFx local workbench
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Heather's Azure AI Agent wrapper. Singleton is appropriate – the
// underlying Azure SDK client is thread-safe and meant to be reused.
builder.Services.AddSingleton<IAgentService, AgentService>();

// ─── Build the application ─────────────────────────────────────────────
var app = builder.Build();

// ─── HTTP request pipeline ─────────────────────────────────────────────
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Production-only error handling: redirect to /Error rather than
    // leaking stack traces to end users.
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for
    // production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();

// Allow this app to be embedded in an iframe from SharePoint Online and
// localhost. We remove the legacy X-Frame-Options header (set by some
// reverse proxies) and emit a modern CSP frame-ancestors directive.
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("X-Frame-Options");
    context.Response.Headers["Content-Security-Policy"] =
        "frame-ancestors 'self' https://*.sharepoint.com https://*.sharepoint.us https://localhost:*";
    await next();
});

// Serve files from wwwroot (CSS, JS, images, favicon, etc.).
app.UseStaticFiles();

// Enable endpoint routing.
app.UseRouting();

// Enable CORS (policies applied per-endpoint below via RequireCors).
app.UseCors();

// Session must be enabled before anything that reads HttpContext.Session
// (e.g. the chat history in Pages/Chat.cshtml.cs).
app.UseSession();

// Authorization middleware (no policies defined in this demo, but included
// so [Authorize] attributes would work if added later).
app.UseAuthorization();

// MapStaticAssets enables the .NET 9 static asset pipeline (fingerprinting, etc.).
app.MapStaticAssets();

// Map all Razor Pages (Index, Chat, Privacy, Error …) with static asset support.
app.MapRazorPages()
   .WithStaticAssets();

// ── API endpoint for the SharePoint SPFx web part ──────────────────────
// This routes requests through the same Azure AI Agent that the
// Razor Pages UI uses, so the SPFx web part doesn't need direct access
// to the AI project — it only talks to this endpoint.
app.MapPost("/api/chat", async (HttpContext ctx, IAgentService agentService) =>
{
    // Attempt to deserialize the incoming JSON body into our DTO.
    // A malformed body is silently treated as "empty" and rejected below.
    ChatApiRequest? body = null;
    try
    {
        body = await ctx.Request.ReadFromJsonAsync<ChatApiRequest>();
    }
    catch { /* bad JSON */ }

    // Validate input – the message field is required.
    var message = body?.Message?.Trim() ?? string.Empty;
    if (string.IsNullOrEmpty(message))
    {
        return Results.BadRequest(new { error = "The 'message' field is required." });
    }

    // Forward to the shared agent service and wrap the answer in a tiny
    // JSON object for the SPFx client.
    var answer = await agentService.AskAsync(message);
    return Results.Ok(new { response = answer });
})
.RequireCors("SharePointSPFx"); // Restrict cross-origin access to SharePoint.

// Start listening.
app.Run();

// ── Minimal request DTO for /api/chat ──────────────────────────────────
/// <summary>
/// Request body accepted by the <c>POST /api/chat</c> endpoint.
/// Declared as a top-level positional record so it can be deserialized
/// by <c>System.Text.Json</c> with minimum ceremony.
/// </summary>
/// <param name="Message">The user's free-text question.</param>
record ChatApiRequest(string? Message);
