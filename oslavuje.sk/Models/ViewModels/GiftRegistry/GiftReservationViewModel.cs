using oslavuje.sk.Validation;
using System.ComponentModel.DataAnnotations;

namespace oslavuje.sk.Models.ViewModels.GiftRegistry;

public class GiftReservationViewModel
{
    public int GiftId { get; set; }
    public string? GiftName { get; set; }
    public string? GiftDescription { get; set; }
    public string? RegistryTitle { get; set; }
    public string? RegistryDescription { get; set; }
    public int RegistryId { get; set; }

    [Required(ErrorMessage = "Zadajte svoje meno")]
    [Display(Name = "Meno")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zadajte svoj email")]
    [EmailAddress(ErrorMessage = "Neplatný formát emailu")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Áno, dávam súhlas na ukladanie a spracovanie mojich údajov")]
    [Required(ErrorMessage = "Pred rezerváciou darčeka nám musíte udeliť súhlas s uložením vašich údajov.")]
    [MustBeTrue(ErrorMessage = "Pred rezerváciou darčeka nám musíte udeliť súhlas s uložením vašich údajov.")]
    public bool Consent { get; set; }

    [Display(Name = "Heslo")]
    public string? MojeHeslo { get; set; }

    [Display(Name = "Potvrdenie hesla")]
    public string? PotvrdMojeHeslo { get; set; }
}