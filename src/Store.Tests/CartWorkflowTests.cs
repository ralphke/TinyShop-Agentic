using Bunit;
using System.Net.Http.Json;
using DataEntities;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Store.Components.Pages;
using Store.Services;
using Xunit;

namespace Store.Tests;

public class CartWorkflowTests : TestContext
{
    [Fact]
    public void AddToCart_ShouldUpdateCartCounter()
    {
        // Arrange
        var cartService = new CartService();
        Services.AddScoped<CartService>(_ => cartService);
        
        var product = new Product { Id = 1, Name = "Test Product", Price = 10.99m };

        // Act
        cartService.AddItem(product);

        // Assert
        cartService.GetItemCount().Should().Be(1);
        cartService.GetItems().Should().ContainSingle()
            .Which.Product.Name.Should().Be("Test Product");
    }

    [Fact]
    public void MultipleProducts_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var cartService = new CartService();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 15.99m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 25.50m };
        var product3 = new Product { Id = 3, Name = "Product 3", Price = 9.99m };

        // Act
        cartService.AddItem(product1);
        cartService.AddItem(product1); // Add twice
        cartService.AddItem(product2);
        cartService.AddItem(product3);
        cartService.AddItem(product3);
        cartService.AddItem(product3); // Add three times

        // Assert
        cartService.GetItemCount().Should().Be(6); // 2+1+3
        cartService.GetTotal().Should().Be(87.45m); // (15.99*2) + (25.50*1) + (9.99*3)
    }

    [Fact]
    public void RemoveProduct_ShouldUpdateTotals()
    {
        // Arrange
        var cartService = new CartService();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 20.00m };
        
        cartService.AddItem(product1);
        cartService.AddItem(product1);
        cartService.AddItem(product2);

        // Act
        cartService.RemoveItem(1); // Remove one of product1

        // Assert
        cartService.GetItemCount().Should().Be(2); // 1+1
        cartService.GetTotal().Should().Be(30.00m); // (10*1) + (20*1)
    }

    [Fact]
    public void Checkout_ShouldClearCart()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        cartService.AddItem(product);
        cartService.AddItem(product);

        // Act - Simulate checkout
        cartService.Clear();

        // Assert
        cartService.GetItemCount().Should().Be(0);
        cartService.GetTotal().Should().Be(0);
        cartService.GetItems().Should().BeEmpty();
    }

    [Fact]
    public void CartService_ShouldHandleEdgeCases()
    {
        // Arrange
        var cartService = new CartService();

        // Act & Assert - Remove from empty cart
        cartService.RemoveItem(999);
        cartService.GetItemCount().Should().Be(0);

        // Act & Assert - Remove non-existent product
        var product = new Product { Id = 1, Name = "Product", Price = 10.00m };
        cartService.AddItem(product);
        cartService.RemoveItem(999);
        cartService.GetItemCount().Should().Be(1);
    }

    [Fact]
    public void OnChange_Event_ShouldTriggerUIUpdate()
    {
        // Arrange
        var cartService = new CartService();
        var changeCount = 0;
        cartService.OnChange += () => changeCount++;
        
        var product = new Product { Id = 1, Name = "Product", Price = 10.00m };

        // Act
        cartService.AddItem(product);
        cartService.AddItem(product);
        cartService.RemoveItem(1);
        cartService.Clear();

        // Assert
        changeCount.Should().Be(4); // Add, Add, Remove, Clear
    }

    [Fact]
    public void CheckoutFlow_SubmitOrder_ClearsCartAndNavigatesToConfirmation()
    {
        // Arrange
        var cartService = new CartService();
        var product = new Product { Id = 7, Name = "Flow Product", Price = 12.34m };
        cartService.AddItem(product, 2);

        var customer = new CustomerProfile
        {
            Id = 11,
            Name = "Taylor Example",
            Email = "taylor@example.com",
            Address = "1 Test Street"
        };

        var shopApiService = new Mock<IShopApiService>();
        shopApiService
            .Setup(service => service.PlaceOrderAsync(customer.Id, It.IsAny<IReadOnlyCollection<CartItem>>()))
            .ReturnsAsync(new Order { Id = 123, CustomerId = customer.Id, Status = OrderStatuses.Completed, Total = 24.68m });

        var customerSession = new CustomerSessionService(shopApiService.Object);
        customerSession.SetCurrentCustomer(customer);

        Services.AddScoped<CartService>(_ => cartService);
        Services.AddSingleton<IShopApiService>(shopApiService.Object);
        Services.AddScoped<CustomerSessionService>(_ => customerSession);

        var navigationManager = Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = RenderComponent<Checkout>();
        cut.FindAll("button").Single(button => button.TextContent.Contains("Place order")).Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cartService.GetItems().Should().BeEmpty();
            cartService.GetTotal().Should().Be(0m);
            navigationManager.Uri.Should().EndWith("/order-confirmation?orderId=123");
        });
    }

    [Fact]
    public void HomePage_ViewDetails_NavigatesToDedicatedProductPage()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Solar Powered Flashlight", Description = "Compact trail light", Details = "Extended flashlight details", Price = 19.99m, ImageUrl = "images/product1.png" },
            new() { Id = 2, Name = "Hiking Poles", Description = "Trail support", Details = "Pole details", Price = 24.99m, ImageUrl = "images/product2.png" },
            new() { Id = 3, Name = "Camping Lantern", Description = "Camp light", Details = "Lantern details", Price = 19.99m, ImageUrl = "images/product3.png" }
        };
        RegisterProductService(products);

        // Act
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Solar Powered Flashlight"));
        cut.FindAll("button").First(button => button.TextContent.Trim() == "View Details").Click();

        // Assert
        navigationManager.Uri.Should().EndWith("/products/1");
    }

    [Fact]
    public void ProductsPage_DetailsButton_NavigatesToDedicatedProductPage()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Name = "Solar Powered Flashlight", Description = "Compact trail light", Details = "Extended flashlight details", Price = 19.99m, ImageUrl = "images/product1.png" },
            new() { Id = 2, Name = "Hiking Poles", Description = "Trail support", Details = "Pole details", Price = 24.99m, ImageUrl = "images/product2.png" }
        };
        RegisterProductService(products);

        // Act
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<Products>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Solar Powered Flashlight"));
        cut.FindAll("button").First(button => button.TextContent.Trim() == "Details").Click();

        // Assert
        navigationManager.Uri.Should().EndWith("/products/1");
    }

    private void RegisterProductService(List<Product> products)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = JsonContent.Create(products)
        });

        Services.AddMemoryCache();
        Services.AddSingleton<ProductService>(_ => new ProductService(
            new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7130") },
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProductBrowserEndpoint"] = "https://localhost:7130"
            }).Build(),
            _.GetRequiredService<IMemoryCache>()));
        Services.AddScoped<CartService>(_ => new CartService());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
