using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Dtos;
using ProductApi.Services;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")] // -> /api/products
[Produces("application/json")]
public class ProductsController(IProductService products) : ControllerBase
{
    // GET /api/products
    [HttpGet]
    [ProducesResponseType<List<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductResponse>>> GetAll(CancellationToken ct)
    {
        return Ok(await products.GetAllAsync(ct));
    }

    // GET /api/products/5
    [HttpGet("{id:int}", Name = "GetProductById")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    // POST /api/products
    [HttpPost]
    [Authorize]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var created = await products.CreateAsync(request, ct);
        // 201 + Location header pointing at the new resource
        return CreatedAtRoute("GetProductById", new { id = created.Id }, created);
    }

    // PUT /api/products/5
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(int id, UpdateProductRequest request, CancellationToken ct)
    {
        var updated = await products.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    // DELETE /api/products/5
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        return await products.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
