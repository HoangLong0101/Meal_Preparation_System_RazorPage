using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.BusinessLogicLayer.Services;

public class ShipperService : IShipperService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShipperService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ShipperDto>> GetActiveShippersAsync()
    {
        await EnsureShippersFromDeliveryManAccountsAsync();

        var shippers = await _unitOfWork.Shippers.FindAsync(s => s.IsActive);
        return shippers
            .OrderBy(s => s.FullName)
            .Select(s => new ShipperDto
            {
                Id = s.Id,
                AccountId = s.AccountId,
                FullName = s.FullName,
                ContactPhone = s.ContactPhone,
                IsActive = s.IsActive
            });
    }

    private async Task EnsureShippersFromDeliveryManAccountsAsync()
    {
        var deliveryAccounts = await _unitOfWork.Accounts.FindAsync(a => a.Role == "DeliveryMan");
        var existingShippers = await _unitOfWork.Shippers.GetAllAsync();

        var existingByAccountId = existingShippers
            .Where(s => s.AccountId.HasValue)
            .ToDictionary(s => s.AccountId!.Value, s => s);

        var hasChanges = false;

        foreach (var account in deliveryAccounts)
        {
            if (existingByAccountId.ContainsKey(account.Id))
            {
                continue;
            }

            var shipper = new Shipper
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                FullName = account.FullName,
                ContactPhone = account.Email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Shippers.AddAsync(shipper);
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
