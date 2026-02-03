using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using FunctionApp.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Functions
{
    public class OrderQueueFunctions
    {
        private readonly ILogger<OrderQueueFunctions> _logger;
        private readonly TableServiceClient _tableService;

        public OrderQueueFunctions(ILogger<OrderQueueFunctions> logger, TableServiceClient tableService)
        {
            _logger = logger;
            _tableService = tableService;
        }

        // HTTP Trigger - Customer places an order
        [Function("PlaceOrder")]
        public async Task<PlaceOrderResponse> PlaceOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            var order = await req.ReadFromJsonAsync<Order>();

            if (order == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                return new PlaceOrderResponse { HttpResponse = badResponse };
            }

            order.OrderId = Guid.NewGuid().ToString();
            order.Status = "Received";

            _logger.LogInformation("Order received: {OrderId}", order.OrderId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new { orderId = order.OrderId, status = "Order received" });

            return new PlaceOrderResponse
            {
                HttpResponse = response,
                QueueMessage = JsonSerializer.Serialize(order)
            };
        }

        // Queue Trigger - Process order and save to Table
        [Function("ProcessOrder")]
        public async Task ProcessOrder(
            [QueueTrigger("orders-queue", Connection = "AzureWebJobsStorage")] string orderJson)
        {
            var order = JsonSerializer.Deserialize<Order>(orderJson);

            if (order == null)
            {
                _logger.LogError("Failed to deserialize order");
                return;
            }

            _logger.LogInformation("Processing order: {OrderId}", order.OrderId);

            // Create table if not exists
            var tableClient = _tableService.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();

            // Convert to table entity
            var orderEntity = new OrderEntity
            {
                PartitionKey = "Orders",
                RowKey = order.OrderId,
                CustomerEmail = order.CustomerEmail,
                ProductName = order.ProductName,
                Quantity = order.Quantity,
                UnitPrice = (double)order.UnitPrice,
                TotalAmount = (double)order.TotalAmount,
                Status = "Completed"
            };

            // Save to table
            await tableClient.AddEntityAsync(orderEntity);

            _logger.LogInformation("Order {OrderId} saved to table!", order.OrderId);
        }

        // Get all orders from Table
        [Function("GetOrders")]
        public async Task<HttpResponseData> GetOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            var tableClient = _tableService.GetTableClient("Orders");

            var orders = new List<OrderEntity>();
            await foreach (var order in tableClient.QueryAsync<OrderEntity>())
            {
                orders.Add(order);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(orders);
            return response;
        }
    }

    public class PlaceOrderResponse
    {
        [HttpResult]
        public HttpResponseData HttpResponse { get; set; } = null!;

        [QueueOutput("orders-queue", Connection = "AzureWebJobsStorage")]
        public string? QueueMessage { get; set; }
    }
}
