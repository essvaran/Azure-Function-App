using FunctionApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionApp.Repositories
{
    public interface IProductRepository
    {
        Task<ProductEntity> GetByIdAsync(string id);
        Task<IEnumerable<ProductEntity>> GetAllAsync();
        Task<ProductEntity> CreateAsync(ProductEntity product);
        Task<ProductEntity> UpdateAsync(ProductEntity product);
        Task<bool> DeleteAsync(string id);
    }
}
