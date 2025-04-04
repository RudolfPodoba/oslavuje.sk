using System.ComponentModel.DataAnnotations;

using oslavuje.sk.Validation;

namespace oslavuje.sk.Models.ViewModels;

public class ContactViewModel
{
    [Display(Name = "Meno a priezvisko")]
    [Required(ErrorMessage = "Meno a priezvisko musí byť zdané")]
    public string? Name { get; set; }

    [Display(Name = "E-mailová adresa")]
    [EmailAddress(ErrorMessage = "Musíte zadať platnú e-mailovú adresu")]
    [Required(ErrorMessage = "E-mailová adresa musí byť zadaná")]
    public string? Email { get; set; }

    [Display(Name = "Telefónne číslo")]
    public string? Phone { get; set; }

    [Display(Name = "Správa")]
    [Required(ErrorMessage = "Správa musí byť zadaná")]
    public string? Message { get; set; }

    [Display(Name = "Áno, dávam súhlas na ukladanie a spracovanie mojich údajov")]
    [Required(ErrorMessage = "Pred odoslaním správy nám musíte udeliť súhlas s uložením vašich údajov.")]
    [MustBeTrue(ErrorMessage = "Pred odoslaním správy nám musíte udeliť súhlas s uložením vašich údajov.")]
    public bool Consent { get; set; }

    [Display(Name = "Heslo")]
    public string? MojeHeslo { get; set; }

    [Display(Name = "Potvrdenie hesla")]
    public string? PotvrdMojeHeslo { get; set; }
}