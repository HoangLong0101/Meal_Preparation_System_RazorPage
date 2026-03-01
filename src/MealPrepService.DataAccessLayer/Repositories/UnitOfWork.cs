using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Unit of Work pattern implementation for managing transactions across repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MealPrepDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(MealPrepDbContext context)
        {
            _context = context;

            // Initialize specialized repositories
            Accounts = new AccountRepository(_context);
            UserSubscriptions = new UserSubscriptionRepository(_context);
            MealPlans = new MealPlanRepository(_context);
            Recipes = new RecipeRepository(_context);
            Orders = new OrderRepository(_context);
            DailyMenus = new DailyMenuRepository(_context);
            FridgeItems = new FridgeItemRepository(_context);

            // Initialize generic repositories
            HealthProfiles = new Repository<HealthProfile>(_context);
            Allergies = new Repository<Allergy>(_context);
            SubscriptionPackages = new Repository<SubscriptionPackage>(_context);
            Meals = new Repository<Meal>(_context);
            Ingredients = new Repository<Ingredient>(_context);
            MenuMeals = new Repository<MenuMeal>(_context);
            OrderDetails = new Repository<OrderDetail>(_context);
            DeliverySchedules = new Repository<DeliverySchedule>(_context);
            RevenueReports = new Repository<RevenueReport>(_context);
            AIConfigurations = new Repository<AIConfiguration>(_context);
        }

        // Specialized repositories
        public IAccountRepository Accounts { get; private set; }
        public IUserSubscriptionRepository UserSubscriptions { get; private set; }
        public IMealPlanRepository MealPlans { get; private set; }
        public IRecipeRepository Recipes { get; private set; }
        public IOrderRepository Orders { get; private set; }
        public IDailyMenuRepository DailyMenus { get; private set; }
        public IFridgeItemRepository FridgeItems { get; private set; }

        // Generic repositories
        public IRepository<HealthProfile> HealthProfiles { get; private set; }
        public IRepository<Allergy> Allergies { get; private set; }
        public IRepository<SubscriptionPackage> SubscriptionPackages { get; private set; }
        public IRepository<Meal> Meals { get; private set; }
        public IRepository<Ingredient> Ingredients { get; private set; }
        public IRepository<MenuMeal> MenuMeals { get; private set; }
        public IRepository<OrderDetail> OrderDetails { get; private set; }
        public IRepository<DeliverySchedule> DeliverySchedules { get; private set; }
        public IRepository<RevenueReport> RevenueReports { get; private set; }
        public IRepository<AIConfiguration> AIConfigurations { get; private set; }

        // AIOperationLog uses int ID, not Guid, so we need direct DbSet access
        public DbSet<AIOperationLog> AIOperationLogs => _context.Set<AIOperationLog>();

        // Direct DbSet access for junction entities (no BaseEntity inheritance)
        public DbSet<MealRecipe> MealRecipes => _context.Set<MealRecipe>();
        public DbSet<RecipeIngredient> RecipeIngredients => _context.Set<RecipeIngredient>();

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}