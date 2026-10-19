using RaceDay.API.Models;

namespace RaceDay.API.Data;

/// <summary>
/// Seeds the database with default seed data for development and demonstration purposes.
/// Only runs if the Users table is empty to avoid duplicate inserts.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(RaceDayContext db)
    {
        if (await db.Database.EnsureCreatedAsync() && !db.Users.Any())
        {
            var organiser = new User
            {
                FullName = "Thabo Nkosi",
                Email = "thabo.nkosi@raceday.co.za",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Organiser123!"),
                Role = "Organiser",
                PhoneNumber = "0821234567",
                DateOfBirth = new DateOnly(1985, 3, 15)
            };

            var participant = new User
            {
                FullName = "Lerato Dlamini",
                Email = "lerato.dlamini@gmail.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Participant123!"),
                Role = "Participant",
                PhoneNumber = "0833456789",
                DateOfBirth = new DateOnly(1995, 11, 8)
            };

            db.Users.AddRange(organiser, participant);
            await db.SaveChangesAsync();
        }
    }
}
