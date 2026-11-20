namespace ProductApi.Models;

// Domain entity: what is stored in the database. Never returned directly from the API.
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsFavorite { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
