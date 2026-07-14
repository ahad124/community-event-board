using System.ComponentModel.DataAnnotations;

namespace EventBoard.Api.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "UserName is required")]
    [StringLength(100, MinimumLength = 2)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Role { get; set; } = "User";

    /// <summary>
    /// Whether the account is enabled. Disabled users cannot log in.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    public ICollection<EventBooking> Bookings { get; set; } = new List<EventBooking>();
    public ICollection<EventFavorite> Favorites { get; set; } = new List<EventFavorite>();
}
