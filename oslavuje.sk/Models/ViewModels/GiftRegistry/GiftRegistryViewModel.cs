using System.ComponentModel.DataAnnotations;
using DataType = System.ComponentModel.DataAnnotations.DataType;

using oslavuje.sk.Validation;

namespace oslavuje.sk.Models.ViewModels.GiftRegistry;

public class GiftRegistryViewModel
{
    [Required(ErrorMessage = "Zadajte názov subdomény")]
    [Display(Name = "Subdoména")]
    [RegularExpression(@"^[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?$",
        ErrorMessage = "Subdoména môže obsahovať len malé písmená, číslice a pomlčky, a nemôže začínať ani končiť pomlčkou")]
    public string SubdomainName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zadajte názov oslavy")]
    [Display(Name = "Názov oslavy")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Popis")]
    public string? Description { get; set; }

    [Display(Name = "Platnosť do")]
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [Display(Name = "Zoznam darčekov")]
    public List<GiftItemViewModel> Gifts { get; set; } = new List<GiftItemViewModel>();

    public int? RegistryId { get; set; }
    public bool IsExistingRegistry { get; set; }
    public GiftItemViewModel? NewGift { get; set; } //= new GiftItemViewModel();
}