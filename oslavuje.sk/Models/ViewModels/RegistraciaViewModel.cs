using System.ComponentModel.DataAnnotations;

using oslavuje.sk.Validation;

using DataType = System.ComponentModel.DataAnnotations.DataType;

namespace oslavuje.sk.Models.ViewModels;

public class RegistraciaViewModel
{
    [Required(ErrorMessage = "Zadajte email")]
    [EmailAddress(ErrorMessage = "Neplatný formát emailu")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zadajte svoje meno")]
    [Display(Name = "Meno")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zadajte heslo")]
    [StringLength(100, ErrorMessage = "Heslo musí mať aspoň {2} znakov", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Heslo")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potvrďte heslo")]
    [DataType(DataType.Password)]
    [Display(Name = "Potvrďte heslo")]
    [Compare(nameof(Password), ErrorMessage = "Heslá sa nezhodujú")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Áno, dávam súhlas na ukladanie a spracovanie mojich údajov")]
    [Required(ErrorMessage = "Pred odoslaním správy nám musíte udeliť súhlas s uložením vašich údajov.")]
    [MustBeTrue(ErrorMessage = "Pred odoslaním správy nám musíte udeliť súhlas s uložením vašich údajov.")]
    public bool Consent { get; set; }

    [Display(Name = "Áno, dávam súhlas s podmienkami využívania služieb serveru oslavuje.sk.")]
    [Required(ErrorMessage = "Pred odoslaním správy nám musíte udeliť súhlas s podmienkami využívania služieb serveru oslavuje.sk.")]
    [MustBeTrue(ErrorMessage = "Pred odoslaním správy nám musíte udeliť súhlas s podmienkami využívania služieb serveru oslavuje.sk.")]
    public bool ConsentServices { get; set; }

    [Display(Name = "Heslo")]
    public string? MojeHeslo { get; set; }

    [Display(Name = "Potvrdenie hesla")]
    public string? PotvrdMojeHeslo { get; set; }
}