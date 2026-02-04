using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Functions
{
    public class DurableFunctions
    {
        private readonly ILogger<DurableFunctions> _logger;

        public DurableFunctions(ILogger<DurableFunctions> logger)
        {
            _logger = logger;
        }

        // ============================================
        // 1. STARTER FUNCTION (Entry Point)
        // ============================================
        // POST /api/start-workflow?name=John
        [Function("StartWorkflow")]
        public async Task<HttpResponseData> StartWorkflow(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "start-workflow")] HttpRequestData req,
            [DurableClient] DurableTaskClient client)
        {
            // Get name from query string
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var name = query["name"] ?? "World";

            // Start the orchestrator
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("OrderWorkflow", name);

            _logger.LogInformation("Started workflow with ID: {InstanceId}", instanceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                instanceId = instanceId,
                status = "Workflow started"
            });
            return response;
        }

        // ============================================
        // 2. ORCHESTRATOR FUNCTION (Coordinator)
        // ============================================
        // Coordinates the workflow - calls activities in sequence
        [Function("OrderWorkflow")]
        public async Task<string> OrderWorkflow(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var name = context.GetInput<string>() ?? "Customer";

            // Step 1: Validate order
            var validation = await context.CallActivityAsync<string>("ValidateOrder", name);

            // Step 2: Process payment
            var payment = await context.CallActivityAsync<string>("ProcessPayment", name);

            // Step 3: Send confirmation
            var confirmation = await context.CallActivityAsync<string>("SendConfirmation", name);

            return $"Workflow completed for {name}: {validation} -> {payment} -> {confirmation}";
        }

        // ============================================
        // 3. ACTIVITY FUNCTIONS (Do the work)
        // ============================================
        [Function("ValidateOrder")]
        public string ValidateOrder([ActivityTrigger] string name)
        {
            _logger.LogInformation("Validating order for: {Name}", name);
            Thread.Sleep(1000); // Simulate work
            return "Validated";
        }

        [Function("ProcessPayment")]
        public string ProcessPayment([ActivityTrigger] string name)
        {
            _logger.LogInformation("Processing payment for: {Name}", name);
            Thread.Sleep(1000); // Simulate work
            return "PaymentDone";
        }

        [Function("SendConfirmation")]
        public string SendConfirmation([ActivityTrigger] string name)
        {
            _logger.LogInformation("Sending confirmation to: {Name}", name);
            Thread.Sleep(500); // Simulate work
            return "EmailSent";
        }

        // ============================================
        // FAN-OUT / FAN-IN EXAMPLE
        // ============================================
        // POST /api/start-fanout
        [Function("StartFanOut")]
        public async Task<HttpResponseData> StartFanOut(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "start-fanout")] HttpRequestData req,
            [DurableClient] DurableTaskClient client)
        {
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("FanOutOrchestrator");

            _logger.LogInformation("Started Fan-Out workflow: {InstanceId}", instanceId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new { instanceId, status = "Fan-Out started" });
            return response;
        }

        // Orchestrator for Fan-Out/Fan-In
        [Function("FanOutOrchestrator")]
        public async Task<int> FanOutOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            // Items to process in parallel
            var items = new[] { "Item1", "Item2", "Item3", "Item4", "Item5" };

            // FAN-OUT: Start all activities in parallel
            var tasks = new List<Task<int>>();
            foreach (var item in items)
            {
                tasks.Add(context.CallActivityAsync<int>("ProcessItem", item));
            }

            // FAN-IN: Wait for all to complete
            var results = await Task.WhenAll(tasks);

            // Aggregate results
            var total = results.Sum();
            return total;
        }

        // Activity for Fan-Out
        [Function("ProcessItem")]
        public int ProcessItem([ActivityTrigger] string item)
        {
            _logger.LogInformation("Processing: {Item}", item);
            Thread.Sleep(1000); // Simulate work
            return item.Length; // Return some value
        }

        // ============================================
        // CHECK STATUS
        // ============================================
        // GET /api/status/{instanceId}
        [Function("GetStatus")]
        public async Task<HttpResponseData> GetStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/{instanceId}")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            string instanceId)
        {
            // getInputsAndOutputs: true to include output data
            var status = await client.GetInstanceAsync(instanceId, getInputsAndOutputs: true);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                instanceId = status?.InstanceId,
                status = status?.RuntimeStatus.ToString(),
                output = status?.SerializedOutput
            });
            return response;
        }
    }
}
