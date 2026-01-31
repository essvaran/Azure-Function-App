using FunctionApp.Models;
using FunctionApp.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionApp.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<ProductEntity> GetProductAsync(string id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<ProductEntity>> GetAllProductsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<ProductEntity> CreateProductAsync(ProductEntity product)
        {
            return await _repository.CreateAsync(product);
        }

        public async Task<ProductEntity> UpdateProductAsync(ProductEntity product)
        {
            return await _repository.UpdateAsync(product);
        }

        public async Task<bool> DeleteProductAsync(string id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
