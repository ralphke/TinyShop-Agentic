using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;
using Products.Services;

namespace Products.Endpoints;

public static class CustomerOrderEndpoints
{
    public static void MapCustomerOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var customerGroup = routes.MapGroup("/api/customers");

        // GET /api/customers/{customerId}
        customerGroup.MapGet("/{customerId:int}", GetCustomerById)
            .WithName("GetCustomerById")
            .Produces<CustomerProfile>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/customers/register - New registration with password
        customerGroup.MapPost("/register", RegisterCustomer)
            .WithName("RegisterCustomer")
            .Produces<CustomerProfile>(StatusCodes.Status200OK)
            .Produces<CustomerProfile>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            ;

        // POST /api/customers/login - Login with email and password
        customerGroup.MapPost("/login", LoginCustomer)
            .WithName("LoginCustomer")
            .Produces<CustomerProfile>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            ;

        // POST /api/customers/{customerId}/change-password - Change password
        customerGroup.MapPost("/{customerId:int}/change-password", ChangePassword)
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            ;

        // GET /api/customers/{customerId}/orders - Get customer's orders
        customerGroup.MapGet("/{customerId:int}/orders", GetCustomerOrders)
            .WithName("GetCustomerOrders")
            .Produces<List<Order>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/customers/{customerId}
        customerGroup.MapDelete("/{customerId:int}", DeleteCustomer)
            .WithName("DeleteCustomer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        var orderGroup = routes.MapGroup("/api/orders");

        // POST /api/orders - Create order
        orderGroup.MapPost("/", CreateOrder)
            .WithName("CreateOrder")
            .Produces<Order>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }

    // GET /api/customers/{customerId}
    private static async Task<IResult> GetCustomerById(int customerId, ProductDataContext db)
    {
        var customer = await db.Customers.FindAsync(customerId);
        return customer is null ? Results.NotFound() : Results.Ok(customer);
    }

    // POST /api/customers/register
    private static async Task<IResult> RegisterCustomer(RegisterRequest request, ProductDataContext db)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { message = "Name is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { message = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return Results.BadRequest(new { message = "Address is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Results.BadRequest(new { message = "Password must be at least 8 characters." });
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Results.BadRequest(new { message = "Passwords do not match." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;

        // Check if email already exists
        var existingCustomer = await db.Customers.SingleOrDefaultAsync(c => c.Email == normalizedEmail);
        if (existingCustomer is not null)
        {
            // Update existing customer (upsert behavior)
            existingCustomer.Name = request.Name.Trim();
            existingCustomer.Email = normalizedEmail;
            existingCustomer.Address = request.Address.Trim();
            existingCustomer.PasswordHash = PasswordService.HashPassword(request.Password);
            existingCustomer.ModifiedDate = now;

            await db.SaveChangesAsync();
            return Results.Ok(existingCustomer);
        }

        // Create new customer
        var newCustomer = new CustomerProfile
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            Address = request.Address.Trim(),
            PasswordHash = PasswordService.HashPassword(request.Password),
            CreatedDate = now,
            ModifiedDate = now
        };

        db.Customers.Add(newCustomer);
        await db.SaveChangesAsync();

        return Results.Created($"/api/customers/{newCustomer.Id}", newCustomer);
    }

    // POST /api/customers/login
    private static async Task<IResult> LoginCustomer(LoginRequest request, ProductDataContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { message = "Email is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Password is required." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var customer = await db.Customers.SingleOrDefaultAsync(c => c.Email == normalizedEmail);

        if (customer is null)
        {
            return Results.NotFound(new { message = "Customer not found." });
        }

        // Verify password
        if (!PasswordService.VerifyPassword(request.Password, customer.PasswordHash))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(customer);
    }

    // POST /api/customers/{customerId}/change-password
    private static async Task<IResult> ChangePassword(int customerId, ChangePasswordRequest request, ProductDataContext db)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Results.BadRequest(new { message = "Current password is required." });
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return Results.BadRequest(new { message = "New password must be at least 8 characters." });
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            return Results.BadRequest(new { message = "New passwords do not match." });
        }

        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return Results.NotFound(new { message = "Customer not found." });
        }

        // Verify current password
        if (!PasswordService.VerifyPassword(request.CurrentPassword, customer.PasswordHash))
        {
            return Results.Unauthorized();
        }

        // Update password
        customer.PasswordHash = PasswordService.HashPassword(request.NewPassword);
        customer.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // GET /api/customers/{customerId}/orders
    private static async Task<IResult> GetCustomerOrders(int customerId, ProductDataContext db)
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return Results.NotFound(new { message = "Customer not found." });
        }

        var orders = await db.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync();

        return Results.Ok(orders);
    }

    // DELETE /api/customers/{customerId}
    private static async Task<IResult> DeleteCustomer(int customerId, ProductDataContext db)
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return Results.NotFound();
        }

        var hasAnyOrders = await db.Orders.AnyAsync(o => o.CustomerId == customerId);
        if (hasAnyOrders)
        {
            return Results.Conflict(new { message = "Customer account cannot be deleted while orders exist in the database." });
        }

        db.Customers.Remove(customer);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // POST /api/orders
    private static async Task<IResult> CreateOrder(CreateOrderRequest request, ProductDataContext db)
    {
        if (request.CustomerId <= 0)
        {
            return Results.BadRequest(new { message = "A registered customer is required to place an order." });
        }

        if (request.Items.Count == 0 || request.Items.Any(item => item.Quantity <= 0))
        {
            return Results.BadRequest(new { message = "Order items must contain at least one valid product." });
        }

        var customer = await db.Customers.FindAsync(request.CustomerId);
        if (customer is null)
        {
            return Results.BadRequest(new { message = "Customer record was not found." });
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await db.Product
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id);

        var missingProductIds = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missingProductIds.Count > 0)
        {
            return Results.BadRequest(new { message = $"Products not found: {string.Join(", ", missingProductIds)}" });
        }

        var order = new Order
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerEmail = customer.Email,
            ShippingAddress = customer.Address,
            Status = OrderStatuses.Completed,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var requestItem in request.Items)
        {
            var product = products[requestItem.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name ?? $"Product {product.Id}",
                UnitPrice = product.Price,
                Quantity = requestItem.Quantity
            });
        }

        order.Total = order.Items.Sum(item => item.UnitPrice * item.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return Results.Created($"/api/orders/{order.Id}", order);
    }
}
