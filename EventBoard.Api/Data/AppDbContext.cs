using EventBoard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventBoard.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<EventBooking> Bookings { get; set; } = null!;
    public DbSet<EventFavorite> Favorites { get; set; } = null!;

    // Keyless projection for the raw-SQL events report (no backing table).
    public DbSet<EventReportRow> EventReport { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.IsActive).HasDefaultValue(true);
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // Configure Event entity
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.CategoryId);

            // One-to-many relationship: User (Organizer) to Event
            entity.HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship: Category to Event
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent category deletion if events exist
        });

        // Configure EventBooking entity
        modelBuilder.Entity<EventBooking>(entity =>
        {
            entity.HasKey(eb => eb.Id);
            entity.HasIndex(eb => eb.EventId);
            entity.HasIndex(eb => eb.UserId);

            // Save enum as string
            entity.Property(eb => eb.Status)
                .HasConversion<string>();

            // Relationships
            entity.HasOne(eb => eb.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(eb => eb.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // NoAction on the direct User→Booking path avoids a "multiple cascade
            // paths" error on SQL Server (bookings are already removed when the
            // event is deleted, which itself cascades from the organizer).
            entity.HasOne(eb => eb.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(eb => eb.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Configure EventFavorite (Composite Primary Key / Join Table)
        modelBuilder.Entity<EventFavorite>(entity =>
        {
            entity.HasKey(ef => new { ef.UserId, ef.EventId });
            entity.HasIndex(ef => ef.UserId);
            entity.HasIndex(ef => ef.EventId);

            // Relationships
            // NoAction on the direct User→Favorite path avoids a "multiple cascade
            // paths" error on SQL Server (favorites cascade from the event side).
            entity.HasOne(ef => ef.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(ef => ef.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(ef => ef.Event)
                .WithMany(e => e.Favorites)
                .HasForeignKey(ef => ef.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Keyless entity: materialized only from raw SQL, never mapped to a table.
        // ToView(null) keeps EnsureCreated/migrations from generating a table for it.
        modelBuilder.Entity<EventReportRow>(entity =>
        {
            entity.HasNoKey();
            entity.ToView(null);
        });
    }
}
