using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using FunctionApp.Models;
using FunctionApp.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FunctionApp.Functions
{
    public class ProductQueueFunctions
    {
        private readonly IProductService _productService;
        private readonly QueueClient _queueClient;
        private readonly ILogger<ProductQueueFunctions> _logger;

        public ProductQueueFunctions(IProductService productService, QueueClient queueClient, ILogger<ProductQueueFunctions> logger)
        {
            _productService = productService;
            _queueClient = queueClient;
            _logger = logger;
        }

        /// <summary>
        /// Queue Trigger - Processes messages from the "product-queue"
        /// This function automatically runs when a message is added to the queue
        /// </summary>
        [Function("ProcessProductQueue")]
        public async Task ProcessProductQueue(
            [QueueTrigger("product-queue", Connection = "AzureWebJobsStorage")] ProductQueueMessage message)
        {
            _logger.LogInformation("Processing queue message for product: {ProductId}, Action: {Action}",
                message.ProductId, message.Action);

            switch (message.Action.ToLower())
            {
                case "create":
                    var newProduct = new ProductEntity
                    {
                        Name = message.ProductName,
                        Price = message.Price,
                        Quantity = message.Quantity
                    };
                    var created = await _productService.CreateProductAsync(newProduct);
                    _logger.LogInformation("Product created via queue: {ProductId}", created.RowKey);
                    break;

                case "update":
                    var existingProduct = await _productService.GetProductAsync(message.ProductId);
                    if (existingProduct != null)
                    {
                        existingProduct.Name = message.ProductName;
                        existingProduct.Price = message.Price;
                        existingProduct.Quantity = message.Quantity;
                        await _productService.UpdateProductAsync(existingProduct);
                        _logger.LogInformation("Product updated via queue: {ProductId}", message.ProductId);
                    }
                    else
                    {
                        _logger.LogWarning("Product not found for update: {ProductId}", message.ProductId);
                    }
                    break;

                case "delete":
                    var deleted = await _productService.DeleteProductAsync(message.ProductId);
                    if (deleted)
                    {
                        _logger.LogInformation("Product deleted via queue: {ProductId}", message.ProductId);
                    }
                    else
                    {
                        _logger.LogWarning("Product not found for delete: {ProductId}", message.ProductId);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown action: {Action}", message.Action);
                    break;
            }
        }

        /// <summary>
        /// HTTP Trigger - Adds a message to the queue for async processing
        /// Use this to enqueue product operations instead of processing immediately
        /// </summary>
        [Function("EnqueueProductOperation")]
        public async Task<HttpResponseData> EnqueueProductOperation(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products/queue")] HttpRequestData req)
        {
            _logger.LogInformation("Enqueueing product operation");

            var message = await req.ReadFromJsonAsync<ProductQueueMessage>();
            if (message == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            // Serialize and send message to queue
            var messageJson = JsonSerializer.Serialize(message);
            await _queueClient.SendMessageAsync(messageJson);

            _logger.LogInformation("Message enqueued: {Action} for product {ProductId}", message.Action, message.ProductId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new { status = "Message enqueued for processing", action = message.Action });
            return response;
        }
    }
}
