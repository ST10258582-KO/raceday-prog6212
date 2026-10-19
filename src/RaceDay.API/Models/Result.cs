using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Result
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ResultID { get; set; }

    [Required]
    public int EnrolmentID { get; set; }

    public TimeOnly? FinishTime { get; set; }

    public int? Position { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "DNS";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EnrolmentID))]
    public Enrolment Enrolment { get; set; } = null!;
}
