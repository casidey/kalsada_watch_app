using KalsadaWatchApp.Components;
using KalsadaWatchApp.Components.Feedback;
using KalsadaWatchApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Supabase: one client for the whole app. This is a Blazor Server app, so the
// client runs on the server and the key never reaches the browser.
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];
if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey) ||
    supabaseUrl.Contains("YOUR-REF") || supabaseKey.EndsWith("..."))
{
    throw new InvalidOperationException(
        "Supabase is not configured. In appsettings.json, set Supabase:Url to your Project URL " +
        "and Supabase:Key to your publishable key (Supabase dashboard -> Connect, and Settings -> API Keys).");
}

var supabase = new Supabase.Client(supabaseUrl, supabaseKey,
    new Supabase.SupabaseOptions { AutoConnectRealtime = false });
await supabase.InitializeAsync();
builder.Services.AddSingleton(supabase);
builder.Services.AddSingleton<HazardReportService>();

// Citizen feedback reviews: approved reviews from Supabase (the approved_reviews view).
builder.Services.AddSingleton<IReviewSource, SupabaseReviewSource>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
