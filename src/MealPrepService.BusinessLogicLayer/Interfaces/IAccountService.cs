using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IAccountService
    {
        Task<AccountDto> RegisterAsync(CreateAccountDto dto);
        Task<AccountDto> AuthenticateAsync(string email, string password);
        Task<AccountDto> GetByIdAsync(Guid accountId);
        Task<bool> EmailExistsAsync(string email);
        
        // Admin CRUD operations for Manager and DeliveryMan accounts
        Task<IEnumerable<AccountDto>> GetAllStaffAccountsAsync();
        Task<IEnumerable<AccountDto>> GetAccountsByRoleAsync(string role);
        Task<AccountDto> CreateStaffAccountAsync(CreateAccountDto dto, string role);
        Task<AccountDto> UpdateStaffAccountAsync(Guid accountId, UpdateAccountDto dto);
        Task<bool> DeleteStaffAccountAsync(Guid accountId);
    }
}