using System.ComponentModel.DataAnnotations;

namespace ProductApi.Dtos;

// What the client receives.
public record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    bool IsFavorite,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

// What the client sends on POST. [ApiController] validates these automatically and returns 400.
public class CreateProductRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    // Optional. Omitted/null: false on create, unchanged on update.
    public bool? IsFavorite { get; set; }
}

// Full replacement (PUT) uses the same rules as create.
public class UpdateProductRequest : CreateProductRequest;
