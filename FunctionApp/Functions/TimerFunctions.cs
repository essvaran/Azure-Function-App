using Azure.Data.Tables;
using FunctionApp.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Functions
{
    public class TimerFunctions
    {
        private readonly ILogger<TimerFunctions> _logger;
        private readonly TableServiceClient _tableService;

        public TimerFunctions(ILogger<TimerFunctions> logger, TableServiceClient tableService)
        {
            _logger = logger;
            _tableService = tableService;
        }

        // Timer Trigger - Runs every 5 minutes
        // CRON: "0 */5 * * * *" = every 5 minutes
        [Function("OrderReportTimer")]
        public async Task OrderReportTimer(
            [TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timerInfo)
        {
            _logger.LogInformation("=== ORDER REPORT ===");
            _logger.LogInformation("Timer triggered at: {Time}", DateTime.Now);

            // Get orders from table
            var tableClient = _tableService.GetTableClient("Orders");

            try
            {
                var orderCount = 0;
                double totalSales = 0;

                await foreach (var order in tableClient.QueryAsync<OrderEntity>())
                {
                    orderCount++;
                    totalSales += order.TotalAmount;
                }

                _logger.LogInformation("Total Orders: {Count}", orderCount);
                _logger.LogInformation("Total Sales: ${Sales}", totalSales);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("No orders table yet: {Error}", ex.Message);
            }

            _logger.LogInformation("Next run: {NextRun}", timerInfo.ScheduleStatus?.Next);
        }
    }
}
