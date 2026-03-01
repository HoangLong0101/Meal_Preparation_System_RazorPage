using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class CustomerProfileAnalyzer : ICustomerProfileAnalyzer
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerProfileAnalyzer> _logger;

        public CustomerProfileAnalyzer(
            IUnitOfWork unitOfWork,
            ILogger<CustomerProfileAnalyzer> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CustomerContext> AnalyzeCustomerAsync(Guid customerId)
        {
            var context = new CustomerContext();

            // Get customer account
            var customer = await _unitOfWork.Accounts.GetByIdAsync(customerId);
            if (customer == null)
            {
                throw new NotFoundException($"Customer with ID {customerId} not found");
            }
            context.Customer = customer;

            // Get health profile
            var allHealthProfiles = await _unitOfWork.HealthProfiles.GetAllAsync();
            context.HealthProfile = allHealthProfiles.FirstOrDefault(hp => hp.AccountId == customerId);
            
            if (context.HealthProfile == null)
            {
                context.MissingDataWarnings.Add("No health profile found");
                _logger.LogWarning("Customer {CustomerId} has no health profile", customerId);
            }
            else
            {
                // Get allergies from health profile
                if (context.HealthProfile.Allergies != null && context.HealthProfile.Allergies.Any())
                {
                    context.Allergies = context.HealthProfile.Allergies.ToList();
                }
                else
                {
                    context.MissingDataWarnings.Add("No allergies recorded");
                }

                // Note: Food preferences are now stored as free text in HealthProfile.FoodPreferences
                // No need to populate context.Preferences list
            }

            // Get order history
            var allOrders = await _unitOfWork.Orders.GetAllAsync();
            context.OrderHistory = allOrders
                .Where(o => o.AccountId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .Take(10) // Last 10 orders
                .ToList();

            if (!context.OrderHistory.Any())
            {
                context.MissingDataWarnings.Add("No order history found");
            }

            // Determine if profile is complete
            context.HasCompleteProfile = context.HealthProfile != null 
                && context.Allergies.Any();

            _logger.LogInformation("Customer profile analyzed for {CustomerId}. Complete: {IsComplete}, Warnings: {WarningCount}", 
                customerId, context.HasCompleteProfile, context.MissingDataWarnings.Count);

            return context;
        }
    }
}
