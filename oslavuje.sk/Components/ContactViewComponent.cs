using Microsoft.AspNetCore.Mvc;

using oslavuje.sk.Models.ViewModels;

namespace oslavuje.sk.Components;

[ViewComponent(Name = "Contact")]
public class ContactViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ContactViewModel model)
    {
        model ??= new ContactViewModel();

        return View(model);
    }
}