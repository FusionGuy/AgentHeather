using HeatherDemoApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Ensure the app binds to the port provided by the hosting environment (Azure App Service sets WEBSITES_PORT)
// Fall back to PORT (used by some platforms) and finally to 80.
var portEnv = Environment.GetEnvironmentVariable("WEBSITES_PORT")
           ?? Environment.GetEnvironmentVariable("PORT")
           ?? "80";
builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddSingleton<IPdfService, PdfService>();
builder.Services.AddSingleton<IRetrievalService, TfIdfRetrievalService>();
builder.Services.AddSingleton<IAgentService, AgentService>();
 
var app = builder.Build();

// Initialize PDF service with pre-loaded data
using (var scope = app.Services.CreateScope())
{
    var pdfService = scope.ServiceProvider.GetRequiredService<IPdfService>();
    await pdfService.InitializeAsync();
    var retrieval = scope.ServiceProvider.GetRequiredService<IRetrievalService>();
    var docs = await pdfService.GetAllDocumentsAsync();
    await retrieval.InitializeAsync(docs);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
 
app.UseSession();
 
app.UseAuthorization();
 
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
 

app.Run();
