using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;

namespace Api.Repositories;

public class ProductsRepository : IProductRepository
{
    private readonly AppDbContext _context;
    public ProductsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        return product;
    }
    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync();
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product is null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product?> UpdateAsync(Product product)
    {
        var existingProduct = await _context.Products.FindAsync(product.Id);

        if (existingProduct is null)
            return null;

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existingProduct;
    }

public async Task<List<Product>> SearchAsync(
    string title,
    int page,
    int pageSize)
{
    title = title.ToLower();

    return await _context.Products
        .AsNoTracking()
        .Where(p => p.Name.ToLower().Contains(title))
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}
}