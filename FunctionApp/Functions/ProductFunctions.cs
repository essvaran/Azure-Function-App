using System.Net;
using System.Threading.Tasks;
using FunctionApp.Models;
using FunctionApp.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Functions
{
    public class ProductFunctions
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductFunctions> _logger;

        public ProductFunctions(IProductService productService, ILogger<ProductFunctions> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [Function("GetAllProducts")]
        public async Task<HttpResponseData> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all products");
            var products = await _productService.GetAllProductsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(products);
            return response;
        }

        [Function("GetProduct")]
        public async Task<HttpResponseData> GetById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("Getting product {Id}", id);
            var product = await _productService.GetProductAsync(id);

            if (product == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(product);
            return response;
        }

        [Function("CreateProduct")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("Creating new product");

            var product = await req.ReadFromJsonAsync<ProductEntity>();
            if (product == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var created = await _productService.CreateProductAsync(product);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(created);
            return response;
        }

        [Function("UpdateProduct")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("Updating product {Id}", id);

            var product = await req.ReadFromJsonAsync<ProductEntity>();
            if (product == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            product.RowKey = id;

            try
            {
                var updated = await _productService.UpdateProductAsync(product);

                if (updated == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(updated);
                return response;
            }
            catch (System.InvalidOperationException)
            {
                var response = req.CreateResponse(HttpStatusCode.Conflict);
                await response.WriteAsJsonAsync(new { error = "The product was modified by another request. Please refresh and try again." });
                return response;
            }
        }

        [Function("DeleteProduct")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation("Deleting product {Id}", id);

            var deleted = await _productService.DeleteProductAsync(id);

            if (!deleted)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
