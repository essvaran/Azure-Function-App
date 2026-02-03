using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Functions
{
    public class BlobFunctions
    {
        private readonly ILogger<BlobFunctions> _logger;

        public BlobFunctions(ILogger<BlobFunctions> logger)
        {
            _logger = logger;
        }

        // Blob Trigger - Runs when a file is uploaded to "uploads" container
        [Function("ProcessBlob")]
        public void ProcessBlob(
            [BlobTrigger("uploads/{name}", Connection = "AzureWebJobsStorage")] string content,
            string name)
        {
            _logger.LogInformation("=== BLOB UPLOADED ===");
            _logger.LogInformation("File name: {Name}", name);
            _logger.LogInformation("File size: {Size} bytes", content.Length);
            _logger.LogInformation("Content preview: {Preview}",
                content.Length > 100 ? content.Substring(0, 100) + "..." : content);
        }
    }
}
