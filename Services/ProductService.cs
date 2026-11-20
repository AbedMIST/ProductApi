using Microsoft.EntityFrameworkCore;
using ProductApi.Data;
using ProductApi.Dtos;
using ProductApi.Models;

namespace ProductApi.Services;

// Business/data logic lives here so the controller only deals with HTTP.
public class ProductService(AppDbContext db) : IProductService
{
    public async Task<List<ProductResponse>> GetAllAsync(CancellationToken ct)
    {
        return await db.Products
            .OrderBy(p => p.Id)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        return product is null ? null : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            IsFavorite = request.IsFavorite ?? false,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct); // Id is generated here
        return ToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return null;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.IsFavorite = request.IsFavorite ?? product.IsFavorite;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return false;

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static ProductResponse ToResponse(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsFavorite, p.CreatedAt, p.UpdatedAt);
}
