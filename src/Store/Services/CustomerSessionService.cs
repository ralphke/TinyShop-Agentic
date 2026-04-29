using DataEntities;

namespace Store.Services;

public class CustomerSessionService
{
    private readonly IShopApiService shopApiService;

    public CustomerSessionService(IShopApiService shopApiService)
    {
        this.shopApiService = shopApiService;
    }

    public CustomerProfile? CurrentCustomer { get; private set; }

    public bool IsRegistered => CurrentCustomer is not null;

    public event Action? OnChange;

    public async Task<CustomerProfile> SaveAsync(CustomerProfile customer)
    {
        CurrentCustomer = await shopApiService.SaveCustomerAsync(customer);
        NotifyStateChanged();
        return CurrentCustomer;
    }

    public async Task DeleteAsync()
    {
        if (CurrentCustomer is null)
        {
            return;
        }

        await shopApiService.DeleteCustomerAsync(CurrentCustomer.Id);
        CurrentCustomer = null;
        NotifyStateChanged();
    }

    public void SetCurrentCustomer(CustomerProfile? customer)
    {
        CurrentCustomer = customer;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
