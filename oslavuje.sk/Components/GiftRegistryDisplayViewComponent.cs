using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using oslavuje.sk.Models.GiftRegistry;
using oslavuje.sk.Repositories;
using Microsoft.Win32;
using oslavuje.sk.Models.ViewModels.GiftRegistry;
using static Lucene.Net.Documents.Field;

namespace oslavuje.sk.Components;

public class GiftRegistryDisplayViewComponent : ViewComponent
{
    private readonly GiftRegistryRepository _giftRegistryRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GiftRegistryDisplayViewComponent(
        GiftRegistryRepository giftRegistryRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _giftRegistryRepository = giftRegistryRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var viewModel = new GiftRegistryDisplayViewModel();
        int? registryId = null;

        // Pokúsime sa najprv získať registryId z query parametra (nový spôsob)
        if (_httpContextAccessor.HttpContext?.Request?.Query?.ContainsKey("registryId") == true)
        {
            if (int.TryParse(_httpContextAccessor.HttpContext.Request.Query["registryId"], out int id))
            {
                registryId = id;
            }
        }

        // Ak registryId nie je v query parametroch, skúsime to starou cestou z HttpContext.Items
        if (!registryId.HasValue && _httpContextAccessor.HttpContext?.Items.TryGetValue("CurrentGiftRegistry", out var registryIdObj) == true &&
            registryIdObj is int contextRegistryId)
        {
            registryId = contextRegistryId;
        }

        if (registryId.HasValue)
        {
            // Načítaj darčeky pre tento register
            var gifts = await _giftRegistryRepository.GetGiftsForRegistryAsync(registryId.Value);
            // Filtruj iba nerezerované darčeky
            viewModel.Gifts = gifts.Where(g => string.IsNullOrEmpty(g.ReservedByEmail));

            // Načítaj informácie o registri
            viewModel.Registry = await _giftRegistryRepository.GetRegistryByIdAsync(registryId.Value);

            // Pre lepšie sledovanie, uložíme ID do HttpContext.Items ak ešte nie je uložené
            if (!_httpContextAccessor.HttpContext.Items.ContainsKey("CurrentGiftRegistry"))
            {
                _httpContextAccessor.HttpContext.Items["CurrentGiftRegistry"] = registryId.Value;
            }
        }

        //----------------Potom to vráť, to je len na testovanie, či to funguje, alebo nie.----------------
        //int targetRegistryId = 11;
        //if (targetRegistryId > 0)
        //{
        //    var gifts = await _giftRegistryRepository.GetGiftsForRegistryAsync(targetRegistryId);
        //    viewModel.Gifts = gifts.Where(g => string.IsNullOrEmpty(g.ReservedByEmail));

        //    viewModel.Registry = await _giftRegistryRepository.GetRegistryByIdAsync(targetRegistryId);
        //}
        //else 
        //{
        //    // PRODUKCIA

        //    // Získa ID registra z HttpContext.Items, ktoré nastavil middleware.
        //    if (_httpContextAccessor.HttpContext?.Items.TryGetValue("CurrentGiftRegistry", out var registryIdObj) == true &&
        //        registryIdObj is int registryId)
        //    {
        //        // Načítaj darčeky pre tento register
        //        var gifts = await _giftRegistryRepository.GetGiftsForRegistryAsync(registryId);
        //        // Filtruj iba nerezerované darčeky
        //        viewModel.Gifts = gifts.Where(g => string.IsNullOrEmpty(g.ReservedByEmail));

        //        // Načítaj informácie o registri
        //        viewModel.Registry = await _giftRegistryRepository.GetRegistryByIdAsync(registryId);
        //    }
        //}        

        return View(viewModel);
    }
}