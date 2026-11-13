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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = "QamishloDelivery.Session";
});


// ============================================================
// HTTP CLIENT
// ============================================================

builder.Services.AddHttpClient(
    "QamishloAPI",
    client =>
    {
        string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"]
            ?? "https://qamishlorestaurantapi.miohammedomran.prod.page/";

        client.BaseAddress = new Uri(
            apiBaseUrl.EndsWith('/') ? apiBaseUrl : apiBaseUrl + "/");

        client.Timeout =
            TimeSpan.FromSeconds(15);
    }
);


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