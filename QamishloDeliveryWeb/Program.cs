var builder = WebApplication.CreateBuilder(args);


// ============================================================
// RAZOR PAGES
// ============================================================

builder.Services.AddRazorPages();


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

    options.Cookie.Name =
        "QamishloDelivery.Session";
});


// ============================================================
// HTTP CLIENT
// ============================================================

builder.Services.AddHttpClient(
    "QamishloAPI",
    client =>
    {
        client.BaseAddress =
            new Uri(
                "https://qamishlorestaurantapi.miohammedomran.blitz.cloud/"
            );

        client.Timeout =
            TimeSpan.FromSeconds(15);
    }
);


// ============================================================
// BUILD APP
// ============================================================

var app = builder.Build();


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


// ============================================================
// RUN
// ============================================================

app.Run();