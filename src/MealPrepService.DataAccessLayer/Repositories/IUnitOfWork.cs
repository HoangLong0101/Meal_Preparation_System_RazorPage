using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Unit of Work pattern interface for managing transactions across repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Specialized repositories
        IAccountRepository Accounts { get; }
        IUserSubscriptionRepository UserSubscriptions { get; }
        IMealPlanRepository MealPlans { get; }
        IRecipeRepository Recipes { get; }
        IOrderRepository Orders { get; }
        IDailyMenuRepository DailyMenus { get; }
        IFridgeItemRepository FridgeItems { get; }

        // Generic repositories
        IRepository<HealthProfile> HealthProfiles { get; }
        IRepository<Allergy> Allergies { get; }
        IRepository<SubscriptionPackage> SubscriptionPackages { get; }
        IRepository<Meal> Meals { get; }
        IRepository<Ingredient> Ingredients { get; }
        IRepository<MenuMeal> MenuMeals { get; }
        IRepository<OrderDetail> OrderDetails { get; }
        IRepository<DeliverySchedule> DeliverySchedules { get; }
        IRepository<RevenueReport> RevenueReports { get; }
        IRepository<AIConfiguration> AIConfigurations { get; }
        
        // AIOperationLog uses int ID, not Guid, so we need direct DbSet access
        DbSet<AIOperationLog> AIOperationLogs { get; }

        // Direct DbSet access for junction entities (no BaseEntity inheritance)
        DbSet<MealRecipe> MealRecipes { get; }
        DbSet<RecipeIngredient> RecipeIngredients { get; }

        // Transaction management
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
