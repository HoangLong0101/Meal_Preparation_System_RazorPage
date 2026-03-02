using MealPrepService.DataAccessLayer.Repositories;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Services;
using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================
// Add services to container
// ==========================

builder.Services.AddRazorPages();

// ADD DbContext
builder.Services.AddDbContext<MealPrepDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ==========================
// Register Repositories
// ==========================

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<IMealPlanRepository, MealPlanRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDailyMenuRepository, DailyMenuRepository>();
builder.Services.AddScoped<IFridgeItemRepository, FridgeItemRepository>();

// ==========================
// Register Business Services
// ==========================

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IRevenueService, RevenueService>();
builder.Services.AddScoped<IAIConfigurationService, AIConfigurationService>();
builder.Services.AddScoped<IVnpayService, VnpayService>();
builder.Services.AddScoped<ICustomerProfileAnalyzer, CustomerProfileAnalyzer>();
builder.Services.AddScoped<IRecommendationEngine, AIRecommendationEngine>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<ILLMService, GeminiRecommendationService>();

// AI service dùng HttpClientFactory
builder.Services.AddHttpClient<INutritionService, NutritionService>();

// Optional generic HttpClient
builder.Services.AddHttpClient();

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// CORS (optional)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ==========================
// Configure pipeline
// ==========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", () => Results.Redirect("/Index"));

app.Run();