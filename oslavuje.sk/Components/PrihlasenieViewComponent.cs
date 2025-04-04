using Microsoft.AspNetCore.Mvc;

using oslavuje.sk.Models.ViewModels;

namespace oslavuje.sk.Components;

[ViewComponent(Name = "Prihlasenie")]
public class PrihlasenieViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PrihlasenieViewModel model)
    {
        model ??= new PrihlasenieViewModel();

        return View(model);
    }
}