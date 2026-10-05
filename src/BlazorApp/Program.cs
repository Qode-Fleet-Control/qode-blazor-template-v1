using BlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Fleet: listen on 0.0.0.0:$PORT when the fleet (or compose) sets PORT, read at runtime.
// Without it, Kestrel keeps its usual defaults (launchSettings.json / ASPNETCORE_URLS).
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (app.Environment.IsDevelopment())
{
    // Fleet: only in development. On the fleet the edge terminates TLS and the app speaks
    // plain HTTP on $PORT, so there is no https port to redirect to.
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

// Fleet: HEALTH_PATH in fleet.conf.
app.MapHealthChecks("/health");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
