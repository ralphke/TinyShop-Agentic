using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataEntities;

/// <summary>
/// Represents a login request with email and password.
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>

/// Represents a customer registration request with password.
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [StringLength(1000)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(255)]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [StringLength(255)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    [StringLength(255)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [StringLength(255)]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    [StringLength(255)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
}

public class CustomerProfile
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Password hash (e.g., using a strong one-way hashing algorithm with salt).
    /// This field is stored as a hash only in the database and is never sent to the client in API responses.
    /// </summary>
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    [JsonIgnore]
    public List<Order> Orders { get; set; } = [];
}

public class CreateOrderRequest
{
    public int CustomerId { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class Order
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public string Status { get; set; } = OrderStatuses.Completed;

    public decimal Total { get; set; }

    public DateTime CreatedDate { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    [JsonIgnore]
    public CustomerProfile? Customer { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }
}
