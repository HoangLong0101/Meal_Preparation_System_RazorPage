using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services;

public class AllergyService : IAllergyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AllergyService> _logger;

    public AllergyService(IUnitOfWork unitOfWork, ILogger<AllergyService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<AllergyDto>> GetAllAsync()
    {
        try
        {
            var allergies = await _unitOfWork.Allergies.GetAllAsync();
            return allergies.Select(MapToDto).OrderBy(a => a.AllergyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all allergies");
            throw new BusinessException("Failed to retrieve allergies", ex);
        }
    }

    public async Task<AllergyDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            return allergy != null ? MapToDto(allergy) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergy {AllergyId}", id);
            throw new BusinessException($"Failed to retrieve allergy with ID {id}", ex);
        }
    }

    public async Task<AllergyDto> CreateAsync(CreateAllergyDto createDto)
    {
        try
        {
            // Validate allergy name is not empty
            if (string.IsNullOrWhiteSpace(createDto.AllergyName))
            {
                throw new ValidationException("Allergy name is required");
            }

            // Check if allergy with same name already exists
            var existingAllergies = await _unitOfWork.Allergies.FindAsync(a => 
                a.AllergyName.ToLower() == createDto.AllergyName.ToLower());
            
            if (existingAllergies.Any())
            {
                throw new ValidationException($"An allergy with the name '{createDto.AllergyName}' already exists");
            }

            var allergy = new Allergy
            {
                Id = Guid.NewGuid(),
                AllergyName = createDto.AllergyName.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Allergies.AddAsync(allergy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Allergy {AllergyName} created successfully with ID {AllergyId}", 
                allergy.AllergyName, allergy.Id);

            return MapToDto(allergy);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating allergy");
            throw new BusinessException("Failed to create allergy", ex);
        }
    }

    public async Task<AllergyDto> UpdateAsync(UpdateAllergyDto updateDto)
    {
        try
        {
            // Validate allergy name is not empty
            if (string.IsNullOrWhiteSpace(updateDto.AllergyName))
            {
                throw new ValidationException("Allergy name is required");
            }

            var allergy = await _unitOfWork.Allergies.GetByIdAsync(updateDto.Id);
            if (allergy == null)
            {
                throw new NotFoundException($"Allergy with ID {updateDto.Id} not found");
            }

            // Check if another allergy with same name already exists
            var existingAllergies = await _unitOfWork.Allergies.FindAsync(a => 
                a.AllergyName.ToLower() == updateDto.AllergyName.ToLower() && a.Id != updateDto.Id);
            
            if (existingAllergies.Any())
            {
                throw new ValidationException($"An allergy with the name '{updateDto.AllergyName}' already exists");
            }

            allergy.AllergyName = updateDto.AllergyName.Trim();
            allergy.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Allergies.UpdateAsync(allergy);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Allergy {AllergyId} updated successfully", allergy.Id);

            return MapToDto(allergy);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergy {AllergyId}", updateDto.Id);
            throw new BusinessException($"Failed to update allergy with ID {updateDto.Id}", ex);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (allergy == null)
            {
                throw new NotFoundException($"Allergy with ID {id} not found");
            }

            // Check if allergy is being used by any health profiles
            var healthProfiles = await _unitOfWork.HealthProfiles.FindAsync(hp => 
                hp.Allergies.Any(a => a.Id == id));
            
            if (healthProfiles.Any())
            {
                throw new ConstraintViolationException(
                    $"Cannot delete allergy '{allergy.AllergyName}' because it is associated with {healthProfiles.Count()} health profile(s)");
            }

            await _unitOfWork.Allergies.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Allergy {AllergyId} deleted successfully", id);
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (ConstraintViolationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting allergy {AllergyId}", id);
            throw new BusinessException($"Failed to delete allergy with ID {id}", ex);
        }
    }

    private static AllergyDto MapToDto(Allergy allergy)
    {
        return new AllergyDto
        {
            Id = allergy.Id,
            AllergyName = allergy.AllergyName
        };
    }
}
