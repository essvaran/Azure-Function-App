using Azure;
using Azure.Data.Tables;

namespace FunctionApp.Models
{
    // For receiving order from API
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount => Quantity * UnitPrice;
        public string Status { get; set; } = "Pending";
    }

    // For saving to Table Storage
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Orders";
        public string RowKey { get; set; } = string.Empty;  // OrderId
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CustomerEmail { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }  // Table Storage doesn't support decimal
        public double TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
