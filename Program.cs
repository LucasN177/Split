using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using Split;
using Supabase;
using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Services;
using Split.Infrastructure;
using Split.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Werte kommen lokal aus appsettings.Development.json, im Deployment aus den GitHub Secrets
var url = builder.Configuration["Supabase:Url"];
var key = builder.Configuration["Supabase:AnonKey"];

if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
    throw new InvalidOperationException("Supabase:Url ist nicht konfiguriert. Siehe README.");

builder.Services.AddSingleton<Client>(provider =>
    new Client(
        url, key, new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false,
            SessionHandler = new LocalStorageSessionPersistence(provider.GetRequiredService<IJSRuntime>())
        }
    )
);
builder.Services.AddSingleton<IAuthRepository, AuthRepository>();
builder.Services.AddSingleton<IGroupRepository, GroupRepository>();
builder.Services.AddSingleton<IExpenseRepository, ExpenseRepository>();
builder.Services.AddSingleton<IReceiptRepository, ReceiptRepository>();
builder.Services.AddSingleton<IPushRepository, PushRepository>();
builder.Services.AddSingleton<LocalStore>();

builder.Services.AddSingleton<AppStateService>();
builder.Services.AddSingleton<ConnectivityService>();
builder.Services.AddSingleton<OfflineSyncService>();
builder.Services.AddSingleton<RealtimeService>();
builder.Services.AddSingleton<PushService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IGroupService, GroupService>();
builder.Services.AddSingleton<IExpenseService, ExpenseService>();

// Deutsche Formatierung für Beträge und Datumsangaben
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("de-DE");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("de-DE");

await builder.Build().RunAsync();
