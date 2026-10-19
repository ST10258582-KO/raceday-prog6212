using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayContext(DbContextOptions<RaceDayContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventRoute> EventRoutes => Set<EventRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role)
                .HasConversion<string>();
            e.HasCheckConstraint("CK_Users_Role", "Role IN ('Organiser', 'Participant')");
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.HasCheckConstraint("CK_Events_Status",
                "Status IN ('Upcoming', 'Open', 'Closed', 'Completed', 'Cancelled')");
            e.HasOne(ev => ev.Organiser)
                .WithMany(u => u.Events)
                .HasForeignKey(ev => ev.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Distance).HasPrecision(6, 2);
            e.Property(c => c.EntryFee).HasPrecision(10, 2);
            e.HasCheckConstraint("CK_Categories_Unit", "DistanceUnit IN ('km', 'm')");
            e.HasCheckConstraint("CK_Categories_Type", "EventType IN ('Running', 'Walking', 'Cycling')");
            e.HasCheckConstraint("CK_Categories_Distance", "Distance > 0");
            e.HasCheckConstraint("CK_Categories_Fee", "EntryFee >= 0");
            e.HasOne(c => c.Event)
                .WithMany(ev => ev.Categories)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventRoute>(e =>
        {
            e.Property(r => r.ElevationGain).HasPrecision(8, 2);
            e.HasIndex(r => r.EventID).IsUnique();
            e.HasOne(r => r.Event)
                .WithOne(ev => ev.Route)
                .HasForeignKey<EventRoute>(r => r.EventID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.HasIndex(en => new { en.ParticipantID, en.CategoryID }).IsUnique();
            e.HasCheckConstraint("CK_Enrolments_PayStatus",
                "PaymentStatus IN ('Pending', 'Paid', 'Refunded')");
            e.HasOne(en => en.Participant)
                .WithMany(u => u.Enrolments)
                .HasForeignKey(en => en.ParticipantID)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(en => en.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(en => en.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.HasIndex(r => r.EnrolmentID).IsUnique();
            e.HasCheckConstraint("CK_Results_Status",
                "Status IN ('Finished', 'DNF', 'DNS', 'DQ')");
            e.HasCheckConstraint("CK_Results_Position", "Position IS NULL OR Position > 0");
            e.HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
