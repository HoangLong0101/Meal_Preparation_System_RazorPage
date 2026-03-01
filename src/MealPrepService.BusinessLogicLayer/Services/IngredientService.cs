using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.BusinessLogicLayer.Services
{
    /// <summary>
    /// Service for ingredient management operations
    /// </summary>
    public class IngredientService : IIngredientService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly MealPrepDbContext _context;
        private readonly ILogger<IngredientService> _logger;

        public IngredientService(IUnitOfWork unitOfWork, MealPrepDbContext context, ILogger<IngredientService> logger)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _logger = logger;
        }

        public async Task<IngredientDto> CreateIngredientAsync(CreateIngredientDto dto)
        {
            _logger.LogInformation("Creating ingredient with name: {IngredientName}", dto.IngredientName);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.IngredientName))
            {
                throw new BusinessException("Ingredient name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Unit))
            {
                throw new BusinessException("Unit is required");
            }

            if (dto.CaloPerUnit < 0)
            {
                throw new BusinessException("Calories per unit must be non-negative");
            }

            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = dto.IngredientName.Trim(),
                Unit = dto.Unit.Trim(),
                CaloPerUnit = dto.CaloPerUnit,
                IsAllergen = dto.IsAllergen,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Ingredients.AddAsync(ingredient);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ingredient created successfully with ID: {IngredientId}", ingredient.Id);

            return MapToDto(ingredient);
        }

        public async Task<IngredientDto> GetByIdAsync(Guid ingredientId)
        {
            var ingredient = await _context.Ingredients
                .Include(i => i.Allergies)
                .FirstOrDefaultAsync(i => i.Id == ingredientId);
            
            if (ingredient == null)
            {
                throw new BusinessException("Ingredient not found");
            }

            return MapToDto(ingredient);
        }

        public async Task<IEnumerable<IngredientDto>> GetAllAsync()
        {
            var ingredients = await _context.Ingredients
                .Include(i => i.Allergies)
                .ToListAsync();
            return ingredients.Select(MapToDto);
        }

        public async Task<IngredientDto> UpdateIngredientAsync(Guid ingredientId, UpdateIngredientDto dto)
        {
            _logger.LogInformation("Updating ingredient with ID: {IngredientId}", ingredientId);

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.IngredientName))
            {
                throw new BusinessException("Ingredient name is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Unit))
            {
                throw new BusinessException("Unit is required");
            }

            if (dto.CaloPerUnit < 0)
            {
                throw new BusinessException("Calories per unit must be non-negative");
            }

            var ingredient = await _context.Ingredients
                .Include(i => i.Allergies)
                .FirstOrDefaultAsync(i => i.Id == ingredientId);
            
            if (ingredient == null)
            {
                throw new BusinessException("Ingredient not found");
            }

            ingredient.IngredientName = dto.IngredientName.Trim();
            ingredient.Unit = dto.Unit.Trim();
            ingredient.CaloPerUnit = dto.CaloPerUnit;
            ingredient.IsAllergen = dto.IsAllergen;
            ingredient.UpdatedAt = DateTime.UtcNow;

            // Update allergies if provided
            if (dto.AllergyIds != null)
            {
                // Clear existing allergies
                ingredient.Allergies.Clear();

                // Add new allergies
                if (dto.AllergyIds.Any())
                {
                    var allergies = await _context.Allergies
                        .Where(a => dto.AllergyIds.Contains(a.Id))
                        .ToListAsync();
                    
                    foreach (var allergy in allergies)
                    {
                        ingredient.Allergies.Add(allergy);
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Ingredient updated successfully with ID: {IngredientId}", ingredientId);

            return MapToDto(ingredient);
        }

        public async Task DeleteIngredientAsync(Guid ingredientId)
        {
            _logger.LogInformation("Deleting ingredient with ID: {IngredientId}", ingredientId);

            var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(ingredientId);
            
            if (ingredient == null)
            {
                throw new BusinessException("Ingredient not found");
            }

            // Check if ingredient is used in any recipes
            var isUsedInRecipes = await _unitOfWork.RecipeIngredients
                .AnyAsync(ri => ri.IngredientId == ingredientId);
            
            if (isUsedInRecipes)
            {
                throw new BusinessException("Cannot delete ingredient that is used in recipes");
            }

            await _unitOfWork.Ingredients.DeleteAsync(ingredientId);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Ingredient deleted successfully with ID: {IngredientId}", ingredientId);
        }

        public async Task<IEnumerable<IngredientDto>> GetAllergensAsync()
        {
            var allergens = await _unitOfWork.Ingredients.FindAsync(i => i.IsAllergen);
            return allergens.Select(MapToDto);
        }

        private static IngredientDto MapToDto(Ingredient ingredient)
        {
            return new IngredientDto
            {
                Id = ingredient.Id,
                IngredientName = ingredient.IngredientName,
                Unit = ingredient.Unit,
                CaloPerUnit = ingredient.CaloPerUnit,
                IsAllergen = ingredient.IsAllergen,
                Allergies = ingredient.Allergies?.Select(a => new AllergyDto
                {
                    Id = a.Id,
                    AllergyName = a.AllergyName
                }).ToList() ?? new List<AllergyDto>()
            };
        }
    }
}