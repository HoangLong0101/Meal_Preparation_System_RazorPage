using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Data;

public class MealPrepDbContext : DbContext
{
    public MealPrepDbContext(DbContextOptions<MealPrepDbContext> options) 
        : base(options) { }
    
    // DbSets for all entities
    public DbSet<Account> Accounts { get; set; }
    public DbSet<HealthProfile> HealthProfiles { get; set; }
    public DbSet<Allergy> Allergies { get; set; }
    public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    public DbSet<MealPlan> MealPlans { get; set; }
    public DbSet<Meal> Meals { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<MealRecipe> MealRecipes { get; set; }
    public DbSet<FridgeItem> FridgeItems { get; set; }
    public DbSet<DailyMenu> DailyMenus { get; set; }
    public DbSet<MenuMeal> MenuMeals { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<DeliverySchedule> DeliverySchedules { get; set; }
    public DbSet<RevenueReport> RevenueReports { get; set; }
    public DbSet<AIConfiguration> AIConfigurations { get; set; }
    public DbSet<AIOperationLog> AIOperationLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure primary keys for SQL Server compatibility
        ConfigurePrimaryKeys(modelBuilder);
        
        // Configure relationships, indexes, and constraints
        ConfigureAccountRelationships(modelBuilder);
        ConfigureMealPlanRelationships(modelBuilder);
        ConfigureRecipeRelationships(modelBuilder);
        ConfigureMenuRelationships(modelBuilder);
        ConfigureOrderRelationships(modelBuilder);
        ConfigureIndexes(modelBuilder);
        ConfigureConstraints(modelBuilder);
        ConfigureAIEntities(modelBuilder);
    }
    
    private void ConfigurePrimaryKeys(ModelBuilder modelBuilder)
    {
        // Configure all GUID primary keys to use non-clustered indexes for SQL Server compatibility
        // This prevents the "Column 'Id' in table is of a type that is invalid for use as a key column in an index" error
        
        modelBuilder.Entity<Account>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<HealthProfile>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<Allergy>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<SubscriptionPackage>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<UserSubscription>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<MealPlan>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<Meal>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<Recipe>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<Ingredient>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<FridgeItem>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<DailyMenu>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<MenuMeal>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<Order>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<OrderDetail>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<DeliverySchedule>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<RevenueReport>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<AIConfiguration>().HasKey(e => e.Id).IsClustered(false);
        modelBuilder.Entity<AIOperationLog>().HasKey(e => e.Id).IsClustered(false);
    }
    
    private void ConfigureAIEntities(ModelBuilder modelBuilder)
    {
        // AIOperationLog indexes for performance
        modelBuilder.Entity<AIOperationLog>()
            .HasIndex(log => log.Timestamp);
        
        modelBuilder.Entity<AIOperationLog>()
            .HasIndex(log => log.Status);
        
        modelBuilder.Entity<AIOperationLog>()
            .HasIndex(log => log.CustomerId);
        
        // AIOperationLog relationship to Account
        modelBuilder.Entity<AIOperationLog>()
            .HasOne(log => log.Customer)
            .WithMany()
            .HasForeignKey(log => log.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // AIConfiguration constraints
        modelBuilder.Entity<AIConfiguration>()
            .Property(ac => ac.UpdatedBy)
            .IsRequired()
            .HasMaxLength(200);
    }
    
    private void ConfigureAccountRelationships(ModelBuilder modelBuilder)
    {
        // One-to-one: Account to HealthProfile
        modelBuilder.Entity<Account>()
            .HasOne(a => a.HealthProfile)
            .WithOne(h => h.Account)
            .HasForeignKey<HealthProfile>(h => h.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Many-to-many: HealthProfile to Allergies
        modelBuilder.Entity<HealthProfile>()
            .HasMany(h => h.Allergies)
            .WithMany(a => a.HealthProfiles)
            .UsingEntity(j => j.ToTable("HealthProfileAllergies"));
        
        // Many-to-many: Ingredient to Allergies
        modelBuilder.Entity<Ingredient>()
            .HasMany(i => i.Allergies)
            .WithMany(a => a.Ingredients)
            .UsingEntity(j => j.ToTable("IngredientAllergies"));
        
        // One-to-many: Account to UserSubscriptions
        modelBuilder.Entity<Account>()
            .HasMany(a => a.Subscriptions)
            .WithOne(us => us.Account)
            .HasForeignKey(us => us.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // One-to-many: SubscriptionPackage to UserSubscriptions
        modelBuilder.Entity<SubscriptionPackage>()
            .HasMany(sp => sp.UserSubscriptions)
            .WithOne(us => us.Package)
            .HasForeignKey(us => us.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // One-to-many: Account to FridgeItems
        modelBuilder.Entity<Account>()
            .HasMany(a => a.FridgeItems)
            .WithOne(fi => fi.Account)
            .HasForeignKey(fi => fi.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // One-to-many: Ingredient to FridgeItems
        modelBuilder.Entity<Ingredient>()
            .HasMany(i => i.FridgeItems)
            .WithOne(fi => fi.Ingredient)
            .HasForeignKey(fi => fi.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private void ConfigureMealPlanRelationships(ModelBuilder modelBuilder)
    {
        // One-to-many: Account to MealPlans
        modelBuilder.Entity<Account>()
            .HasMany(a => a.MealPlans)
            .WithOne(mp => mp.Account)
            .HasForeignKey(mp => mp.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // One-to-many: MealPlan to Meals
        modelBuilder.Entity<MealPlan>()
            .HasMany(mp => mp.Meals)
            .WithOne(m => m.Plan)
            .HasForeignKey(m => m.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Many-to-many: Meal to Recipe through MealRecipe
        modelBuilder.Entity<MealRecipe>()
            .HasKey(mr => new { mr.MealId, mr.RecipeId });
        
        modelBuilder.Entity<MealRecipe>()
            .HasOne(mr => mr.Meal)
            .WithMany(m => m.MealRecipes)
            .HasForeignKey(mr => mr.MealId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<MealRecipe>()
            .HasOne(mr => mr.Recipe)
            .WithMany(r => r.MealRecipes)
            .HasForeignKey(mr => mr.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private void ConfigureRecipeRelationships(ModelBuilder modelBuilder)
    {
        // Composite key for RecipeIngredient
        modelBuilder.Entity<RecipeIngredient>()
            .HasKey(ri => new { ri.RecipeId, ri.IngredientId });
        
        // Many-to-many: Recipe to Ingredient through RecipeIngredient
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Recipe)
            .WithMany(r => r.RecipeIngredients)
            .HasForeignKey(ri => ri.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(ri => ri.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private void ConfigureMenuRelationships(ModelBuilder modelBuilder)
    {
        // One-to-many: DailyMenu to MenuMeals
        modelBuilder.Entity<DailyMenu>()
            .HasMany(dm => dm.MenuMeals)
            .WithOne(mm => mm.Menu)
            .HasForeignKey(mm => mm.MenuId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // One-to-many: Recipe to MenuMeals
        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.MenuMeals)
            .WithOne(mm => mm.Recipe)
            .HasForeignKey(mm => mm.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
    
    private void ConfigureOrderRelationships(ModelBuilder modelBuilder)
    {
        // One-to-many: Account to Orders
        modelBuilder.Entity<Account>()
            .HasMany(a => a.Orders)
            .WithOne(o => o.Account)
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // One-to-many: Order to OrderDetails
        modelBuilder.Entity<Order>()
            .HasMany(o => o.OrderDetails)
            .WithOne(od => od.Order)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // One-to-many: MenuMeal to OrderDetails
        modelBuilder.Entity<MenuMeal>()
            .HasMany(mm => mm.OrderDetails)
            .WithOne(od => od.MenuMeal)
            .HasForeignKey(od => od.MenuMealId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // One-to-one: Order to DeliverySchedule
        modelBuilder.Entity<Order>()
            .HasOne(o => o.DeliverySchedule)
            .WithOne(d => d.Order)
            .HasForeignKey<DeliverySchedule>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Unique index on Account.Email
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Email)
            .IsUnique();
        
        // Index on UserSubscription for active subscription queries
        modelBuilder.Entity<UserSubscription>()
            .HasIndex(us => new { us.AccountId, us.Status, us.EndDate });
        
        // Index on DailyMenu.MenuDate for date-based queries
        modelBuilder.Entity<DailyMenu>()
            .HasIndex(dm => dm.MenuDate);
        
        // Index on Order.OrderDate for reporting
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderDate);
        
        // Index on FridgeItem for account queries
        modelBuilder.Entity<FridgeItem>()
            .HasIndex(fi => new { fi.AccountId, fi.ExpiryDate });
    }
    
    private void ConfigureConstraints(ModelBuilder modelBuilder)
    {
        // Email validation constraint
        modelBuilder.Entity<Account>()
            .Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        modelBuilder.Entity<Account>()
            .Property(a => a.PasswordHash)
            .IsRequired();
        
        modelBuilder.Entity<Account>()
            .Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(200);
        
        modelBuilder.Entity<Account>()
            .Property(a => a.Role)
            .IsRequired()
            .HasMaxLength(50);
        
        // Positive value constraints for HealthProfile (no precision for float types in SQL Server)
        modelBuilder.Entity<HealthProfile>()
            .Property(h => h.Gender)
            .IsRequired()
            .HasMaxLength(20);
        
        // Decimal precision for money
        modelBuilder.Entity<SubscriptionPackage>()
            .Property(sp => sp.Price)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<SubscriptionPackage>()
            .Property(sp => sp.PackageName)
            .IsRequired()
            .HasMaxLength(100);
        
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.UnitPrice)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<MenuMeal>()
            .Property(mm => mm.Price)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<RevenueReport>()
            .Property(rr => rr.TotalSubscriptionRev)
            .HasPrecision(10, 2);
        
        modelBuilder.Entity<RevenueReport>()
            .Property(rr => rr.TotalOrderRev)
            .HasPrecision(10, 2);
        
        // String length constraints
        modelBuilder.Entity<Recipe>()
            .Property(r => r.RecipeName)
            .IsRequired()
            .HasMaxLength(200);
        
        modelBuilder.Entity<Ingredient>()
            .Property(i => i.IngredientName)
            .IsRequired()
            .HasMaxLength(100);
        
        modelBuilder.Entity<Ingredient>()
            .Property(i => i.Unit)
            .IsRequired()
            .HasMaxLength(50);
        
        modelBuilder.Entity<Allergy>()
            .Property(a => a.AllergyName)
            .IsRequired()
            .HasMaxLength(100);
        
        modelBuilder.Entity<MealPlan>()
            .Property(mp => mp.PlanName)
            .IsRequired()
            .HasMaxLength(200);
        
        modelBuilder.Entity<Meal>()
            .Property(m => m.MealType)
            .IsRequired()
            .HasMaxLength(50);
        
        modelBuilder.Entity<DailyMenu>()
            .Property(dm => dm.Status)
            .IsRequired()
            .HasMaxLength(50);
        
        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(50);
        
        modelBuilder.Entity<UserSubscription>()
            .Property(us => us.Status)
            .IsRequired()
            .HasMaxLength(50);
        
        modelBuilder.Entity<DeliverySchedule>()
            .Property(ds => ds.Address)
            .IsRequired()
            .HasMaxLength(500);
        
        modelBuilder.Entity<DeliverySchedule>()
            .Property(ds => ds.DriverContact)
            .IsRequired()
            .HasMaxLength(100);
    }
}
