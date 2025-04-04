using Microsoft.AspNetCore.Mvc;

using oslavuje.sk.Models.ViewModels.GiftRegistry;
using oslavuje.sk.Repositories;

using Umbraco.Cms.Core.Security;

namespace oslavuje.sk.Components;

public class GiftRegistryManagerViewComponent : ViewComponent
{
    private readonly IMemberManager _memberManager;
    private readonly GiftRegistryRepository _giftRegistryRepository;

    public GiftRegistryManagerViewComponent(
        IMemberManager memberManager,
        GiftRegistryRepository giftRegistryRepository)
    {
        _memberManager = memberManager;
        _giftRegistryRepository = giftRegistryRepository;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Základný model s prázdnym darčekom
        var model = new GiftRegistryViewModel
        {
            Gifts = new List<GiftItemViewModel> { new GiftItemViewModel() }
        };

        // Skontrolujeme, či je používateľ prihlásený
        var member = await _memberManager.GetCurrentMemberAsync();
        if (member != null)
        {
            // Ak je prihlásený, skúsime nájsť jeho existujúci register
            var existingRegistry = await _giftRegistryRepository.GetRegistryByOwnerIdAsync(member.Id);

            // Ak register existuje, naplníme model údajmi
            if (existingRegistry != null)
            {
                model.RegistryId = existingRegistry.Id;
                model.SubdomainName = existingRegistry.SubdomainName;
                model.Title = existingRegistry.Title;
                model.Description = existingRegistry.Description;
                model.ExpiryDate = existingRegistry.ExpiryDate;
                model.IsExistingRegistry = true;

                // Načítame existujúce darčeky
                var gifts = await _giftRegistryRepository.GetGiftsForRegistryAsync(existingRegistry.Id);
                model.Gifts = gifts.Select(g => new GiftItemViewModel
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Price = g.Price,
                    ImageUrl = g.ImageUrl,
                    IsReserved = !string.IsNullOrEmpty(g.ReservedByEmail),
                    ReservedByName = g.ReservedByName,
                    ReservedByEmail = g.ReservedByEmail
                }).ToList();

                // Pridáme jeden prázdny darček pre pridávanie
                model.NewGift = new GiftItemViewModel();
            }
        }

        return View(model);
    }
}