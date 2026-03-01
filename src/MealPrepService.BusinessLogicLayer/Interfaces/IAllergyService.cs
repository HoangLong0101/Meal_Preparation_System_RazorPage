using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces;

public interface IAllergyService
{
    Task<IEnumerable<AllergyDto>> GetAllAsync();
    Task<AllergyDto?> GetByIdAsync(Guid id);
    Task<AllergyDto> CreateAsync(CreateAllergyDto createDto);
    Task<AllergyDto> UpdateAsync(UpdateAllergyDto updateDto);
    Task DeleteAsync(Guid id);
}
