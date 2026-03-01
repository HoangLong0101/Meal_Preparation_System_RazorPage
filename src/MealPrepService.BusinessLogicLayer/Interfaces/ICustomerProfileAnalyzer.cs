using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface ICustomerProfileAnalyzer
    {
        Task<CustomerContext> AnalyzeCustomerAsync(Guid customerId);
    }
}
