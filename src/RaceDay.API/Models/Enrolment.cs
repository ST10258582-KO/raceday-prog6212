using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models;

public class Enrolment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EnrolmentID { get; set; }

    [Required]
    public int ParticipantID { get; set; }

    [Required]
    public int CategoryID { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [MaxLength(20)]
    public string? BibNumber { get; set; }

    [ForeignKey(nameof(ParticipantID))]
    public User Participant { get; set; } = null!;

    [ForeignKey(nameof(CategoryID))]
    public Category Category { get; set; } = null!;

    public Result? Result { get; set; }
}
