using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CategoryID { get; set; }

    [Required]
    public int EventID { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal Distance { get; set; }

    [MaxLength(10)]
    public string DistanceUnit { get; set; } = "km";

    public decimal EntryFee { get; set; } = 0m;

    public int? MaxParticipants { get; set; }

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventID))]
    public Event Event { get; set; } = null!;

    public ICollection<Enrolment> Enrolments { get; set; } = [];
}
