using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class FamilyMemberService : IFamilyMemberService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FamilyMemberService> _logger;

        public FamilyMemberService(IUnitOfWork unitOfWork, ILogger<FamilyMemberService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<FamilyMemberDto>> GetByAccountIdAsync(Guid accountId)
        {
            var members = await _unitOfWork.FamilyMembers.FindAsync(fm => fm.AccountId == accountId);
            return members.Select(fm => new FamilyMemberDto
            {
                Id = fm.Id,
                AccountId = fm.AccountId,
                Name = fm.Name,
                Age = fm.Age,
                Notes = fm.Notes
            });
        }

        public async Task<FamilyMemberDto> AddAsync(FamilyMemberDto dto)
        {
            var entity = new FamilyMember
            {
                Id = Guid.NewGuid(),
                AccountId = dto.AccountId,
                Name = dto.Name,
                Age = dto.Age,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FamilyMembers.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Family member '{Name}' added for account {AccountId}", dto.Name, dto.AccountId);

            dto.Id = entity.Id;
            return dto;
        }

        public async Task RemoveAsync(Guid memberId, Guid accountId)
        {
            var member = await _unitOfWork.FamilyMembers.GetByIdAsync(memberId);
            if (member == null || member.AccountId != accountId)
            {
                throw new InvalidOperationException("Family member not found.");
            }

            await _unitOfWork.FamilyMembers.DeleteAsync(memberId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Family member '{Name}' removed from account {AccountId}", member.Name, accountId);
        }

        public async Task<int> GetCountAsync(Guid accountId)
        {
            var members = await _unitOfWork.FamilyMembers.FindAsync(fm => fm.AccountId == accountId);
            return members.Count() + 1; // +1 for the account owner
        }
    }
}
