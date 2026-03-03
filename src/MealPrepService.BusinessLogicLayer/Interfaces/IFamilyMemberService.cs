using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IFamilyMemberService
    {
        Task<IEnumerable<FamilyMemberDto>> GetByAccountIdAsync(Guid accountId);
        Task<FamilyMemberDto> AddAsync(FamilyMemberDto dto);
        Task RemoveAsync(Guid memberId, Guid accountId);
        Task<int> GetCountAsync(Guid accountId);
    }
}
