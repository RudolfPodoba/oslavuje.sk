using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using oslavuje.sk.Configuration;
using oslavuje.sk.Models.GiftRegistry;
using oslavuje.sk.Models.ViewModels.GiftRegistry;
using oslavuje.sk.Repositories;
using oslavuje.sk.Utils;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Mail;


using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using System.Collections;
using Microsoft.Win32;

namespace oslavuje.sk.Controllers.Surface;

public class GiftRegistrySurfaceController : SurfaceController
{
    private readonly IMemberManager _memberManager;
    private readonly GiftRegistryRepository _giftRegistryRepository;
    private readonly ILogger<GiftRegistrySurfaceController> _logger;
    private readonly OdosielanieEmailovConfig _odosielanieEmailovConfig;
    private readonly IEmailSender _emailSender;
    private readonly UmbracoStrankyConfig _presmerovanieConfig;

    public GiftRegistrySurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IMemberManager memberManager,
        GiftRegistryRepository giftRegistryRepository,
        ILogger<GiftRegistrySurfaceController> logger,
        IOptions<OdosielanieEmailovConfig> odosielanieEmailovConfig,
        IEmailSender emailSender,
        IOptions<UmbracoStrankyConfig> presmerovanieConfig)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _memberManager = memberManager;
        _giftRegistryRepository = giftRegistryRepository;
        _logger = logger;
        _odosielanieEmailovConfig = odosielanieEmailovConfig.Value;
        _emailSender = emailSender;
        _presmerovanieConfig = presmerovanieConfig.Value;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGiftRegistry(GiftRegistryViewModel model)
    {
        try
        {
            // Validation
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Zadané údaje nie sú správne vyplnené.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
                //return CurrentUmbracoPage();
            }

            //if (!new ApiKeyValidator().IsValid(model.MojeHeslo ?? string.Empty, model.PotvrdMojeHeslo ?? string.Empty))
            //{
            //    ModelState.AddModelError("", "Musíte označiť, že nie ste robot.");
            //    return CurrentUmbracoPage();
            //}

            // Check if user is authenticated
            var member = await _memberManager.GetUserAsync(User);
            if (member == null)
            {
                ModelState.AddModelError("", "Pre vytvorenie registra darčekov musíte byť prihlásený.");
                //return CurrentUmbracoPage();
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia_nedostupna);
            }

            // Check if subdomain already exists
            if (await _giftRegistryRepository.SubdomainExistsAsync(model.SubdomainName.ToLower()))
            {
                ModelState.AddModelError("", "Zadaná subdoména je už obsadená.");
                TempData["ErrorMessage"] = "Zadaná subdoména je už obsadená.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Create registry
            var registry = new SubdomainGiftRegistry
            {
                SubdomainName = model.SubdomainName.ToLower(),
                Title = model.Title,
                Description = model.Description,
                OwnerId = Int32.Parse(member.Id),
                ExpiryDate = model.ExpiryDate,
                IsActive = true
            };

            bool success = await _giftRegistryRepository.CreateRegistryAsync(registry);
            if (!success)
            {
                ModelState.AddModelError("", "Nastala chyba pri vytváraní registra darčekov.");
                TempData["ErrorMessage"] = "Nastala chyba pri vytváraní registra darčekov.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
                //return CurrentUmbracoPage();
            }

            // Add gifts - už je to dvojkrokové, takže toto je len príklad, ako by to mohlo vyzerať
            // pri ďalšom to odstráň...
            foreach (var giftModel in model.Gifts.Where(g => !string.IsNullOrWhiteSpace(g.Name)))
            {
                var gift = new Gift
                {
                    Name = giftModel.Name,
                    Description = giftModel.Description,
                    Price = giftModel.Price,
                    ImageUrl = giftModel.ImageUrl,
                    RegistryId = registry.Id
                };

                
                await _giftRegistryRepository.AddGiftAsync(gift);
            }

            TempData["SuccessMessage"] = "Register darčekov bol úspešne vytvorený.";
            //return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            return RedirectToCurrentUmbracoPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri vytváraní registra darčekov");
            ModelState.AddModelError("", "Nastala neočakávaná chyba pri vytváraní registra darčekov.");
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            //return CurrentUmbracoPage();
        }
    }

    // In oslavuje.sk/Controllers/Surface/GiftRegistrySurfaceController.cs
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReserveGift(GiftReservationViewModel model)
    {
        try
        {
            // Pokúsime sa získať registryId z Request query parametra ak model.RegistryId nie je nastavené
            if (model.RegistryId == 0 &&
                HttpContext.Request.Query.ContainsKey("registryId") &&
                int.TryParse(HttpContext.Request.Query["registryId"], out int queryRegistryId))
            {
                model.RegistryId = queryRegistryId;
            }

            if (!ModelState.IsValid)
            {
                //foreach (var modelState in ModelState.Values)
                //{
                //    foreach (var error in modelState.Errors)
                //    {
                //        _logger.LogError($"Validation error: {error.ErrorMessage}");
                //    }
                //}

                TempData["ErrorMessage"] = "Zadané údaje nie sú správne vyplnené.";
                return RedirectToUmbracoPage(
                    contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                    queryString: new QueryString($"?registryId={model.RegistryId}")
                );

                //return RedirectToUmbracoPage(_presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov, new { registryId = model.RegistryId });
                //return RedirectToUmbracoPage(
                //    contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                //    routeValues: new { registryId = model.RegistryId }
                //    );
            }

            // Ochrana proti robotom.
            if (!new ApiKeyValidator().IsValid(model.MojeHeslo ?? string.Empty, model.PotvrdMojeHeslo ?? string.Empty))
            {
                TempData["ErrorMessage"] = "Musíte označiť, že nie ste robot.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                    queryString: new QueryString($"?registryId={model.RegistryId}"));
            }

            bool isGift = model.GiftId != 0;
            string? token = null;
            bool success;
            SubdomainGiftRegistry? registry = null;
            string? ownerEmail = null;

            if (!isGift)
            {
                // Vytvorenie nového darčeka typu "Potvrdenie účasti"
                registry = await _giftRegistryRepository.GetRegistryByIdAsync(model.RegistryId);
                if (registry == null)
                {
                    TempData["ErrorMessage"] = "Register darčekov nebol nájdený.";
                    return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                    queryString: new QueryString($"?registryId={model.RegistryId}"));
                }

                var potvrdenie = new Gift
                {
                    Name = "Potvrdenie účasti",
                    Description = $"Účasť potvrdil/a: {model.Name}",
                    RegistryId = registry.Id,                    
                };

                var (success2, giftId) = await _giftRegistryRepository.AddGiftAsync(potvrdenie);
                if (!success2)
                {
                    TempData["ErrorMessage"] = "Nepodarilo sa potvrdiť účasť.";
                    return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                    queryString: new QueryString($"?registryId={model.RegistryId}"));
                }
                if (giftId.HasValue) 
                {
                    model.GiftId = giftId.Value;
                }
            }

            // Pôvodná logika pre rezerváciu darčeka
            (success, token) = await _giftRegistryRepository.ReserveGiftAsync(model.GiftId, model.Email, model.Name);
            if (!success || token == null)
            {
                TempData["ErrorMessage"] = "Darček už bol rezervovaný alebo nie je k dispozícii.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                    queryString: new QueryString($"?registryId={model.RegistryId}"));
            }


            // Získanie e-mailu organizátora
            if (registry == null)
            {
                registry = await _giftRegistryRepository.GetRegistryByIdAsync(model.RegistryId);
            }
            if (registry != null)
            {
                ownerEmail = await _giftRegistryRepository.GetRegistryOwnerEmailAsync(registry.Id);
            }
                

            // Odoslanie potvrdzovacieho emailu
            await SendReservationConfirmationEmailAsync(model, token, isGift, ownerEmail);

            TempData["SuccessMessage"] = "Účasť bola potvrdená, darček bol úspešne rezervovaný. Na váš email bola odoslaná správa s potvrdením.";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                    queryString: new QueryString($"?registryId={model.RegistryId}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri rezervovaní darčeka");
            TempData["ErrorMessage"] = "Nastala neočakávaná chyba pri rezervovaní darčeka.";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Zoznam_darcekov,
                    queryString: new QueryString($"?registryId={model.RegistryId}"));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmReservation(string token)
    {
        try
        {
            // TOTO treba dokončiť s vlastnou stránkou. Teraz to netreba rezervácia sa potvrdí už po odoslaní e-mailu....



            bool success = await _giftRegistryRepository.ConfirmGiftReservationAsync(token);
            if (success)
            {
                TempData["SuccessMessage"] = "Rezervácia darčeka bola úspešne potvrdená.";
            }
            else
            {
                TempData["ErrorMessage"] = "Neplatný alebo expirovaný token.";
            }

            return RedirectToCurrentUmbracoPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri potvrdzovaní rezervácie darčeka");
            TempData["ErrorMessage"] = "Nastala neočakávaná chyba pri potvrdzovaní rezervácie.";
            return RedirectToCurrentUmbracoPage();
        }
    }

    // Add these methods to GiftRegistrySurfaceController
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGiftRegistry(GiftRegistryViewModel model)
    {
        try
        {
            // Validation
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Zadané údaje nie sú správne vyplnené.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Check if user is authenticated
            var member = await _memberManager.GetUserAsync(User);
            if (member == null)
            {
                ModelState.AddModelError("", "Pre úpravu registra darčekov musíte byť prihlásený.");
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia_nedostupna);
            }

            // Check if registry exists and belongs to the current user
            if (!model.RegistryId.HasValue)
            {
                TempData["ErrorMessage"] = "Zadaný register darčekov neexistuje.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Get existing registry
            var existingRegistry = await _giftRegistryRepository.GetRegistryByOwnerIdAsync(member.Id);
            if (existingRegistry == null || existingRegistry.Id != model.RegistryId)
            {
                TempData["ErrorMessage"] = "Nemáte oprávnenie upraviť tento register darčekov.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Update registry (you need to add UpdateRegistryAsync method to repository)
            existingRegistry.Title = model.Title;
            existingRegistry.Description = model.Description;
            existingRegistry.ExpiryDate = model.ExpiryDate;

            bool success = await _giftRegistryRepository.UpdateRegistryAsync(existingRegistry);
            if (!success)
            {
                ModelState.AddModelError("", "Nastala chyba pri aktualizácii registra darčekov.");
                TempData["ErrorMessage"] = "Nastala chyba pri aktualizácii registra darčekov.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Update existing gifts and add new ones
            foreach (var giftModel in model.Gifts.Where(g => !string.IsNullOrWhiteSpace(g.Name)))
            {
                if (giftModel.Id.HasValue)
                {
                    // Update existing gift
                    var gift = new Gift
                    {
                        Id = giftModel.Id.Value,
                        Name = giftModel.Name,
                        Description = giftModel.Description,
                        Price = giftModel.Price,
                        ImageUrl = giftModel.ImageUrl,
                        RegistryId = existingRegistry.Id
                    };
                    await _giftRegistryRepository.UpdateGiftAsync(gift);
                }
                else
                {
                    // Add new gift
                    var gift = new Gift
                    {
                        Name = giftModel.Name,
                        Description = giftModel.Description,
                        Price = giftModel.Price,
                        ImageUrl = giftModel.ImageUrl,
                        RegistryId = existingRegistry.Id
                    };
                    await _giftRegistryRepository.AddGiftAsync(gift);
                }
            }

            TempData["SuccessMessage"] = "Register darčekov bol úspešne aktualizovaný.";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri aktualizácii registra darčekov");
            ModelState.AddModelError("", "Nastala neočakávaná chyba pri aktualizácii registra darčekov.");
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
    }

    [HttpGet]
    public async Task<IActionResult> DeleteGiftRegistry(int id)
    {
        try
        {
            // Check if user is authenticated
            var member = await _memberManager.GetUserAsync(User);
            if (member == null)
            {
                TempData["ErrorMessage"] = "Pre odstránenie registra darčekov musíte byť prihlásený.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia_nedostupna);
            }

            // Get existing registry
            var existingRegistry = await _giftRegistryRepository.GetRegistryByOwnerIdAsync(member.Id);
            if (existingRegistry == null || existingRegistry.Id != id)
            {
                TempData["ErrorMessage"] = "Nemáte oprávnenie odstrániť tento register darčekov.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Delete registry (you need to add DeleteRegistryAsync method to repository)
            bool success = await _giftRegistryRepository.DeleteRegistryAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = "Nastala chyba pri odstraňovaní registra darčekov.";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            TempData["SuccessMessage"] = "Register darčekov bol úspešne odstránený.";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri odstraňovaní registra darčekov");
            TempData["ErrorMessage"] = "Nastala neočakávaná chyba pri odstraňovaní registra darčekov.";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGift(GiftRegistryViewModel model)
    {
        try
        {
            var newGift = model.NewGift;

            // Validation
            if (newGift == null || string.IsNullOrWhiteSpace(newGift.Name))
            {
                TempData["ErrorMessage"] = "Názov darčeka je povinný";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Check if user is authenticated
            var member = await _memberManager.GetUserAsync(User);
            if (member == null)
            {
                TempData["ErrorMessage"] = "Pre pridanie darčeka musíte byť prihlásený";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia_nedostupna);
            }

            // Get registry for current user
            var registry = await _giftRegistryRepository.GetRegistryByOwnerIdAsync(member.Id);
            if (registry == null)
            {
                TempData["ErrorMessage"] = "Najprv musíte vytvoriť register darčekov";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Add gift
            var gift = new Gift
            {
                Name = newGift.Name,
                Description = newGift.Description,
                Price = newGift.Price,
                ImageUrl = newGift.ImageUrl,
                RegistryId = registry.Id
            };

            var (success, giftId) = await _giftRegistryRepository.AddGiftAsync(gift);
            if (!success)
            {
                TempData["ErrorMessage"] = "Nastala chyba pri pridávaní darčeka";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            TempData["SuccessMessage"] = "Darček bol úspešne pridaný";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri pridávaní darčeka");
            TempData["ErrorMessage"] = "Nastala neočakávaná chyba pri pridávaní darčeka";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
    }

    [HttpGet] // Zmena z HttpPost na HttpGet pre jednoduchšie použitie vo View
    public async Task<IActionResult> DeleteGift(int id)
    {
        try
        {
            // Check if user is authenticated
            var member = await _memberManager.GetUserAsync(User);
            if (member == null)
            {
                TempData["ErrorMessage"] = "Pre odstránenie darčeka musíte byť prihlásený";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia_nedostupna);
            }

            // Get registry for current user
            var registry = await _giftRegistryRepository.GetRegistryByOwnerIdAsync(member.Id);
            if (registry == null)
            {
                TempData["ErrorMessage"] = "Register darčekov neexistuje";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Verify gift belongs to user's registry before deleting
            var gifts = await _giftRegistryRepository.GetGiftsForRegistryAsync(registry.Id);
            var giftToDelete = gifts.FirstOrDefault(g => g.Id == id);

            if (giftToDelete == null)
            {
                TempData["ErrorMessage"] = "Darček neexistuje alebo nepatrí k vášmu registru";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Check if gift is reserved
            if (!string.IsNullOrEmpty(giftToDelete.ReservedByEmail))
            {
                TempData["ErrorMessage"] = "Nemôžete odstrániť rezervovaný darček";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            // Delete gift
            bool success = await _giftRegistryRepository.DeleteGiftAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = "Nastala chyba pri odstraňovaní darčeka";
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
            }

            TempData["SuccessMessage"] = "Darček bol úspešne odstránený";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri odstraňovaní darčeka");
            TempData["ErrorMessage"] = "Nastala neočakávaná chyba pri odstraňovaní darčeka";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetGifts()
    {
        try
        {
            // Check if user is authenticated
            var member = await _memberManager.GetUserAsync(User);
            if (member == null)
            {
                return Json(new { success = false, message = "Pre zobrazenie darčekov musíte byť prihlásený" });
            }

            // Get registry for current user
            var registry = await _giftRegistryRepository.GetRegistryByOwnerIdAsync(member.Id);
            if (registry == null)
            {
                return Json(new { success = false, message = "Register darčekov neexistuje" });
            }

            // Get gifts
            var gifts = await _giftRegistryRepository.GetGiftsForRegistryAsync(registry.Id);
            var giftViewModels = gifts.Select(g => new {
                id = g.Id,
                name = g.Name,
                description = g.Description,
                price = g.Price,
                imageUrl = g.ImageUrl,
                isReserved = !string.IsNullOrEmpty(g.ReservedByEmail), // inak na to slúži IsConfirmed, no to až v druhej fáze, keď sa bude potvrdzovať rezervácia. Ak sa bude potvrdzovať... 
                registryId = g.RegistryId
            });

            return Json(new { success = true, gifts = giftViewModels });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri načítaní darčekov");
            return Json(new { success = false, message = "Nastala neočakávaná chyba pri načítaní darčekov" });
        }
    }

    private async Task SendReservationConfirmationEmailAsync(GiftReservationViewModel model, string? token, bool isGift, string? ownerEmail)
    {
        try
        {
            _logger.LogInformation("GiftRegistrySurfaceController-SendReservationConfirmationEmailAsync: Začiatok");

            string templatePath = string.Empty;
            string templateText = string.Empty;
            List<(string ParamName, string ParamValue)> parameters = new List<(string ParamName, string ParamValue)>();
            string subject = string.Empty;

            if (isGift)
            {
                // Build confirmation link
                var confirmationLink = Url.Action("ConfirmReservation", "GiftRegistrySurface",
                    new { token }, Request.Scheme);

                // Get email template
                templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "EmailTemplates", "RezervaciaDarcekaPotvrdenie.html");
                templateText = await System.IO.File.ReadAllTextAsync(templatePath);

                subject = "Potvrdenie účasti na oslave a rezervácie darčeka";

                // Replace placeholders
                parameters = new List<(string ParamName, string ParamValue)>
                {
                    ("EMAIL_NAME", model.Name),
                    ("EMAIL_SUBJ", subject),                    
                    ("GIFT_NAME", model.GiftName ?? "Názov darčeka nebol zadaný"),
                    ("GIFT_DESCRIPTION", model.GiftDescription ?? "Popis darčeka nebol zadaný"),
                    ("PARTY_TITLE", model.RegistryTitle ?? "Názov oslavy nebol zadaný"),
                    ("PARTY_DESCRIPTION", model.RegistryDescription ?? "Popis oslavy nebol zadaný"),
                    ("CONFIRMATION_LINK", confirmationLink ?? string.Empty),
                    ("EMAIL_TO", model.Email)
                };
            }
            else
            {
                templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "EmailTemplates", "PotvrdenieUcasti.html");
                templateText = await System.IO.File.ReadAllTextAsync(templatePath);

                subject = "Potvrdenie účasti na oslave";

                // Replace placeholders
                parameters = new List<(string ParamName, string ParamValue)>
                {
                    ("EMAIL_NAME", model.Name),
                    ("EMAIL_SUBJ", subject),
                    ("PARTY_TITLE", model.RegistryTitle ?? "Názov oslavy nebol zadaný"),
                    ("PARTY_DESCRIPTION", model.RegistryDescription ?? "Popis oslavy nebol zadaný"),
                    ("EMAIL_TO", model.Email)
                };
            }

            templateText = EmailUtils.ApplyTemplateParameters(templateText, parameters);
            _logger.LogInformation("GiftRegistrySurfaceController-SendReservationConfirmationEmailAsync: Stred");
            // Pridanie organizátora do CC
            string[]? ccArray = null;
            if (!string.IsNullOrEmpty(ownerEmail))
            {
                ccArray = [ownerEmail];
            }

            string[]? bccArray = null;

            var message = new EmailMessage(
                from: _odosielanieEmailovConfig?.EmailSettings?.From,
                to: [model.Email],
                cc: ccArray,
                bcc: bccArray,
                replyTo: null,
                subject: subject,
                body: templateText,
                isBodyHtml: true,
                attachments: null
            );

            _logger.LogInformation("GiftRegistrySurfaceController-SendReservationConfirmationEmailAsync: Skoro koniec");
            await _emailSender.SendAsync(message, emailType: "GiftReservation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri odosielaní potvrdzovacieho emailu");
        }
    }
}