using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class EventRoute
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RouteID { get; set; }

    [Required]
    public int EventID { get; set; }

    public string? RouteDescription { get; set; }

    [MaxLength(500)]
    public string? MapURL { get; set; }

    public string? GPXData { get; set; }

    public decimal? ElevationGain { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventID))]
    public Event Event { get; set; } = null!;
}
