using System.ComponentModel.DataAnnotations;

namespace oslavuje.sk.Models.ViewModels;

public class MemberDataViewModel
{
    [Required(ErrorMessage = "Prosím, zadajte údaje")]
    [Display(Name = "Vaše údaje")]
    public string UserInput { get; set; } = string.Empty;

    public List<MemberData> PreviousData { get; set; } = new List<MemberData>();
}