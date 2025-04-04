using Microsoft.AspNetCore.Mvc;
using oslavuje.sk.Models.ViewModels;
using oslavuje.sk.Repositories;
using Umbraco.Cms.Core.Security;

namespace oslavuje.sk.Components;

public class MemberDataViewComponent : ViewComponent
{
    private readonly IMemberManager _memberManager;
    private readonly MemberDataRepository _repository;

    public MemberDataViewComponent(IMemberManager memberManager, MemberDataRepository repository)
    {
        _memberManager = memberManager;
        _repository = repository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new MemberDataViewModel();

        var member = await _memberManager.GetUserAsync(HttpContext.User);
        if (member != null)
        {
            model.PreviousData = await _repository.GetMemberDataAsync(member.Id);
        }

        return View("~/Views/Partials/MemberData/MemberDataForm.cshtml", model);
    }
}