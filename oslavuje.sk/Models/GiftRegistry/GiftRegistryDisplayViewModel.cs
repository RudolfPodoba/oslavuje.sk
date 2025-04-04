using oslavuje.sk.Models.GiftRegistry;

namespace oslavuje.sk.Models.ViewModels.GiftRegistry;

public class GiftRegistryDisplayViewModel
{
    public SubdomainGiftRegistry? Registry { get; set; }
    public IEnumerable<Gift> Gifts { get; set; } = Enumerable.Empty<Gift>();
}