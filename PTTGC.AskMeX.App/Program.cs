using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using PTTGC.AskMeGc;
using PTTGC.AskMeGc.BlazorCore;
using PTTGC.AskMeX.App;
using PTTGC.AskMeX.App.Core;
using PTTGC.AskMeX.App.Core.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMsalAuthentication(opt =>
{
    builder.Configuration.Bind("AzureAd", opt.ProviderOptions);

    opt.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/.default");
    opt.ProviderOptions.LoginMode = "Redirect";
    opt.ProviderOptions.Cache.CacheLocation = "localStorage";
    opt.UserOptions.RoleClaim = "roles";

}).AddAccountClaimsPrincipalFactory<ExtractRolesClaimsPrincipalFactory<RemoteUserAccount>>();

ExtractRolesClaimsPrincipalFactory<RemoteUserAccount>.ValidAUDClaim = builder.Configuration.GetValue<string>("AzureAd:Authentication:ClientId");

builder.Services.AddBlazorBootstrap();

// Register services
builder.Services.AddScoped<ChatSessionMediator>();

var sentryDSN = builder.Configuration.GetValue<string>("SentryDSN");
SentrySdk.Init(options =>
{
    options.Dsn = sentryDSN;

    // This option is recommended. It enables Sentry's "Release Health" feature.
    options.AutoSessionTracking = true;

    // Enabling this option is recommended for client applications only. It ensures all threads use the same global scope.
    options.IsGlobalModeEnabled = true;

    // This option will enable Sentry's tracing features. You still need to start transactions and spans.
    options.EnableTracing = true;

    // Example sample rate for your transactions: captures 10% of transactions
    options.TracesSampleRate = 0.1;
});

Configuration.Default.Container = builder.Configuration.GetValue<string>(key: "ConfigurationStorage");

AskMeXGateKeeperClient.Instance.BaseAddress = builder.Configuration.GetValue<string>("GateKeeperUrl");
AskMeXGateKeeperClient.Instance.ConfigContainer = builder.Configuration.GetValue<string>("ConfigurationStorage");
AskMeXGateKeeperClient.Instance.BlobStorageBaseAddress = builder.Configuration.GetValue<string>("BlobBaseAddress");

GCOpenAIPlatform.Instance.PlatformUrl = builder.Configuration.GetValue<string>("PlatformUrl");
GCOpenAIPlatform.Instance.ConfigContainer = builder.Configuration.GetValue<string>("ConfigurationStorage");
GCOpenAIPlatform.Instance.Company = builder.Configuration.GetValue<string>("CompanyCode");

Action<HttpRequestMessage> streamingEnable = (req) =>
{
    req.SetBrowserResponseStreamingEnabled(true);
};

GCOpenAIPlatform.Instance.ReqiestInitialization = streamingEnable;
AskMeXGateKeeperClient.Instance.RequestInitialization = streamingEnable;
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddLocalStorageServices();

var app = builder.Build();

var navigationManager = app.Services.GetRequiredService<NavigationManager>();
var currentHost = navigationManager.BaseUri;

currentHost = builder.Configuration.GetValue<string>("Referer");

GCOpenAIPlatform.Instance.ClientHost = currentHost;
AskMeXGateKeeperClient.Instance.ClientHost = currentHost;

//#if DEBUG

//GCOpenAIPlatform.Instance.PlatformUrl = "http://localhost:5169/";
//AskMeXGateKeeperClient.Instance.BaseAddress = "http://localhost:5170";

//#endif

await app.RunAsync();
