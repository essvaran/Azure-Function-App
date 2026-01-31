namespace FunctionApp.Models
{
    public class ProductQueueMessage
    {
        public string ProductId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "create", "update", "delete"
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
