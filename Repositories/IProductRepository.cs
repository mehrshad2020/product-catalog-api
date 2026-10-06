using Api.Models;

namespace Api.Repositories;

public interface IProductRepository
{
    Task<Product> CreateAsync(Product product);
    Task<List<Product>> GetAllAsync();
    Task<bool> DeleteAsync(int id);

    Task<Product?> GetByIdAsync(int id);

    Task<Product?> UpdateAsync(Product product);
   Task<List<Product>> SearchAsync(
    string title,
    int page,
    int pageSize);
}