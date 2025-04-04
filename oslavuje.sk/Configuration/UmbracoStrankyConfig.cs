using System.ComponentModel.DataAnnotations;

namespace oslavuje.sk.Configuration;

public class UmbracoStrankyConfig
{
    public static string SectionName { get; } = "UmbracoStranky";

    public StrankyKontaktnehoFormulara KontaktnyFormular { get; set; } = new();
    public StrankyClenskejSekcie ClenskaSekcia { get; set; } = new();

    public class StrankyKontaktnehoFormulara
    {
        public Guid Kontakt { get; set; }
        public Guid Kontakt_odoslany { get; set; }
        public Guid Kontakt_zlyhal { get; set; }
    }

    public class StrankyClenskejSekcie
    {
        public Guid Registracia { get; set; }
        public Guid Registracia_podakovanie { get; set; }
        public Guid Registracia_dokoncena { get; set; }
        public Guid Registracia_neuspesna { get; set; }
        public Guid Prihlasenie { get; set; }
        public Guid Clenska_sekcia { get; set; }
        public Guid Clenska_sekcia_nedostupna { get; set; }
        public Guid Clenska_sekcia_odhlasenie_uspesne { get; set; }
        public Guid Zoznam_darcekov { get; set; }
    }
}