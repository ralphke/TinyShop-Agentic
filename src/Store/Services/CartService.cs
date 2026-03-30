using DataEntities;

namespace Store.Services;

public class CartItem
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public class CartService : IDisposable
{
    private readonly Dictionary<int, CartItem> _items = new();
    private bool _disposed;

    public event Action? OnChange;

    public IReadOnlyDictionary<int, CartItem> Items => _items;

    public IReadOnlyList<CartItem> GetItems() => _items.Values.ToList().AsReadOnly();

    public int GetItemCount() => _items.Values.Sum(i => i.Quantity);

    public void AddItem(Product product, int quantity = 1)
    {
        if (product is null) throw new ArgumentNullException(nameof(product));
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (_items.TryGetValue(product.Id, out var existing))
            existing.Quantity += quantity;
        else
            _items[product.Id] = new CartItem { Product = product, Quantity = quantity };

        NotifyStateChanged();
    }

    public void RemoveItem(int productId, int quantity = 1)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (_items.TryGetValue(productId, out var existing))
        {
            existing.Quantity -= quantity;
            if (existing.Quantity <= 0)
                _items.Remove(productId);
        }

        NotifyStateChanged();
    }

    public void RemoveAll(int productId)
    {
        _items.Remove(productId);
        NotifyStateChanged();
    }

    public decimal GetTotal() => _items.Values.Sum(i => i.Product.Price * i.Quantity);

    public void Clear()
    {
        _items.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        if (OnChange is null)
        {
            return;
        }

        foreach (Action handler in OnChange.GetInvocationList())
        {
            try
            {
                handler();
            }
            catch (ObjectDisposedException)
            {
                // A disposed UI subscriber should not break cart operations.
            }
            catch (InvalidOperationException)
            {
                // Ignore off-dispatcher UI updates during request/circuit teardown.
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Clear();
        }
    }
}
