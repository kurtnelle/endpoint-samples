using BlazorApp.Components;
using BlazorApp.Services;
using GHIElectronics.Endpoint.Devices.Network;
using Iot.Device.Card.Ultralight;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
{
    Args = args,
    WebRootPath = "/root/.epdata/BlazorApp/wwwroot/"
});

builder.WebHost.ConfigureKestrel((options) => {
    options.ListenAnyIP(8080);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<WiFiNetworkingService>();

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

StartNetworkingService(app.Services);

app.Run();

void StartNetworkingService(IServiceProvider services)
{
    var scope = services.CreateScope();
    var wifi = scope.ServiceProvider.GetRequiredService<WiFiNetworkingService>();
    wifi.Enable();
}