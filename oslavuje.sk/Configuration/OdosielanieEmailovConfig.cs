using System.ComponentModel.DataAnnotations;

using Stubble.Core.Renderers.StringRenderer;

namespace oslavuje.sk.Configuration;

public class OdosielanieEmailovConfig
{
    public static string SectionName { get; } = "OdosielanieEmailov";

    [Required(ErrorMessage = "Nastavenia emailu sú povinné")]
    public EmailSettings EmailSettings { get; set; } = new();
}

public class EmailSettings
{
    public string From { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Bcc { get; set; } = string.Empty;
}