using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Umbraco.Cms.Core.Security;

namespace oslavuje.sk.Models.GiftRegistry;

public class SubdomainGiftRegistry
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string SubdomainName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public int OwnerId { get; set; } 

    [ForeignKey(nameof(OwnerId))]
    public virtual MemberIdentityUser? Owner { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Gift> Gifts { get; set; } = new List<Gift>();
}