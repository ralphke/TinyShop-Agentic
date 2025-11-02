using System;
using System.Net;
using System.Net.Http.Json;
using DataEntities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Products.Data;
using Store.Services;
using Xunit;

namespace IntegrationTests;

public class StoreUiTests
{
    [Fact]
    public async Task ProductsPage_LoadsAndContainsProductNames()
    {
        // Start the Products app in-memory
        await using var productsFactory = new WebApplicationFactory<Products.Program>();
   
        // Seed the Products database with test data
        using (var scope = productsFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDataContext>();
            db.Database.EnsureCreated();
            
            if (!db.Product.Any())
            {
                db.Product.Add(new Product { Id = 1, Name = "Solar Powered Flashlight", Description = "Test product", Price = 19.99m, ImageUrl = "product1.png" });
                db.SaveChanges();
            }
        }

        var productsClient = productsFactory.CreateClient();
        var productsBase = productsClient.BaseAddress?.ToString() ?? throw new InvalidOperationException("Products base address not available");

        // Use the Products test server base (do NOT append '/api/Product' here).
        // ProductService calls '/api/Product' relative to its BaseAddress, so providing the server root avoids duplicated paths.
        var productApiBase = productsBase.TrimEnd('/');

        // Start the Store app but override configuration so it calls the in-memory Products server
        // ALSO override the Store's HttpClient registrations to route outgoing HTTP calls
        // into the in-memory Products server (avoids connecting to localhost:80).
        await using var storeFactory = new WebApplicationFactory<Store.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, conf) =>
                {
                    // Inject ProductEndpoint that points to the running Products test server root
                    var dict = new Dictionary<string, string?>
                    {
                        ["ProductEndpoint"] = productApiBase
                    };
                    conf.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    // Override the typed HttpClient for ProductService to use the in-memory Products TestServer.
                    services.AddHttpClient<ProductService>(client =>
                    {
                        client.BaseAddress = new Uri(productApiBase);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => productsFactory.Server.CreateHandler());
                });
            });

        var client = storeFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var response = await client.GetAsync("/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();

        // Write HTML to test output for debugging
        Console.WriteLine("===== STORE PAGE HTML START =====");
        Console.WriteLine(html);
        Console.WriteLine("===== STORE PAGE HTML END =====");

        html.Should().Contain("Products");
        // check at least one seeded product name exists
        html.Should().Contain("Solar Powered Flashlight");
    }
}
