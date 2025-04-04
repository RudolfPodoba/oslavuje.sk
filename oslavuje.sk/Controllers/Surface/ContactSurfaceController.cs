using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using oslavuje.sk.Configuration;
using oslavuje.sk.Models.ViewModels;
using oslavuje.sk.Utils;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace oslavuje.sk.Controllers.Surface;

public class ContactSurfaceController : SurfaceController
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ContactSurfaceController> _logger;
    private readonly OdosielanieEmailovConfig _odosielanieEmailovConfig;
    private readonly UmbracoStrankyConfig _presmerovanieConfig;

    public ContactSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IEmailSender emailSender,
        ILogger<ContactSurfaceController> logger,
        IOptions<OdosielanieEmailovConfig> odosielanieEmailovConfig,
        IOptions<UmbracoStrankyConfig> presmerovanieConfig) : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        (_emailSender, _logger, _odosielanieEmailovConfig, _presmerovanieConfig) = (emailSender, logger, odosielanieEmailovConfig.Value, presmerovanieConfig.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactViewModel model)
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
            return CurrentUmbracoPage();
        }

        try
        {
            var subject = "Ďakujeme, že ste nás kontaktovali";

            // Načítanie HTML šablóny
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "EmailTemplates", "Kontakt.html");
            string templateText = await System.IO.File.ReadAllTextAsync(templatePath);

            // Nahradenie placeholderov hodnotami
            var parameters = new List<(string ParamName, string ParamValue)>
            {
                ("EMAIL_SUBJ", subject),
                ("EMAIL_NAME", model.Name ?? string.Empty),
                ("EMAIL_TO", model.Email ?? string.Empty),
                ("EMAIL_PHONE", model.Phone ?? string.Empty),
                ("EMAIL_MSG", model.Message ?? string.Empty),
                ("WEB_ROOT", Request.Scheme + "://" + Request.Host)
            };

            templateText = EmailUtils.ApplyTemplateParameters(templateText, parameters);

            // Transformácia reťazcov do požadovaných polí
            string[]? ccArray = !string.IsNullOrEmpty(_odosielanieEmailovConfig?.EmailSettings?.Cc)
                ? _odosielanieEmailovConfig.EmailSettings.Cc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                : null;

            string[]? bccArray = !string.IsNullOrEmpty(_odosielanieEmailovConfig?.EmailSettings?.Bcc)
                ? _odosielanieEmailovConfig.EmailSettings.Bcc.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                : null;

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


            _logger.LogInformation("Pokus o odoslanie e-mailu z {From} na {To}, CC: {Cc}, BCC: {Bcc}",
                message.From, string.Join(", ", message.To),
                message.Cc != null ? string.Join(", ", message.Cc) : null,
                message.Bcc != null ? string.Join(", ", message.Bcc) : null);

            await _emailSender.SendAsync(message, emailType: "Contact");

            _logger.LogInformation("E-mail bol uspesne odoslany.");
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.KontaktnyFormular.Kontakt_odoslany);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba odoslania e-mailu kontaktného formulára: {ErrorMessage}", ex.Message);
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.KontaktnyFormular.Kontakt_zlyhal);
        }
    }
}