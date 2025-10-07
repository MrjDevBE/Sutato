using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using Sutato.Client;
using Sutato.Client.Features.Auth.Services;
using Sutato.Shared.Features.Auth; // Add this



var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredSessionStorage();

// Auth state & handler
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<AuthHttpHandler>();


// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
});



// Register API HttpClient with AuthHttpHandler
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:BaseUrl"]);
})
.AddHttpMessageHandler<AuthHttpHandler>();

// Optional: a default HttpClient for local/static files
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

//var host = builder.Build();

//// Remove loader after app starts
//await host.RunAsync();
//var js = host.Services.GetRequiredService<IJSRuntime>();
//await js.InvokeVoidAsync("removeLoader");
var host = builder.Build();

// preload AuthState before app starts
using (var scope = host.Services.CreateScope())
{
    var authState = scope.ServiceProvider.GetRequiredService<AuthState>();
    await authState.LoadStateAsync();
}

// now run the app
await host.RunAsync();

// remove loader
var js = host.Services.GetRequiredService<IJSRuntime>();
await js.InvokeVoidAsync("removeLoader");
