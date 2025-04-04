using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oslavuje.sk.Models.GiftRegistry;

public class Gift
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal? Price { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    public int RegistryId { get; set; }

    [ForeignKey(nameof(RegistryId))]
    public virtual SubdomainGiftRegistry? Registry { get; set; }

    public string? ReservedByName { get; set; }

    public string? ReservedByEmail { get; set; }

    public DateTime? ReservationDate { get; set; }

    public bool IsConfirmed { get; set; } = false;

    public string? ConfirmationToken { get; set; }
}