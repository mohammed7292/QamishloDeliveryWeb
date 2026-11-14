using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);


// ============================================================
// RAZOR PAGES
// ============================================================

builder.Services.AddRazorPages();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


// ============================================================
// SESSION
// ============================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout =
        TimeSpan.FromHours(12);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "QamishloDelivery.Session";
});


// ============================================================
// HTTP CLIENT
// ============================================================

builder.Services.AddHttpClient("QamishloAPI", client =>
{
    // Blitz can override this with ApiSettings__BaseUrl. Keep the fallback
    // pointed at the published API, never localhost or a developer machine.
    string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
        ?? "https://qamishlorestaurantapi.miohammedomran.prod.page/";

    if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out Uri? baseUri) ||
        (baseUri.Scheme != Uri.UriSchemeHttps && !builder.Environment.IsDevelopment()))
    {
        throw new InvalidOperationException(
            "ApiSettings:BaseUrl must be an absolute HTTPS URL in production. Set Blitz variable ApiSettings__BaseUrl.");
    }

    client.BaseAddress = new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(25);
});


// ============================================================
// BUILD APP
// ============================================================

var app = builder.Build();

app.UseForwardedHeaders();


// ============================================================
// ERROR HANDLING
// ============================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    app.UseHsts();
}


// ============================================================
// HTTPS
// ============================================================

app.UseHttpsRedirection();


// ============================================================
// STATIC FILES
// ============================================================

app.UseStaticFiles();


// ============================================================
// ROUTING
// ============================================================

app.UseRouting();


// ============================================================
// SESSION
// IMPORTANT:
// Must be BEFORE Razor Pages
// ============================================================

app.UseSession();


// ============================================================
// AUTHORIZATION
// ============================================================

app.UseAuthorization();


// ============================================================
// RAZOR PAGES
// ============================================================

app.MapRazorPages();
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "QamishloDeliveryWeb",
    utc = DateTime.UtcNow
}));


// ============================================================
// RUN
// ============================================================

app.Run();