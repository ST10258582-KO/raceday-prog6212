using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Event
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EventID { get; set; }

    [Required]
    public int OrganiserID { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateOnly EventDate { get; set; }

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Province { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Status { get; set; } = "Upcoming";

    [MaxLength(500)]
    public string? ImageURL { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(OrganiserID))]
    public User Organiser { get; set; } = null!;

    public ICollection<Category> Categories { get; set; } = [];
    public EventRoute? Route { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = [];
}
