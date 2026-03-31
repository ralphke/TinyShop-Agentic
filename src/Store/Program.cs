using System.Globalization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Localization;
using Store.Components;
using Store.Services;

var builder = WebApplication.CreateBuilder(args);
var enableHttpsRedirection = builder.Configuration.GetValue("EnableHttpsRedirection", true);
var productEndpoint = builder.Configuration["ProductEndpoint"] ?? "https://localhost:7130";
var cultureName = builder.Configuration["Localization:DefaultCulture"] ?? "en-IE";
var defaultCulture = CreateDefaultCulture(cultureName);

CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

builder.AddServiceDefaults();

builder.Services.AddHttpClient<ProductService>(client =>
{
    client.BaseAddress = new Uri(productEndpoint);
});

builder.Services.AddHttpClient<IShopApiService, ShopApiService>(client =>
{
    client.BaseAddress = new Uri(productEndpoint);
});

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CustomerSessionService>();
builder.Services.AddSingleton<CircuitHandler, CartCircuitHandler>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = [defaultCulture],
    SupportedUICultures = [defaultCulture]
});

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static CultureInfo CreateDefaultCulture(string cultureName)
{
    try
    {
        return CultureInfo.GetCultureInfo(cultureName);
    }
    catch (CultureNotFoundException)
    {
        return CultureInfo.GetCultureInfo("en-IE");
    }
}

namespace Store
{
    public partial class Program { }
}
