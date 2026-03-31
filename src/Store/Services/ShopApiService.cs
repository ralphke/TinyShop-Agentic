using System.Text.Json;
using System.Text.Json.Serialization;
using DataEntities;

namespace Store.Services;

public interface IShopApiService
{
    Task<CustomerProfile> RegisterAsync(RegisterRequest request);

    Task<CustomerProfile> LoginAsync(LoginRequest request);

    Task<CustomerProfile> SaveCustomerAsync(CustomerProfile customer);

    Task<bool> ChangePasswordAsync(int customerId, ChangePasswordRequest request);

    Task DeleteCustomerAsync(int customerId);

    Task<List<Order>> GetCustomerOrdersAsync(int customerId);

    Task<Order> PlaceOrderAsync(int customerId, IReadOnlyCollection<CartItem> items);
}

public class ShopApiService : IShopApiService
{
    private readonly HttpClient httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ShopApiService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<CustomerProfile> RegisterAsync(RegisterRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("/api/customers/register", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await GetErrorMessageAsync(response, "Unable to register customer."));
        }

        return await response.Content.ReadFromJsonAsync<CustomerProfile>(JsonOptions)
            ?? throw new InvalidOperationException("The server returned an empty customer response.");
    }

    public async Task<CustomerProfile> LoginAsync(LoginRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("/api/customers/login", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException("Customer not found.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Invalid email or password.");
            }

            throw new InvalidOperationException(await GetErrorMessageAsync(response, "Unable to login."));
        }

        return await response.Content.ReadFromJsonAsync<CustomerProfile>(JsonOptions)
            ?? throw new InvalidOperationException("The server returned an empty customer response.");
    }

    public async Task<CustomerProfile> SaveCustomerAsync(CustomerProfile customer)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/customers/{customer.Id}", customer, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await GetErrorMessageAsync(response, "Unable to save customer details."));
        }

        return await response.Content.ReadFromJsonAsync<CustomerProfile>(JsonOptions)
            ?? throw new InvalidOperationException("The server returned an empty customer response.");
    }

    public async Task<bool> ChangePasswordAsync(int customerId, ChangePasswordRequest request)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/customers/{customerId}/change-password", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            throw new InvalidOperationException(await GetErrorMessageAsync(response, "Unable to change password."));
        }

        return true;
    }

    public async Task DeleteCustomerAsync(int customerId)
    {
        var response = await httpClient.DeleteAsync($"/api/customers/{customerId}");
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException(await GetErrorMessageAsync(response, "Unable to delete customer details."));
    }

    public async Task<List<Order>> GetCustomerOrdersAsync(int customerId)
    {
        var response = await httpClient.GetAsync($"/api/customers/{customerId}/orders");
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await GetErrorMessageAsync(response, "Unable to retrieve orders."));
        }

        return await response.Content.ReadFromJsonAsync<List<Order>>(JsonOptions)
            ?? new List<Order>();
    }

    public async Task<Order> PlaceOrderAsync(int customerId, IReadOnlyCollection<CartItem> items)
    {
        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = items
                .Select(item => new CreateOrderItemRequest
                {
                    ProductId = item.Product.Id,
                    Quantity = item.Quantity
                })
                .ToList()
        };

        var response = await httpClient.PostAsJsonAsync("/api/orders", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(await GetErrorMessageAsync(response, "Unable to place the order."));
        }

        return await response.Content.ReadFromJsonAsync<Order>(JsonOptions)
            ?? throw new InvalidOperationException("The server returned an empty order response.");
    }

    private static async Task<string> GetErrorMessageAsync(HttpResponseMessage response, string fallbackMessage)
    {
        try
        {
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(contentStream);

            if (document.RootElement.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }
        catch (JsonException)
        {
        }

        return fallbackMessage;
    }
}
