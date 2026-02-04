# Azure Functions App

A comprehensive Azure Functions project demonstrating various trigger types, bindings, and patterns using .NET 8 Isolated Worker model.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Trigger Types](#trigger-types)
  - [HTTP Trigger](#1-http-trigger)
  - [Queue Trigger](#2-queue-trigger)
  - [Timer Trigger](#3-timer-trigger)
  - [Blob Trigger](#4-blob-trigger)
  - [Durable Functions](#5-durable-functions)
- [Bindings](#bindings)
- [Real-World Usage](#real-world-usage)
- [Limitations](#limitations)
- [API Reference](#api-reference)
- [Running Locally](#running-locally)

---

## Overview

This project demonstrates Azure Functions with:
- **Isolated Worker Model** (.NET 8)
- **Multiple Trigger Types** (HTTP, Queue, Timer, Blob, Durable)
- **Azure Table Storage** for data persistence
- **Dependency Injection** for clean architecture

---

## Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- Azurite (local storage emulator)
- Visual Studio 2022 or VS Code

---

## Project Structure

```
FunctionApp/
├── Functions/
│   ├── ProductFunctions.cs      # HTTP CRUD operations
│   ├── QueueFunctions.cs        # Queue trigger & output binding
│   ├── TimerFunctions.cs        # Scheduled tasks
│   ├── BlobFunctions.cs         # Blob processing
│   └── DurableFunctions.cs      # Orchestration workflows
├── Models/
│   ├── ProductEntity.cs         # Product model for Table Storage
│   └── Order.cs                 # Order models
├── Services/
│   └── ProductService.cs        # Business logic
├── Repositories/
│   └── ProductRepository.cs     # Data access layer
├── Program.cs                   # Host configuration & DI
├── host.json                    # Function host settings
└── local.settings.json          # Local configuration
```

---

## Trigger Types

### 1. HTTP Trigger

**What it does:** Responds to HTTP requests (GET, POST, PUT, DELETE)

**Real-world usage:**
- REST APIs
- Webhooks
- Form submissions

**Example:**
```csharp
[Function("GetAllProducts")]
public async Task<HttpResponseData> GetAll(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
{
    var products = await _productService.GetAllProductsAsync();
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(products);
    return response;
}
```

**Available Methods:**
| Method | Description |
|--------|-------------|
| `req.ReadFromJsonAsync<T>()` | Deserialize request body |
| `req.CreateResponse(statusCode)` | Create HTTP response |
| `response.WriteAsJsonAsync(data)` | Write JSON response |
| `req.Url.Query` | Get query parameters |
| `req.Headers` | Access request headers |

**Limitations:**
- Default timeout: 230 seconds (Consumption plan)
- Max request size: 100 MB
- Cold start latency on Consumption plan

---

### 2. Queue Trigger

**What it does:** Processes messages from Azure Storage Queue

**Real-world usage:**
- Order processing
- Email notifications
- Background jobs
- Decoupling services

**Example:**
```csharp
// Queue Trigger - receives messages
[Function("ProcessOrder")]
public async Task ProcessOrder(
    [QueueTrigger("orders-queue", Connection = "AzureWebJobsStorage")] string orderJson)
{
    var order = JsonSerializer.Deserialize<Order>(orderJson);
    _logger.LogInformation("Processing order: {OrderId}", order?.OrderId);
}

// Queue Output - sends messages
[Function("PlaceOrder")]
public async Task<PlaceOrderResponse> PlaceOrder(
    [HttpTrigger(...)] HttpRequestData req)
{
    return new PlaceOrderResponse
    {
        HttpResponse = response,
        QueueMessage = JsonSerializer.Serialize(order)  // Goes to queue
    };
}

public class PlaceOrderResponse
{
    [HttpResult]
    public HttpResponseData HttpResponse { get; set; }

    [QueueOutput("orders-queue", Connection = "AzureWebJobsStorage")]
    public string? QueueMessage { get; set; }
}
```

**How Queue Output Binding Works:**
```
1. Function returns PlaceOrderResponse
            ↓
2. Azure Functions sees [QueueOutput] attribute
            ↓
3. Automatically sends QueueMessage value to queue
            ↓
4. Queue Trigger picks it up for processing
```

**Available Properties:**
| Property | Description |
|----------|-------------|
| `queueName` | Name of the queue |
| `Connection` | Connection string setting name |

**Limitations:**
- Message size: Max 64 KB
- Message visibility timeout: 7 days max
- Poison queue after 5 failures (configurable)
- At-least-once delivery (may process duplicates)

---

### 3. Timer Trigger

**What it does:** Runs on a schedule using CRON expressions

**Real-world usage:**
- Scheduled reports
- Data cleanup
- Cache refresh
- Batch processing

**Example:**
```csharp
[Function("OrderReportTimer")]
public async Task OrderReportTimer(
    [TimerTrigger("0 */5 * * * *", RunOnStartup = true)] TimerInfo timerInfo)
{
    _logger.LogInformation("Timer triggered at: {Time}", DateTime.Now);
    _logger.LogInformation("Next run: {NextRun}", timerInfo.ScheduleStatus?.Next);
}
```

**CRON Expression Format:**
```
┌───────────── second (0-59)
│ ┌───────────── minute (0-59)
│ │ ┌───────────── hour (0-23)
│ │ │ ┌───────────── day of month (1-31)
│ │ │ │ ┌───────────── month (1-12)
│ │ │ │ │ ┌───────────── day of week (0-6, Sun=0)
│ │ │ │ │ │
* * * * * *
```

**Common Schedules:**
| Expression | Description |
|------------|-------------|
| `0 */5 * * * *` | Every 5 minutes |
| `0 0 * * * *` | Every hour |
| `0 0 9 * * *` | Daily at 9 AM |
| `0 0 0 * * 0` | Every Sunday at midnight |
| `0 0 0 1 * *` | First day of each month |

**Available Properties:**
| Property | Description |
|----------|-------------|
| `RunOnStartup` | Run immediately when app starts |
| `timerInfo.ScheduleStatus.Next` | Next scheduled run |
| `timerInfo.ScheduleStatus.Last` | Last run time |
| `timerInfo.IsPastDue` | True if running late |

**Limitations:**
- Minimum interval: 1 second
- Not guaranteed exact timing (may have delays)
- Single instance runs at a time (by default)
- Consumption plan: May have cold start delays

---

### 4. Blob Trigger

**What it does:** Processes files when uploaded to Azure Blob Storage

**Real-world usage:**
- Image processing/resizing
- File validation
- Document parsing
- Data import

**Example:**
```csharp
[Function("ProcessBlob")]
public void ProcessBlob(
    [BlobTrigger("uploads/{name}", Connection = "AzureWebJobsStorage")] string content,
    string name)
{
    _logger.LogInformation("File uploaded: {Name}", name);
    _logger.LogInformation("File size: {Size} bytes", content.Length);
}
```

**Path Patterns:**
| Pattern | Description |
|---------|-------------|
| `container/{name}` | Capture filename |
| `container/{name}.{ext}` | Capture name and extension |
| `container/{folder}/{name}` | Capture folder and name |

**Available Properties:**
| Property | Description |
|----------|-------------|
| `name` | Blob name from path |
| `content` | Blob content (string or Stream) |
| `BlobClient` | SDK client for blob operations |

**Limitations:**
- Not real-time (polling-based, can take up to 10 minutes)
- At-least-once processing (may process duplicates)
- Large files may timeout
- Consider Event Grid trigger for real-time needs

---

### 5. Durable Functions

**What it does:** Enables stateful, long-running workflows

**Components:**
```
┌──────────────┐     ┌────────────────┐     ┌──────────────┐
│   STARTER    │────>│  ORCHESTRATOR  │────>│   ACTIVITY   │
│  (HTTP/etc)  │     │  (Coordinator) │     │   (Worker)   │
└──────────────┘     └────────────────┘     └──────────────┘
```

**Real-world usage:**
- Multi-step order processing
- Approval workflows
- Long-running data processing
- Parallel processing (fan-out/fan-in)

#### Simple Workflow Example:

```csharp
// 1. STARTER - Entry point
[Function("StartWorkflow")]
public async Task<HttpResponseData> StartWorkflow(
    [HttpTrigger(...)] HttpRequestData req,
    [DurableClient] DurableTaskClient client)
{
    string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("OrderWorkflow", "John");
    return response;
}

// 2. ORCHESTRATOR - Coordinates workflow
[Function("OrderWorkflow")]
public async Task<string> OrderWorkflow(
    [OrchestrationTrigger] TaskOrchestrationContext context)
{
    var name = context.GetInput<string>();

    // Call activities in sequence
    var step1 = await context.CallActivityAsync<string>("ValidateOrder", name);
    var step2 = await context.CallActivityAsync<string>("ProcessPayment", name);
    var step3 = await context.CallActivityAsync<string>("SendConfirmation", name);

    return $"Completed: {step1} -> {step2} -> {step3}";
}

// 3. ACTIVITY - Does actual work
[Function("ValidateOrder")]
public string ValidateOrder([ActivityTrigger] string name)
{
    return "Validated";
}
```

#### Fan-Out/Fan-In Pattern:

```csharp
[Function("FanOutOrchestrator")]
public async Task<int> FanOutOrchestrator(
    [OrchestrationTrigger] TaskOrchestrationContext context)
{
    var items = new[] { "Item1", "Item2", "Item3", "Item4", "Item5" };

    // FAN-OUT: Start all in parallel
    var tasks = new List<Task<int>>();
    foreach (var item in items)
    {
        tasks.Add(context.CallActivityAsync<int>("ProcessItem", item));
    }

    // FAN-IN: Wait for all to complete
    var results = await Task.WhenAll(tasks);

    return results.Sum();
}
```

**Orchestrator Available Methods:**
| Method | Description |
|--------|-------------|
| `context.GetInput<T>()` | Get input data |
| `context.CallActivityAsync<T>(name, input)` | Call an activity |
| `context.CreateTimer(fireAt)` | Create a timer |
| `context.WaitForExternalEvent<T>(name)` | Wait for external event |
| `context.ContinueAsNew(input)` | Restart orchestration |

**DurableTaskClient Methods:**
| Method | Description |
|--------|-------------|
| `ScheduleNewOrchestrationInstanceAsync()` | Start new orchestration |
| `GetInstanceAsync(id, getInputsAndOutputs)` | Get status |
| `TerminateInstanceAsync(id, reason)` | Terminate |
| `RaiseEventAsync(id, eventName, data)` | Send event |

**Limitations:**
- Orchestrator code must be deterministic
- No I/O in orchestrators (use activities)
- State stored in Azure Storage (adds latency)
- Max execution time: 7 days (default)

---

## Bindings

### Input Bindings
| Binding | Purpose |
|---------|---------|
| `[HttpTrigger]` | HTTP request data |
| `[QueueTrigger]` | Queue message |
| `[BlobTrigger]` | Blob content |
| `[TimerTrigger]` | Timer information |
| `[OrchestrationTrigger]` | Orchestration context |
| `[ActivityTrigger]` | Activity input |
| `[DurableClient]` | Durable task client |

### Output Bindings
| Binding | Purpose |
|---------|---------|
| `[HttpResult]` | HTTP response |
| `[QueueOutput]` | Send to queue |
| `[BlobOutput]` | Write to blob |
| `[TableOutput]` | Write to table |

---

## Real-World Usage

| Trigger | Use Case |
|---------|----------|
| HTTP | REST APIs, webhooks, form processing |
| Queue | Order processing, email sending, decoupled services |
| Timer | Reports, cleanup jobs, cache refresh |
| Blob | File processing, image resizing, document parsing |
| Durable | Multi-step workflows, approval processes, batch jobs |

---

## Limitations

### General
- **Cold Start**: Consumption plan may have 1-10 second delays
- **Timeout**: 5 min (Consumption), 30 min (Premium), unlimited (Dedicated)
- **Memory**: 1.5 GB max (Consumption)
- **Scaling**: Up to 200 instances (Consumption)

### Storage
- **Queue Message**: 64 KB max
- **Blob Trigger Latency**: Up to 10 minutes (use Event Grid for real-time)
- **Table Entity**: 1 MB max

### Durable Functions
- **Orchestrator**: Must be deterministic, no I/O
- **Payload Size**: 16 KB for activities (use blob for larger)
- **Max Duration**: 7 days default

---

## API Reference

### Products API
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | Get all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Delete product |

### Orders API
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/orders` | Place order (queued) |
| GET | `/api/orders` | Get all orders |

### Durable Functions API
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/start-workflow?name=John` | Start workflow |
| POST | `/api/start-fanout` | Start fan-out demo |
| GET | `/api/status/{instanceId}` | Get workflow status |

---

## Running Locally

1. **Start Azurite** (storage emulator):
   ```bash
   azurite --silent
   ```

2. **Run the function app**:
   ```bash
   cd FunctionApp
   func start
   ```

3. **Test endpoints** using Postman or curl.

---

## Configuration

### local.settings.json
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### host.json
```json
{
  "version": "2.0",
  "logging": {
    "logLevel": {
      "default": "Information",
      "Azure.Core": "Warning",
      "Azure.Storage": "Warning"
    }
  }
}
```

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.Azure.Functions.Worker` | Core worker |
| `Microsoft.Azure.Functions.Worker.Sdk` | SDK tooling |
| `Microsoft.Azure.Functions.Worker.Extensions.Http` | HTTP trigger |
| `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` | Queue trigger |
| `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` | Blob trigger |
| `Microsoft.Azure.Functions.Worker.Extensions.Timer` | Timer trigger |
| `Microsoft.Azure.Functions.Worker.Extensions.DurableTask` | Durable functions |
| `Azure.Data.Tables` | Table Storage SDK |
