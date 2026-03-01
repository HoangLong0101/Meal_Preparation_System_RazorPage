using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            IUnitOfWork unitOfWork, 
            IPasswordHasher passwordHasher, 
            ILogger<AccountService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AccountDto> RegisterAsync(CreateAccountDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new BusinessException("Email is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new BusinessException("Password is required");
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new BusinessException("Full name is required");
            }

            // Validate email doesn't exist
            if (await _unitOfWork.Accounts.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", dto.Email);
                throw new BusinessException("Email already exists");
            }

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            // Create account entity
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                PasswordHash = passwordHash,
                FullName = dto.FullName,
                Role = "Customer",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Accounts.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Account created successfully for email: {Email}", dto.Email);

            return MapToDto(account);
        }

        public async Task<AccountDto> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
            }

            var account = await _unitOfWork.Accounts.GetByEmailAsync(email);

            if (account == null || !_passwordHasher.VerifyPassword(password, account.PasswordHash))
            {
                _logger.LogWarning("Failed authentication attempt for email: {Email}", email);
                throw new AuthenticationException("Invalid credentials");
            }

            _logger.LogInformation("Successful authentication for email: {Email}", email);

            return MapToDto(account);
        }

        public async Task<AccountDto> GetByIdAsync(Guid accountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);

            if (account == null)
            {
                throw new BusinessException($"Account with ID {accountId} not found");
            }

            return MapToDto(account);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return await _unitOfWork.Accounts.EmailExistsAsync(email);
        }

        // Admin CRUD operations for Manager and DeliveryMan accounts
        public async Task<IEnumerable<AccountDto>> GetAllStaffAccountsAsync()
        {
            var accounts = await _unitOfWork.Accounts.GetAllAsync();
            var staffAccounts = accounts.Where(a => a.Role == "Manager" || a.Role == "DeliveryMan");
            
            _logger.LogInformation("Retrieved {Count} staff accounts", staffAccounts.Count());
            
            return staffAccounts.Select(MapToDto);
        }

        public async Task<IEnumerable<AccountDto>> GetAccountsByRoleAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Role is required", nameof(role));
            }

            var accounts = await _unitOfWork.Accounts.GetAllAsync();
            var roleAccounts = accounts.Where(a => a.Role.Equals(role, StringComparison.OrdinalIgnoreCase));
            
            _logger.LogInformation("Retrieved {Count} accounts with role: {Role}", roleAccounts.Count(), role);
            
            return roleAccounts.Select(MapToDto);
        }

        public async Task<AccountDto> CreateStaffAccountAsync(CreateAccountDto dto, string role)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Role is required", nameof(role));
            }

            // Validate role is Manager or DeliveryMan
            if (role != "Manager" && role != "DeliveryMan")
            {
                throw new BusinessException("Invalid role. Only Manager and DeliveryMan roles can be created.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new BusinessException("Email is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new BusinessException("Password is required");
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new BusinessException("Full name is required");
            }

            // Validate email doesn't exist
            if (await _unitOfWork.Accounts.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Staff account creation attempt with existing email: {Email}", dto.Email);
                throw new BusinessException("Email already exists");
            }

            // Hash password
            var passwordHash = _passwordHasher.HashPassword(dto.Password);

            // Create account entity
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                PasswordHash = passwordHash,
                FullName = dto.FullName,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Accounts.AddAsync(account);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Staff account created successfully for email: {Email} with role: {Role}", dto.Email, role);

            return MapToDto(account);
        }

        public async Task<AccountDto> UpdateStaffAccountAsync(Guid accountId, UpdateAccountDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);

            if (account == null)
            {
                throw new NotFoundException($"Account with ID {accountId} not found");
            }

            // Validate role is Manager or DeliveryMan
            if (account.Role != "Manager" && account.Role != "DeliveryMan")
            {
                throw new BusinessException("Only Manager and DeliveryMan accounts can be updated through this method.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new BusinessException("Email is required");
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new BusinessException("Full name is required");
            }

            // Validate role if being changed
            if (!string.IsNullOrWhiteSpace(dto.Role) && dto.Role != "Manager" && dto.Role != "DeliveryMan")
            {
                throw new BusinessException("Invalid role. Only Manager and DeliveryMan roles are allowed.");
            }

            // Check if email is being changed and if new email already exists
            if (dto.Email != account.Email && await _unitOfWork.Accounts.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Staff account update attempt with existing email: {Email}", dto.Email);
                throw new BusinessException("Email already exists");
            }

            // Update account properties
            account.Email = dto.Email;
            account.FullName = dto.FullName;
            
            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                account.Role = dto.Role;
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                account.PasswordHash = _passwordHasher.HashPassword(dto.Password);
            }

            await _unitOfWork.Accounts.UpdateAsync(account);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Staff account updated successfully for ID: {AccountId}", accountId);

            return MapToDto(account);
        }

        public async Task<bool> DeleteStaffAccountAsync(Guid accountId)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);

            if (account == null)
            {
                throw new NotFoundException($"Account with ID {accountId} not found");
            }

            // Validate role is Manager or DeliveryMan
            if (account.Role != "Manager" && account.Role != "DeliveryMan")
            {
                throw new BusinessException("Only Manager and DeliveryMan accounts can be deleted through this method.");
            }

            await _unitOfWork.Accounts.DeleteAsync(accountId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Staff account deleted successfully for ID: {AccountId}", accountId);

            return true;
        }

        private AccountDto MapToDto(Account account)
        {
            return new AccountDto
            {
                Id = account.Id,
                Email = account.Email,
                FullName = account.FullName,
                Role = account.Role
            };
        }
    }
}