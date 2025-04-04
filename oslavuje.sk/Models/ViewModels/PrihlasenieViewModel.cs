using System.ComponentModel.DataAnnotations;

using DataType = System.ComponentModel.DataAnnotations.DataType;

namespace oslavuje.sk.Models.ViewModels;

public class PrihlasenieViewModel
{
    [Required(ErrorMessage = "Zadajte email")]
    [EmailAddress(ErrorMessage = "Neplatný formát emailu")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zadajte heslo")]
    [Display(Name = "Heslo")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Zapamätať si ma")]
    public bool RememberMe { get; set; }
}