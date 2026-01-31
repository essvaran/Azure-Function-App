using Azure.Data.Tables;
using FunctionApp.Repositories;
using FunctionApp.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace FunctionApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FunctionsDebugger.Enable();

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    // Register TableServiceClient
                    services.AddSingleton(sp =>
                    {
                        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        return new TableServiceClient(connectionString);
                    });

                    // Register Repository and Service
                    services.AddScoped<IProductRepository, ProductRepository>();
                    services.AddScoped<IProductService, ProductService>();
                })
                .Build();

            host.Run();
        }
    }
}
