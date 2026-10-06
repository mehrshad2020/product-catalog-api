using Api.Models;
using Api.Models.Products;
using Api.Repositories;

namespace Api.Services;

using Api.Services.Cache;
using Microsoft.Extensions.Caching.Memory;
public class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cache;

    public ProductService(
     IProductRepository productRepository,
     ICacheService cache)
    {
        _productRepository = productRepository;
        _cache = cache;
    }
    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.name,
            Description = request.Description,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _productRepository.CreateAsync(product);

        _cache.IncrementVersion("products");

        return result;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await _productRepository.GetAllAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result =  await _productRepository.DeleteAsync(id);
        _cache.IncrementVersion("products");
        return result;
    }
 
    public async Task<Product?> UpdateAsync(
        int id,
        UpdateProductRequest request)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
            return null;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.UpdatedAt = DateTime.UtcNow;

        var result = await _productRepository.UpdateAsync(product);
        _cache.IncrementVersion("products");
        return result;
    }
    public async Task<List<Product>> SearchAsync(
     string title,
     int page,
     int pageSize)
    {
        var version = _cache.GetVersion("products");

        var cacheKey =
            $"products:v{version}:search:{title.ToLower()}:{page}:{pageSize}";

        var cached = _cache.Get<List<Product>>(cacheKey);

        if (cached is not null)
            return cached;

        var products = await _productRepository.SearchAsync(
            title,
            page,
            pageSize);

        _cache.Set(
            cacheKey,
            products,
            TimeSpan.FromMinutes(5));

        return products;
    }
}