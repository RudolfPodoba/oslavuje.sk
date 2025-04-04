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

// Základná Umbraco konfigurácia
var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddSlimsy()
    .AddDeliveryApi()
    .AddComposers();

// Konfigurácia èlenstva v Umbraco 13.7
umbracoBuilder.Build();

// Kontrola a registrácia konfigurácie emailov
var emailConfigSection = builder.Configuration.GetSection(OdosielanieEmailovConfig.SectionName);
if (!emailConfigSection.Exists())
{
    throw new InvalidOperationException($"Chıba konfiguraèná sekcia '{OdosielanieEmailovConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<OdosielanieEmailovConfig>(emailConfigSection);

// Kontrola a registrácia konfigurácie presmerovaní
var umbracoStrankySection = builder.Configuration.GetSection(UmbracoStrankyConfig.SectionName);
if (!umbracoStrankySection.Exists())
{
    throw new InvalidOperationException($"Chıba konfiguraèná sekcia '{UmbracoStrankyConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<UmbracoStrankyConfig>(umbracoStrankySection);

// Kontrola a registrácia konfigurácie súhlasov
var suhlasySection = builder.Configuration.GetSection(SuhlasyConfig.SectionName);
if (!suhlasySection.Exists())
{
    throw new InvalidOperationException($"Chıba konfiguraèná sekcia '{SuhlasyConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<SuhlasyConfig>(suhlasySection);

// Povie ASP.NET Core, aby pouil Serilog a naèítal jeho nastavenia z appsettings.json
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// Èlenskú autentifikáciu bude rieši Umbraco automaticky
// DÔLEITÉ: Najprv autentifikácia a autorizácia
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Prida pred vytvorením WebApplication app = builder.Build();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Urèuje, èi je potrebnı súhlas pouívate¾a pre cookies
    options.CheckConsentNeeded = context => true;
    // Minimálny štandard pre cookies
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = HttpOnlyPolicy.None;
    options.Secure = CookieSecurePolicy.Always;
});

// Registrácia repozitára pre pouívate¾ské dáta
builder.Services.AddScoped<MemberDataRepository>();
// Registrácia repozitára pre darèeky a subdomény.
builder.Services.AddScoped<GiftRegistryRepository>();

WebApplication app = builder.Build();

// Toto je pre presmerovanie na HTTPS.
//if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    // Prida hneï po app.UseForwardedHeaders().
    app.UseCookiePolicy();
    app.UseHttpsRedirection();
    app.UseHsts();
}
// HTTPS END

// Pridanie middleware pre dynamické preposielanie.
// Middleware pre subdomény - umoòuje presmerova rôzne subdomény na rôzne cesty v rámci aplikácie, prièom zachováva prístup k statickım súborom.
//app.Use(async (context, next) =>
//{
//    var host = context.Request.Host.Host;
//    var path = context.Request.Path.Value ?? string.Empty;
//    //Log.Information("Received 1 host: {host}", host);

//    // Prístup k statickım súborom zachováme
//    if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/assets/") || path.StartsWith("/media/") || path.StartsWith("/scripts/"))
//    {
//        await next();
//        return;
//    }

//    // Statické mapovanie subdomén (záloha ak databáza nie je dostupná/pre testovanie, èi aspoò to funguje :D, prípadne pre nejakı default...)
//    //var subdomainMappings = new Dictionary<string, string>
//    //{
//    //    { "wtke.oslavuje.sk", "/Home/Kontakt" },
//    //    { "pokus.oslavuje.sk", "/Home/about-this-page" },
//    //};


//    // Extrahujeme len prvú èas subdomény (pred prvou bodkou)
//    string subdomain = host.Split('.')[0];

//    // Skúsi naèíta z databázy - pouite scope pre prístup k repozitáru
//    using (var scope = app.Services.CreateScope())
//    {
//        var giftRegistryRepo = scope.ServiceProvider.GetRequiredService<GiftRegistryRepository>();
//        var registry = await giftRegistryRepo.GetRegistryBySubdomainAsync(subdomain);

//        if (registry != null && registry.IsActive)
//        {
//            // Cesta pre zobrazenie registra darèekov
//            context.Request.Path = "/Home/Zoznam_darcekov";
//            context.Items["CurrentGiftRegistry"] = registry.Id;

//            await next();
//            return;
//        }
//    }

//    // Pokraèuj so statickım mapovaním ak nebolo nájdené v databáze
//    //if (subdomainMappings.TryGetValue(host, out var targetPath))
//    //{
//    //    // Zmení cestu pre zobrazenie obsahu.
//    //    context.Request.Path = targetPath;
//    //}
//    await next();
//});
// Middleware pre subdomény - presmeruje poiadavky na hlavnú doménu s príslušnou cestou
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    var path = context.Request.Path.Value ?? string.Empty;
    var scheme = context.Request.Scheme;

    // Prístup k statickım súborom zachováme
    if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/assets/") || path.StartsWith("/media/") || path.StartsWith("/scripts/"))
    {
        await next();
        return;
    }

    // Kontroluje, èi sme na subdoméne (t.j. nie sme na hlavnej doméne "oslavuje.sk")
    if (host != "oslavuje.sk" && host.EndsWith(".oslavuje.sk"))
    {
        // Extrahujeme len prvú èas subdomény (pred prvou bodkou)
        string subdomain = host.Split('.')[0];

        // Skúsi naèíta z databázy - pouite scope pre prístup k repozitáru
        using (var scope = app.Services.CreateScope())
        {
            var giftRegistryRepo = scope.ServiceProvider.GetRequiredService<GiftRegistryRepository>();
            var registry = await giftRegistryRepo.GetRegistryBySubdomainAsync(subdomain);

            if (registry != null && registry.IsActive)
            {
                // Presmerovanie na hlavnú doménu so správnou cestou a registryId ako query parametrom
                //string redirectUrl = $"{scheme}://oslavuje.sk/Home/Zoznam_darcekov?registryId={registry.Id}";
                //context.Response.Redirect(redirectUrl, permanent: false);
                // V middleware ponecháme presmerovanie ako je teraz
                string redirectUrl = $"{scheme}://oslavuje.sk/Home/Zoznam_darcekov?registryId={registry.Id}&subdomain={subdomain}";
                context.Response.Redirect(redirectUrl, permanent: false);

                return;
            }
        }

        // Ak subdomain nemá asociovanı registry, presmerujeme na hlavnú stránku
        context.Response.Redirect($"{scheme}://oslavuje.sk/", permanent: false);
        return;
    }

    await next();
});

// Štart Umbraco
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