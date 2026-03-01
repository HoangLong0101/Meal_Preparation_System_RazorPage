using MealPrepService.DataAccessLayer.Repositories;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Add session support (if needed for authentication/shopping cart)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register Unit of Work Pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Generic Repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register Specialized Repositories (if they have specific interfaces beyond IRepository<T>)
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<IMealPlanRepository, MealPlanRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDailyMenuRepository, DailyMenuRepository>();
builder.Services.AddScoped<IFridgeItemRepository, FridgeItemRepository>();

// Register Business Logic Layer Services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IRevenueService, RevenueService>();
builder.Services.AddScoped<IAIConfigurationService, AIConfigurationService>();
builder.Services.AddScoped<IVnpayService, VnpayService>();
builder.Services.AddScoped<ICustomerProfileAnalyzer, CustomerProfileAnalyzer>();
builder.Services.AddScoped<IRecommendationEngine, AIRecommendationEngine>();

// Register additional services (add based on your BLL)
// Uncomment and add services as they exist in your project:
// builder.Services.AddScoped<IMealService, MealService>();
// builder.Services.AddScoped<IRecipeService, RecipeService>();
// builder.Services.AddScoped<IIngredientService, IngredientService>();
// builder.Services.AddScoped<IHealthProfileService, HealthProfileService>();
// builder.Services.AddScoped<IDeliveryService, DeliveryService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();
// builder.Services.AddScoped<IMealPlanService, MealPlanService>();
// builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
// builder.Services.AddScoped<IFridgeService, FridgeService>();

// Add HTTP Client for external API calls (if needed for AI or payment services)
builder.Services.AddHttpClient();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Add CORS (if you need to support API calls from different origins)
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

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Enable static files (CSS, JS, images)
app.UseStaticFiles();

app.UseRouting();

// Enable CORS (if configured)
// app.UseCors("AllowAll");

// Enable session (if configured)
app.UseSession();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Razor Pages
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Optional: Add a default redirect
app.MapGet("/", () => Results.Redirect("/Index"));

app.Run();
