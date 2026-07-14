using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventBoard.Api.Models;

public class EventBooking
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EventId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Yes;

    // Navigation properties
    [ForeignKey("EventId")]
    public Event? Event { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }
}
