using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBoard.Api.Models;

public class Event
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; }

    [StringLength(300)]
    public string? Location { get; set; }

    // Relative URL of the uploaded event image, e.g. "/uploads/{guid}.jpg"
    [StringLength(400)]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "CategoryId is required")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "OrganizerId is required")]
    public Guid OrganizerId { get; set; }

    // Navigation properties
    [ForeignKey("CategoryId")]
    public Category? Category { get; set; }

    [ForeignKey("OrganizerId")]
    public User? Organizer { get; set; }

    public ICollection<EventBooking> Bookings { get; set; } = new List<EventBooking>();
    public ICollection<EventFavorite> Favorites { get; set; } = new List<EventFavorite>();
}
