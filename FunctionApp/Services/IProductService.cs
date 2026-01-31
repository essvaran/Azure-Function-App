using FunctionApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionApp.Services
{
    public interface IProductService
    {
        Task<ProductEntity> GetProductAsync(string id);
        Task<IEnumerable<ProductEntity>> GetAllProductsAsync();
        Task<ProductEntity> CreateProductAsync(ProductEntity product);
        Task<ProductEntity> UpdateProductAsync(ProductEntity product);
        Task<bool> DeleteProductAsync(string id);
    }
}
