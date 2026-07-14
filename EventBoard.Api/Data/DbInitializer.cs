using EventBoard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventBoard.Api.Data;

public static class DbInitializer
{
    public static void Seed(AppDbContext context)
    {
        // Apply the schema. Relational providers (SQL Server) run EF Core migrations;
        // non-relational providers used in tests (InMemory) don't support migrations,
        // so fall back to EnsureCreated().
        if (context.Database.IsRelational())
        {
            context.Database.Migrate();
        }
        else
        {
            context.Database.EnsureCreated();
        }

        // 1. Seed Categories
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(
                new Category { Name = "Conference" },
                new Category { Name = "Workshop" },
                new Category { Name = "Meetup" },
                new Category { Name = "Concert" },
                new Category { Name = "Webinar" }
            );
            context.SaveChanges();
        }

        // 2. Seed Users (with BCrypt-hashed passwords)
        if (!context.Users.Any())
        {
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                Email = "admin@eventboard.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role = "Admin"
            };

            var aliceUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "alice",
                Email = "alice@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Alice123!"),
                Role = "User"
            };

            var bobUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "bob",
                Email = "bob@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Bob123!"),
                Role = "User"
            };

            context.Users.AddRange(adminUser, aliceUser, bobUser);
            context.SaveChanges();
        }

        // 3. Seed Events
        if (!context.Events.Any())
        {
            var admin = context.Users.First(u => u.Role == "Admin");
            var conference = context.Categories.First(c => c.Name == "Conference");
            var workshop = context.Categories.First(c => c.Name == "Workshop");
            var meetup = context.Categories.First(c => c.Name == "Meetup");
            var concert = context.Categories.First(c => c.Name == "Concert");
            var webinar = context.Categories.First(c => c.Name == "Webinar");

            context.Events.AddRange(
                new Event
                {
                    Title = "Global Tech Summit 2026",
                    Description = "The premier developer and tech conference covering cloud, AI, and architecture.",
                    Date = DateTime.UtcNow.AddMonths(1),
                    Location = "San Francisco, CA",
                    CategoryId = conference.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "Intro to React & Vite Workshop",
                    Description = "A hands-on coding workshop to get started with modern React development using Vite.",
                    Date = DateTime.UtcNow.AddDays(15),
                    Location = "Austin, TX",
                    CategoryId = workshop.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "AI & Machine Learning meetup",
                    Description = "Casual networking and lightning talks about LLMs and agents.",
                    Date = DateTime.UtcNow.AddDays(7),
                    Location = "New York, NY",
                    CategoryId = meetup.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "Symphony of Lights Concert",
                    Description = "Enjoy an evening of classical music with a modern electronic twist.",
                    Date = DateTime.UtcNow.AddMonths(2),
                    Location = "Chicago, IL",
                    CategoryId = concert.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "Clean Architecture Webinar",
                    Description = "Online deep dive into domain-driven design, repositories, and Clean Architecture.",
                    Date = DateTime.UtcNow.AddDays(5),
                    Location = "Zoom / Online",
                    CategoryId = webinar.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "Startup Founders Meetup",
                    Description = "Connect with early-stage founders and venture capital professionals.",
                    Date = DateTime.UtcNow.AddDays(20),
                    Location = "Denver, CO",
                    CategoryId = meetup.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "Advanced ASP.NET Core Workshop",
                    Description = "Advanced concepts including middleware optimization, security, and scalability.",
                    Date = DateTime.UtcNow.AddMonths(1).AddDays(10),
                    Location = "Seattle, WA",
                    CategoryId = workshop.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "Cybersecurity Conference 2026",
                    Description = "Annual cybersecurity summit focusing on modern threats and defense strategies.",
                    Date = DateTime.UtcNow.AddMonths(3),
                    Location = "Boston, MA",
                    CategoryId = conference.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "Vocal Harmony Concert",
                    Description = "A relaxing live acoustic showcase featuring independent artists.",
                    Date = DateTime.UtcNow.AddMonths(2).AddDays(15),
                    Location = "Nashville, TN",
                    CategoryId = concert.Id,
                    OrganizerId = admin.Id
                },
                new Event
                {
                    Title = "NextJS vs Remix Webinar",
                    Description = "A friendly debate and detailed comparison of modern React framework paradigms.",
                    Date = DateTime.UtcNow.AddDays(12),
                    Location = "Webex / Online",
                    CategoryId = webinar.Id,
                    OrganizerId = admin.Id
                }
            );
            context.SaveChanges();
        }

        // 4. Seed Bookings & Favorites
        var bookingsExist = context.Bookings.Any();
        var favoritesExist = context.Favorites.Any();

        if (!bookingsExist || !favoritesExist)
        {
            var alice = context.Users.FirstOrDefault(u => u.UserName == "alice");
            var bob = context.Users.FirstOrDefault(u => u.UserName == "bob");
            var events = context.Events.ToList();

            if (alice != null && bob != null && events.Count >= 3)
            {
                if (!bookingsExist)
                {
                    context.Bookings.AddRange(
                        new EventBooking
                        {
                            EventId = events[0].Id,
                            UserId = alice.Id,
                            BookingDate = DateTime.UtcNow.AddDays(-2),
                            Status = BookingStatus.Yes
                        },
                        new EventBooking
                        {
                            EventId = events[1].Id,
                            UserId = alice.Id,
                            BookingDate = DateTime.UtcNow.AddDays(-1),
                            Status = BookingStatus.Maybe
                        },
                        new EventBooking
                        {
                            EventId = events[0].Id,
                            UserId = bob.Id,
                            BookingDate = DateTime.UtcNow.AddDays(-3),
                            Status = BookingStatus.No
                        },
                        new EventBooking
                        {
                            EventId = events[2].Id,
                            UserId = bob.Id,
                            BookingDate = DateTime.UtcNow,
                            Status = BookingStatus.Yes
                        }
                    );
                }

                if (!favoritesExist)
                {
                    context.Favorites.AddRange(
                        new EventFavorite
                        {
                            UserId = alice.Id,
                            EventId = events[0].Id,
                            AddedAt = DateTime.UtcNow.AddDays(-1)
                        },
                        new EventFavorite
                        {
                            UserId = alice.Id,
                            EventId = events[2].Id,
                            AddedAt = DateTime.UtcNow
                        },
                        new EventFavorite
                        {
                            UserId = bob.Id,
                            EventId = events[1].Id,
                            AddedAt = DateTime.UtcNow.AddDays(-2)
                        }
                    );
                }

                context.SaveChanges();
            }
        }
    }
}
