using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Models;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Mechanical Keyboard", Price = 89.99m },
        new Product { Id = 2, Name = "USB-C Dock", Price = 49.50m }
    };
    private static int _nextId = 3;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetAll()
    {
        return Ok(_products);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult Create([FromBody] Product product)
    {
        product.Id = _nextId++;
        _products.Add(product);
        return CreatedAtAction(nameof(GetAll), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Update(int id, [FromBody] Product updated)
    {
        var existing = _products.FirstOrDefault(p => p.Id == id);
        if (existing is null) return NotFound();

        existing.Name = updated.Name;
        existing.Price = updated.Price;
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id)
    {
        var existing = _products.FirstOrDefault(p => p.Id == id);
        if (existing is null) return NotFound();

        _products.Remove(existing);
        return NoContent(); // Returns 204
    }
}