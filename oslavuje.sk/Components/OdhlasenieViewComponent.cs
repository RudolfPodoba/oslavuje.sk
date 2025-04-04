using Microsoft.AspNetCore.Mvc;

using oslavuje.sk.Models.ViewModels;

namespace oslavuje.sk.Components;

[ViewComponent(Name = "Odhlasenie")]
public class OdhlasenieViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}