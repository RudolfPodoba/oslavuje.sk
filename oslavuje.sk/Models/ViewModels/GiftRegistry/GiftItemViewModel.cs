using System.ComponentModel.DataAnnotations;

using DataType = System.ComponentModel.DataAnnotations.DataType;

namespace oslavuje.sk.Models.ViewModels.GiftRegistry;

public class GiftItemViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Zadajte názov darčeka")]
    [Display(Name = "Názov")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Popis")]
    public string? Description { get; set; }

    [Display(Name = "Cena")]
    [DataType(DataType.Currency)]
    public decimal? Price { get; set; }

    [Display(Name = "URL odkaz")]
    [DataType(DataType.Url)]
    public string? ImageUrl { get; set; }

    [Display(Name = "Rezervované")]
    public bool IsReserved { get; set; }

    [Display(Name = "Rezervované kým")]
    public string? ReservedByName { get; set; }

    [Display(Name = "Email rezervujúceho")]
    public string? ReservedByEmail { get; set; }
}