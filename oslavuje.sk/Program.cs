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

// Z�kladn� Umbraco konfigur�cia
var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddSlimsy()
    .AddDeliveryApi()
    .AddComposers();

// Konfigur�cia �lenstva v Umbraco 13.7
umbracoBuilder.Build();

// Kontrola a registr�cia konfigur�cie emailov
var emailConfigSection = builder.Configuration.GetSection(OdosielanieEmailovConfig.SectionName);
if (!emailConfigSection.Exists())
{
    throw new InvalidOperationException($"Ch�ba konfigura�n� sekcia '{OdosielanieEmailovConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<OdosielanieEmailovConfig>(emailConfigSection);

// Kontrola a registr�cia konfigur�cie presmerovan�
var umbracoStrankySection = builder.Configuration.GetSection(UmbracoStrankyConfig.SectionName);
if (!umbracoStrankySection.Exists())
{
    throw new InvalidOperationException($"Ch�ba konfigura�n� sekcia '{UmbracoStrankyConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<UmbracoStrankyConfig>(umbracoStrankySection);

// Kontrola a registr�cia konfigur�cie s�hlasov
var suhlasySection = builder.Configuration.GetSection(SuhlasyConfig.SectionName);
if (!suhlasySection.Exists())
{
    throw new InvalidOperationException($"Ch�ba konfigura�n� sekcia '{SuhlasyConfig.SectionName}' v appsettings.json");
}
builder.Services.Configure<SuhlasyConfig>(suhlasySection);

// Povie ASP.NET Core, aby pou�il Serilog a na��tal jeho nastavenia z appsettings.json
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// �lensk� autentifik�ciu bude rie�i� Umbraco automaticky
// D�LE�IT�: Najprv autentifik�cia a autoriz�cia
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Prida� pred vytvoren�m WebApplication app = builder.Build();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // Ur�uje, �i je potrebn� s�hlas pou��vate�a pre cookies
    options.CheckConsentNeeded = context => true;
    // Minim�lny �tandard pre cookies
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = HttpOnlyPolicy.None;
    options.Secure = CookieSecurePolicy.Always;
});

// Registr�cia repozit�ra pre pou��vate�sk� d�ta
builder.Services.AddScoped<MemberDataRepository>();
// Registr�cia repozit�ra pre dar�eky a subdom�ny.
builder.Services.AddScoped<GiftRegistryRepository>();

WebApplication app = builder.Build();

// Toto je pre presmerovanie na HTTPS.
//if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    // Prida� hne� po app.UseForwardedHeaders().
    app.UseCookiePolicy();
    app.UseHttpsRedirection();
    app.UseHsts();
}
// HTTPS END

// Pridanie middleware pre dynamick� preposielanie.
// Middleware pre subdom�ny - umo��uje presmerova� r�zne subdom�ny na r�zne cesty v r�mci aplik�cie, pri�om zachov�va pr�stup k statick�m s�borom.
//app.Use(async (context, next) =>
//{
//    var host = context.Request.Host.Host;
//    var path = context.Request.Path.Value ?? string.Empty;
//    //Log.Information("Received 1 host: {host}", host);

//    // Pr�stup k statick�m s�borom zachov�me
//    if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/assets/") || path.StartsWith("/media/") || path.StartsWith("/scripts/"))
//    {
//        await next();
//        return;
//    }

//    // Statick� mapovanie subdom�n (z�loha ak datab�za nie je dostupn�/pre testovanie, �i aspo� to funguje :D, pr�padne pre nejak� default...)
//    //var subdomainMappings = new Dictionary<string, string>
//    //{
//    //    { "wtke.oslavuje.sk", "/Home/Kontakt" },
//    //    { "pokus.oslavuje.sk", "/Home/about-this-page" },
//    //};


//    // Extrahujeme len prv� �as� subdom�ny (pred prvou bodkou)
//    string subdomain = host.Split('.')[0];

//    // Sk�si� na��ta� z datab�zy - pou�ite scope pre pr�stup k repozit�ru
//    using (var scope = app.Services.CreateScope())
//    {
//        var giftRegistryRepo = scope.ServiceProvider.GetRequiredService<GiftRegistryRepository>();
//        var registry = await giftRegistryRepo.GetRegistryBySubdomainAsync(subdomain);

//        if (registry != null && registry.IsActive)
//        {
//            // Cesta pre zobrazenie registra dar�ekov
//            context.Request.Path = "/Home/Zoznam_darcekov";
//            context.Items["CurrentGiftRegistry"] = registry.Id;

//            await next();
//            return;
//        }
//    }

//    // Pokra�uj so statick�m mapovan�m ak nebolo n�jden� v datab�ze
//    //if (subdomainMappings.TryGetValue(host, out var targetPath))
//    //{
//    //    // Zmen� cestu pre zobrazenie obsahu.
//    //    context.Request.Path = targetPath;
//    //}
//    await next();
//});
// Middleware pre subdom�ny - presmeruje po�iadavky na hlavn� dom�nu s pr�slu�nou cestou
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    var path = context.Request.Path.Value ?? string.Empty;
    var scheme = context.Request.Scheme;

    // Pr�stup k statick�m s�borom zachov�me
    if (path.StartsWith("/css/") || path.StartsWith("/js/") || path.StartsWith("/assets/") || path.StartsWith("/media/") || path.StartsWith("/scripts/"))
    {
        await next();
        return;
    }

    // Kontroluje, �i sme na subdom�ne (t.j. nie sme na hlavnej dom�ne "oslavuje.sk")
    if (host != "oslavuje.sk" && host.EndsWith(".oslavuje.sk"))
    {
        // Extrahujeme len prv� �as� subdom�ny (pred prvou bodkou)
        string subdomain = host.Split('.')[0];

        // Sk�si� na��ta� z datab�zy - pou�ite scope pre pr�stup k repozit�ru
        using (var scope = app.Services.CreateScope())
        {
            var giftRegistryRepo = scope.ServiceProvider.GetRequiredService<GiftRegistryRepository>();
            var registry = await giftRegistryRepo.GetRegistryBySubdomainAsync(subdomain);

            if (registry != null && registry.IsActive)
            {
                // Presmerovanie na hlavn� dom�nu so spr�vnou cestou a registryId ako query parametrom
                //string redirectUrl = $"{scheme}://oslavuje.sk/Home/Zoznam_darcekov?registryId={registry.Id}";
                //context.Response.Redirect(redirectUrl, permanent: false);
                // V middleware ponech�me presmerovanie ako je teraz
                string redirectUrl = $"{scheme}://oslavuje.sk/Home/Zoznam_darcekov?registryId={registry.Id}&subdomain={subdomain}";
                context.Response.Redirect(redirectUrl, permanent: false);

                return;
            }
        }

        // Ak subdomain nem� asociovan� registry, presmerujeme na hlavn� str�nku
        context.Response.Redirect($"{scheme}://oslavuje.sk/", permanent: false);
        return;
    }

    await next();
});

// �tart Umbraco
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