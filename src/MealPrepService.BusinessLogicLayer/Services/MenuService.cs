using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MenuService> _logger;

        public MenuService(IUnitOfWork unitOfWork, ILogger<MenuService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DailyMenuDto> CreateDailyMenuAsync(DateTime menuDate)
        {
            // Validate menu date is not in the past
            if (menuDate.Date < DateTime.UtcNow.Date)
            {
                throw new BusinessException("Menu date cannot be in the past");
            }

            // Check if menu already exists for this date
            var existingMenu = await _unitOfWork.DailyMenus.GetByDateAsync(menuDate.Date);
            if (existingMenu != null)
            {
                throw new BusinessException($"Menu already exists for date {menuDate:yyyy-MM-dd}");
            }

            // Create daily menu entity with draft status
            var dailyMenu = new DailyMenu
            {
                Id = Guid.NewGuid(),
                MenuDate = menuDate.Date,
                Status = "draft",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DailyMenus.AddAsync(dailyMenu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Daily menu created for date {MenuDate} with status draft", menuDate.Date);

            return MapToDto(dailyMenu);
        }

        public async Task<DailyMenuDto?> GetByDateAsync(DateTime date)
        {
            var dailyMenu = await _unitOfWork.DailyMenus.GetByDateAsync(date.Date);
            return dailyMenu != null ? MapToDto(dailyMenu) : null;
        }

        public async Task<IEnumerable<DailyMenuDto>> GetWeeklyMenuAsync(DateTime startDate)
        {
            var weeklyMenus = await _unitOfWork.DailyMenus.GetWeeklyMenuAsync(startDate.Date);
            return weeklyMenus.Select(MapToDto);
        }

        public async Task AddMealToMenuAsync(Guid menuId, MenuMealDto menuMealDto)
        {
            if (menuMealDto == null)
            {
                throw new ArgumentNullException(nameof(menuMealDto));
            }

            // Validate required fields
            if (menuMealDto.RecipeId == Guid.Empty)
            {
                throw new BusinessException("Recipe ID is required");
            }

            if (menuMealDto.Price <= 0)
            {
                throw new BusinessException("Price must be greater than zero");
            }

            if (menuMealDto.AvailableQuantity < 0)
            {
                throw new BusinessException("Available quantity cannot be negative");
            }

            // Verify menu exists
            var menu = await _unitOfWork.DailyMenus.GetByIdAsync(menuId);
            if (menu == null)
            {
                throw new BusinessException($"Menu with ID {menuId} not found");
            }

            // Verify recipe exists
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(menuMealDto.RecipeId);
            if (recipe == null)
            {
                throw new BusinessException($"Recipe with ID {menuMealDto.RecipeId} not found");
            }

            // Create menu meal entity
            var menuMeal = new MenuMeal
            {
                Id = Guid.NewGuid(),
                MenuId = menuId,
                RecipeId = menuMealDto.RecipeId,
                Price = menuMealDto.Price,
                AvailableQuantity = menuMealDto.AvailableQuantity,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MenuMeals.AddAsync(menuMeal);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Meal added to menu {MenuId}: Recipe {RecipeId}, Price {Price}, Quantity {Quantity}", 
                menuId, menuMealDto.RecipeId, menuMealDto.Price, menuMealDto.AvailableQuantity);
        }

        public async Task PublishMenuAsync(Guid menuId)
        {
            var menu = await _unitOfWork.DailyMenus.GetByIdAsync(menuId);
            if (menu == null)
            {
                throw new BusinessException($"Menu with ID {menuId} not found");
            }

            if (menu.Status == "active")
            {
                throw new BusinessException("Menu is already published");
            }

            // Validate menu has at least one meal
            var menuWithMeals = await _unitOfWork.DailyMenus.GetWithMealsAsync(menuId);
            if (menuWithMeals?.MenuMeals == null || !menuWithMeals.MenuMeals.Any())
            {
                throw new BusinessException("Cannot publish menu without meals");
            }

            menu.Status = "active";
            menu.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DailyMenus.UpdateAsync(menu);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Menu {MenuId} published for date {MenuDate}", menuId, menu.MenuDate);
        }

        public async Task UpdateMealQuantityAsync(Guid menuMealId, int newQuantity)
        {
            if (newQuantity < 0)
            {
                throw new BusinessException("Quantity cannot be negative");
            }

            var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(menuMealId);
            if (menuMeal == null)
            {
                throw new BusinessException($"Menu meal with ID {menuMealId} not found");
            }

            menuMeal.AvailableQuantity = newQuantity;
            menuMeal.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Menu meal {MenuMealId} quantity updated to {NewQuantity}", 
                menuMealId, newQuantity);
        }

        private DailyMenuDto MapToDto(DailyMenu dailyMenu)
        {
            var menuMeals = dailyMenu.MenuMeals?.Select(MapMenuMealToDto).ToList() ?? new List<MenuMealDto>();

            return new DailyMenuDto
            {
                Id = dailyMenu.Id,
                MenuDate = dailyMenu.MenuDate,
                Status = dailyMenu.Status,
                MenuMeals = menuMeals
            };
        }

        private MenuMealDto MapMenuMealToDto(MenuMeal menuMeal)
        {
            var recipeDto = new RecipeDto();
            if (menuMeal.Recipe != null)
            {
                recipeDto = new RecipeDto
                {
                    Id = menuMeal.Recipe.Id,
                    RecipeName = menuMeal.Recipe.RecipeName,
                    Instructions = menuMeal.Recipe.Instructions,
                    TotalCalories = menuMeal.Recipe.TotalCalories,
                    ProteinG = menuMeal.Recipe.ProteinG,
                    FatG = menuMeal.Recipe.FatG,
                    CarbsG = menuMeal.Recipe.CarbsG
                };
            }

            return new MenuMealDto
            {
                Id = menuMeal.Id,
                MenuId = menuMeal.MenuId,
                RecipeId = menuMeal.RecipeId,
                RecipeName = menuMeal.Recipe?.RecipeName ?? string.Empty,
                Price = menuMeal.Price,
                AvailableQuantity = menuMeal.AvailableQuantity,
                IsSoldOut = menuMeal.AvailableQuantity == 0,
                Recipe = recipeDto
            };
        }
    }
}