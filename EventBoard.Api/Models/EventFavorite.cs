using System.ComponentModel.DataAnnotations.Schema;

namespace EventBoard.Api.Models;

public class EventFavorite
{
    public Guid UserId { get; set; }

    public int EventId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("EventId")]
    public Event? Event { get; set; }
}
