using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.HttpOverrides;

using oslavuje.sk.Configuration;
using oslavuje.sk.Repositories;

using Serilog;

using Slimsy.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Z·kladn· Umbraco konfigur·cia
var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddSlimsy()
    .AddDeliveryApi()
    .AddComposers();

// Konfigur·cia Ëlenstva v Umbraco 13.7
umbracoBuilder.Build();

// Kontrola a registr·cia konfigur·cie emailov
var emailConfigSection = builder.Configuration.GetSection(OdosielanieEmailovConfig.SectionName);
if (!emailConfigSection.Exists())
{
    throw new InvalidOperationException($"Ch˝ba konfiguraËn· sekcia '{OdosielanieEmailovConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<OdosielanieEmailovConfig>(emailConfigSection);

// Kontrola a registr·cia konfigur·cie presmerovanÌ
var umbracoStrankySection = builder.Configuration.GetSection(UmbracoStrankyConfig.SectionName);
if (!umbracoStrankySection.Exists())
{
    throw new InvalidOperationException($"Ch˝ba konfiguraËn· sekcia '{UmbracoStrankyConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<UmbracoStrankyConfig>(umbracoStrankySection);

// Kontrola a registr·cia konfigur·cie s˙hlasov
var suhlasySection = builder.Configuration.GetSection(SuhlasyConfig.SectionName);
if (!suhlasySection.Exists())
{
    throw new InvalidOperationException($"Ch˝ba konfiguraËn· sekcia '{SuhlasyConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<SuhlasyConfig>(suhlasySection);

// Povie ASP.NET Core, aby pouûil Serilog a naËÌtal jeho nastavenia z appsettings.json
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// »lensk˙ autentifik·ciu bude rieöiù Umbraco automaticky
// D‘LEéIT…: Najprv autentifik·cia a autoriz·cia
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Pridaù pred vytvorenÌm WebApplication app = builder.Build();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // UrËuje, Ëi je potrebn˝ s˙hlas pouûÌvateæa pre cookies
    options.CheckConsentNeeded = context => true;
    // Minim·lny ötandard pre cookies
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = HttpOnlyPolicy.None;
    options.Secure = CookieSecurePolicy.Always;
});

// Registr·cia repozit·ra pre pouûÌvateæskÈ d·ta
builder.Services.AddScoped<MemberDataRepository>();
// Registr·cia repozit·ra pre darËeky a subdomÈny.
builder.Services.AddScoped<GiftRegistryRepository>();

WebApplication app = builder.Build();

// Toto je pre presmerovanie na HTTPS.
//if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    // Pridaù hneÔ po app.UseForwardedHeaders().
    app.UseCookiePolicy();
    app.UseHttpsRedirection();
    app.UseHsts();
}
// HTTPS END


// Middleware pre subdomÈny - presmeruje poûiadavky na hlavn˙ domÈnu s prÌsluönou cestou
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    var path = context.Request.Path.Value ?? string.Empty;
    var scheme = context.Request.Scheme;

    // PrÌstup k statick˝m s˙borom zachov·me
    if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/assets/") || path.StartsWith("/media/") || path.StartsWith("/scripts/"))
    {
        await next();
        return;
    }

    // Kontroluje, Ëi sme na subdomÈne (t.j. nie sme na hlavnej domÈne "oslavuje.sk")
    if (host != "oslavuje.sk" && host.EndsWith(".oslavuje.sk"))
    {
        // Extrahujeme len prv˙ Ëasù subdomÈny (pred prvou bodkou)
        string subdomain = host.Split('.')[0];

        // Sk˙siù naËÌtaù z datab·zy - pouûite scope pre prÌstup k repozit·ru
        using (var scope = app.Services.CreateScope())
        {
            var giftRegistryRepo = scope.ServiceProvider.GetRequiredService<GiftRegistryRepository>();
            var registry = await giftRegistryRepo.GetRegistryBySubdomainAsync(subdomain);

            if (registry != null && registry.IsActive)
            {
                // V middleware ponech·me presmerovanie ako je teraz
                string redirectUrl = $"{scheme}://oslavuje.sk/Home/Zoznam_darcekov?registryId={registry.Id}&subdomain={subdomain}";
                context.Response.Redirect(redirectUrl, permanent: false);

                return;
            }
        }

        // Ak subdomain nem· asociovan˝ registry, presmerujeme na hlavn˙ str·nku
        context.Response.Redirect($"{scheme}://oslavuje.sk/", permanent: false);
        return;
    }

    await next();
});

// ätart Umbraco
await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseInstallerEndpoints();
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();