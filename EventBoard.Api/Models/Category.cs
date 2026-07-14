using System.ComponentModel.DataAnnotations;

namespace EventBoard.Api.Models;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
