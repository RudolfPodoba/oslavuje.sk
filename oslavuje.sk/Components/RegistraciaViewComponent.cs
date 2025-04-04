using System.Text.Json;

using Microsoft.AspNetCore.Mvc;

using oslavuje.sk.Models.ViewModels;

namespace oslavuje.sk.Components;

[ViewComponent(Name = "Registracia")]
public class RegistraciaViewComponent : ViewComponent
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public IViewComponentResult Invoke()
    {
        var model = new RegistraciaViewModel();

        // Načítanie modelu z TempData ak existuje
        if (TempData["RegistraciaModel"] is string jsonData && !string.IsNullOrEmpty(jsonData))
        {
            try
            {
                var deserializedModel = JsonSerializer.Deserialize<RegistraciaViewModel>(
                    jsonData,
                    JsonSerializerOptions);

                if (deserializedModel != null)
                {
                    model = deserializedModel;
                }
            }
            catch /*(JsonException ex)*/
            {
                // Môžete logovať chybu, ak je to potrebné
                // System.Diagnostics.Debug.WriteLine($"Chyba pri deserializácii (RegistraciaViewComponent): {ex.Message}");
            }
        }

        return View(model);
    }
}