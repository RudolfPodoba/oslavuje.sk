using System.ComponentModel.DataAnnotations;

namespace oslavuje.sk.Configuration;

public class SuhlasyConfig
{
    public static string SectionName { get; } = "Suhlasy";

    [Required(ErrorMessage = "Nastavenia súhlasov sú povinné")]
    public Suhlasy Verzie { get; set; } = new();
}

public class Suhlasy
{
    public string Suhlas_s_GDPR { get; set; } = string.Empty;
    public string Suhlas_s_podmienkami_vyuzivania { get; set; } = string.Empty;
}