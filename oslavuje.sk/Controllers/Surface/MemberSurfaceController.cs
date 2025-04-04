using System.Text;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using oslavuje.sk.Configuration;
using oslavuje.sk.Models.ViewModels;
using oslavuje.sk.Utils;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;

namespace oslavuje.sk.Controllers.Surface;

public class MemberSurfaceController : SurfaceController
{
    private readonly IMemberService _memberService;
    private readonly IMemberManager _memberManager;
    private readonly IMemberSignInManager _memberSignInManager;
    private readonly IMemberGroupService _memberGroupService;
    private readonly ILogger<MemberSurfaceController> _logger;
    private readonly UmbracoStrankyConfig _presmerovanieConfig;
    private readonly OdosielanieEmailovConfig _odosielanieEmailovConfig;
    private readonly IEmailSender _emailSender;
    private readonly SuhlasyConfig _suhlasyConfig;

    public MemberSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IMemberService memberService,
        IMemberManager memberManager,
        IMemberSignInManager memberSignInManager,
        IMemberGroupService memberGroupService,
        ILogger<MemberSurfaceController> logger,
        IOptions<UmbracoStrankyConfig> presmerovanieConfig,
        IOptions<OdosielanieEmailovConfig> odosielanieEmailovConfig,
        IEmailSender emailSender,
        IOptions<SuhlasyConfig> suhlasyConfig)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        (_memberService, _memberManager, _memberSignInManager, _memberGroupService, _logger, _presmerovanieConfig, _odosielanieEmailovConfig, _emailSender, _suhlasyConfig) =
        (memberService, memberManager, memberSignInManager, memberGroupService, logger, presmerovanieConfig.Value, odosielanieEmailovConfig.Value, emailSender, suhlasyConfig.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registracia(RegistraciaViewModel model)
    {
        try
        {
            // Kontrola ochrany proti robotom.
            if (ModelState.IsValid)
            {
                if (!new ApiKeyValidator().IsValid(model.MojeHeslo ?? string.Empty, model.PotvrdMojeHeslo ?? string.Empty))
                {
                    ModelState.AddModelError("", "Musíte označiť, že nie ste robot.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Uloženie modelu do TempData pre zobrazenie zadaných hodnôt
                TempData["RegistraciaModel"] = System.Text.Json.JsonSerializer.Serialize(model);

                // Uloženie chýb do TempData
                var errors = new Dictionary<string, string>();
                bool hasGeneralError = false;

                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        if (string.IsNullOrEmpty(state.Key))
                        {
                            // Toto je všeobecná chyba (napr. "Musíte označiť, že nie ste robot.")
                            TempData["ErrorMessage"] = state.Value.Errors.First().ErrorMessage;
                            hasGeneralError = true;
                        }
                        else
                        {
                            errors[state.Key] = state.Value.Errors.First().ErrorMessage;
                        }
                    }
                }

                // Ak nemáme hlavnú chybu a existujú nejaké chyby, použijeme prvú ako hlavnú
                if (!hasGeneralError && errors.Count > 0)
                {
                    TempData["ErrorMessage"] = errors.First().Value;
                }

                if (errors.Count > 0)
                {
                    TempData["ValidationErrors"] = System.Text.Json.JsonSerializer.Serialize(errors);
                }

                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia);
            }

            // Kontrola, či používateľ udelil súhlas.
            if (!model.Consent)
            {
                ModelState.AddModelError("Consent", "Pred registráciou nám musíte udeliť súhlas s uložením vašich údajov.");
                TempData["ValidationErrors"] = System.Text.Json.JsonSerializer.Serialize(
                    new Dictionary<string, string> { { "Consent", "Pred registráciou nám musíte udeliť súhlas s uložením vašich údajov." } });
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia);
            }

            if (!model.ConsentServices)
            {
                ModelState.AddModelError("ConsentServices", "Pred registráciou nám musíte udeliť súhlas s podmienkami využívania služieb serveru oslavuje.sk.");
                TempData["ValidationErrors"] = System.Text.Json.JsonSerializer.Serialize(
                    new Dictionary<string, string> { { "ConsentServices", "Pred registráciou nám musíte udeliť súhlas s podmienkami využívania služieb serveru oslavuje.sk." } });
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia);
            }

            // Kontrola existujúceho emailu
            var existingUser = await _memberManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Tento e-mail je už zaregistrovaný";
                TempData["RegistraciaModel"] = System.Text.Json.JsonSerializer.Serialize(model);
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia);
            }

            // Vytvorenie nového člena ako nepovoleného
            var memberIdentityUser = new MemberIdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                IsApproved = false,
                MemberTypeAlias = "RegistrovanyPouzivatel"
            };

            var identityResult = await _memberManager.CreateAsync(memberIdentityUser, model.Password);
            if (!identityResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                TempData["RegistraciaModel"] = System.Text.Json.JsonSerializer.Serialize(model);
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia);
            }

            // Pridanie do skupiny "RegistrovanyPouzivatel"
            var memberGroup = _memberGroupService.GetById(new Guid("8e9f12f4-3772-4298-a8a1-5340ab3ffe86"));
            if (memberGroup != null && !string.IsNullOrEmpty(memberGroup.Name))
            {
                _memberService.AssignRoles([memberIdentityUser.UserName], [memberGroup.Name]);
            }

            var member = _memberService.GetByEmail(model.Email);
            if (member != null)
            {
                member.SetValue("umbracoPoznamkyOclenovi", "Používateľ bol vytvorený registráciou na stránke.");
                member.SetValue("suhlasGDPR", true);
                member.SetValue("suhlasPodmienky", true);
                member.SetValue("suhlasDatum", DateTime.UtcNow);
                member.SetValue("suhlasIpAdresa", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown" );
                member.SetValue("suhlasVerzie", "GDPR - " + _suhlasyConfig.Verzie.Suhlas_s_GDPR + "; Podmienky - " + _suhlasyConfig.Verzie.Suhlas_s_podmienkami_vyuzivania + ";" );
                _memberService.Save(member);
            }            

            // Generovanie aktivačného odkazu
            var activationLink = GenerateActivationLink(model.Email);

            // Načítanie HTML šablóny
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "EmailTemplates", "AktivaciaClena.html");
            string templateText = await System.IO.File.ReadAllTextAsync(templatePath);

            var subject = "Aktivácia účtu";
            var email = model.Email;

            // Nahradenie placeholderov hodnotami
            var parameters = new List<(string ParamName, string ParamValue)>
            {
                ("EMAIL_SUBJ", subject),
                ("EMAIL_NAME", model.Name ?? string.Empty),
                ("ACTIVATION_LINK", activationLink),
                ("EMAIL_TO", email)
            };

            templateText = EmailUtils.ApplyTemplateParameters(templateText, parameters);

            var message = new EmailMessage(
                from: _odosielanieEmailovConfig?.EmailSettings?.From,
                to: email,
                subject: subject,
                body: templateText,
                isBodyHtml: true
            );

            _logger.LogInformation("Pokus o odoslanie registracneho e-mailu pre {To}", message.To);
            await _emailSender.SendAsync(message, emailType: "Activation");

            // Presmerovanie na stránku po registrácii
            _logger.LogInformation("E-mail bol uspesne odoslany.");
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia_podakovanie);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri registrácii člena");
            TempData["ErrorMessage"] = "Nastala neočakávaná chyba pri registrácii. Ak sa chyba opakuje, kontaktujte nás.";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia);
        }

        //Prečo som skúsil túto metódu(s TempData)

        //1.Stabilita a spoľahlivosť: Metóda s TempData a presmerovaním je robustnejšia a menej náchylná na chyby v porovnaní s priamym vracaním stránky pomocou CurrentUmbracoPage(). Aj keď to môže vyzerať ako obchádzanie problému, je to praktickejšie riešenie v Umbraco prostredí.
        //2.Konzistencia prístupu: V metóde Registracia už používate tento prístup s TempData a funguje správne, takže použiť rovnaký prístup pre Prihlasenie zabezpečí konzistentnosť kódu.
        //3.Potenciálne problémy s kontextom: CurrentUmbracoPage() môže mať problémy s kontextom v rôznych scenároch(napr.pri POST požiadavkách), zatiaľ čo presmerovanie s TempData je univerzálnejšie riešenie.
    }

    [HttpGet]
    public async Task<IActionResult> ActivateMember(string token)
    {
        try
        {
            var email = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var member = await _memberManager.FindByEmailAsync(email);
            if (member != null)
            {
                member.IsApproved = true;
                await _memberManager.UpdateAsync(member);
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia_dokoncena);
            }

            _logger.LogWarning("Pokus o aktiváciu neexistujúceho člena: {Email}", email);
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia_neuspesna);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri aktivácii člena");
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Registracia_neuspesna);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Prihlasenie(PrihlasenieViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            var signInResult = await _memberSignInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                false);

            if (signInResult.Succeeded)
            {
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            ModelState.AddModelError("", "Nesprávny email alebo heslo");
            return CurrentUmbracoPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri prihlasovaní");
            ModelState.AddModelError("", "Nastala neočakávaná chyba pri prihlasovaní");
            return CurrentUmbracoPage();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Odhlasenie()
    {
        try
        {
            await _memberSignInManager.SignOutAsync();

            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia_odhlasenie_uspesne);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri odhlasovaní");
            return Redirect("/");
        }
    }

    [HttpGet]
    public async Task<IActionResult> IsAuthenticated()
    {
        try
        {
            var member = await _memberManager.GetUserAsync(User);

            return Json(new
            {
                isAuthenticated = member != null,
                memberName = member?.Name ?? "Používateľ",
                memberEmail = member?.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri kontrole autentifikácie");
            return Json(new { isAuthenticated = false });
        }
    }
    
    private string GenerateActivationLink(string email)
    {
        var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(email));
        var activationLink = Url.Action("ActivateMember", "MemberSurface", new { token }, Request.Scheme);
        return activationLink ?? string.Empty;
    }
}