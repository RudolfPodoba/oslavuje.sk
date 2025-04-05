using Microsoft.AspNetCore.Mvc;
using oslavuje.sk.Models.ViewModels;
using oslavuje.sk.Models;
using oslavuje.sk.Repositories;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;
using oslavuje.sk.Configuration;
using Microsoft.Extensions.Options;

namespace oslavuje.sk.Controllers.Surface;

public class MemberDataSurfaceController : SurfaceController
{
    private readonly IMemberManager _memberManager;
    private readonly MemberDataRepository _repository;
    private readonly ILogger<MemberDataSurfaceController> _logger;
    private readonly UmbracoStrankyConfig _presmerovanieConfig;

    public MemberDataSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IMemberManager memberManager,
        MemberDataRepository repository,
        ILogger<MemberDataSurfaceController> logger,
        IOptions<UmbracoStrankyConfig> presmerovanieConfig)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _memberManager = memberManager;
        _repository = repository;
        _logger = logger;
        _presmerovanieConfig = presmerovanieConfig.Value;
    }

    public async Task<IActionResult> Index()
    {
        var model = new MemberDataViewModel();

        try
        {
            var member = await _memberManager.GetUserAsync(User);
            if (member != null)
            {
                model.PreviousData = await _repository.GetMemberDataAsync(member.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri načítaní predchádzajúcich údajov");
        }

        return View("~/Views/Partials/MemberData/MemberDataForm.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(MemberDataViewModel model)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Chyba pri validácii údajov (MemberDataSurfaceController - save)");
            TempData["ErrorMessage"] = "Zadané údaje nie sú správne vyplnené.";
            return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
        }

        try
        {
            var member = await _memberManager.GetUserAsync(User);
            if (member == null)
            {
                _logger.LogError("Chyba pri získavaní prihláseného používateľa (MemberDataSurfaceController - save)");
                return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia_nedostupna);
            }

            var data = new MemberData
            {
                MemberId = member.Id,
                Email = member.Email ?? string.Empty,
                UserInput = model.UserInput,
                DateCreated = DateTime.UtcNow
            };

            await _repository.SaveUserDataAsync(data);

            TempData["SuccessMessage"] = "Vaše údaje boli úspešne uložené.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chyba pri ukladaní údajov");
            TempData["ErrorMessage"] = "Nastala chyba pri ukladaní údajov.";
        }

        return RedirectToUmbracoPage(contentKey: _presmerovanieConfig.ClenskaSekcia.Clenska_sekcia);
    }
}