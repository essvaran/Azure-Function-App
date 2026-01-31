using Azure;
using Azure.Data.Tables;
using FunctionApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionApp.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly TableClient _tableClient;
        private const string PartitionKey = "Products";

        public ProductRepository(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient("Products");
            _tableClient.CreateIfNotExists();
        }

        public async Task<ProductEntity> GetByIdAsync(string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<ProductEntity>(PartitionKey, id);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<IEnumerable<ProductEntity>> GetAllAsync()
        {
            var products = new List<ProductEntity>();
            await foreach (var product in _tableClient.QueryAsync<ProductEntity>(p => p.PartitionKey == PartitionKey))
            {
                products.Add(product);
            }
            return products;
        }

        public async Task<ProductEntity> CreateAsync(ProductEntity product)
        {
            product.RowKey = Guid.NewGuid().ToString();
            product.PartitionKey = PartitionKey;
            await _tableClient.AddEntityAsync(product);
            return product;
        }

        public async Task<ProductEntity> UpdateAsync(ProductEntity product)
        {
            try
            {
                product.PartitionKey = PartitionKey;

                // Fetch existing entity to get current ETag for concurrency control
                var existing = await _tableClient.GetEntityAsync<ProductEntity>(PartitionKey, product.RowKey);

                await _tableClient.UpdateEntityAsync(product, existing.Value.ETag, TableUpdateMode.Replace);
                return product;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // Concurrency conflict - entity was modified by another request
                throw new InvalidOperationException("The product was modified by another request. Please refresh and try again.", ex);
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(PartitionKey, id);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }
    }
}
