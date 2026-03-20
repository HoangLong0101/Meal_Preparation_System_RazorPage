using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.BusinessLogicLayer.Services
{
    /// <summary>
    /// Service for revenue reporting and analytics operations
    /// </summary>
    public class RevenueService : IRevenueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RevenueService> _logger;

        public RevenueService(IUnitOfWork unitOfWork, ILogger<RevenueService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<RevenueReportDto> GenerateMonthlyReportAsync(int year, int month)
        {
            _logger.LogInformation("Generating monthly revenue report for {Year}-{Month}", year, month);

            // Validate input
            if (year < 2000 || year > 3000)
            {
                throw new BusinessException("Invalid year provided");
            }

            if (month < 1 || month > 12)
            {
                throw new BusinessException("Invalid month provided");
            }

            // Check if report already exists
            var existingReport = await _unitOfWork.RevenueReports
                .FindAsync(r => r.Year == year && r.Month == month);
            
            var existingReportEntity = existingReport.FirstOrDefault();
            if (existingReportEntity != null)
            {
                _logger.LogInformation("Monthly report already exists for {Year}-{Month}", year, month);
                return MapToDto(existingReportEntity);
            }

            // Calculate date range for the month
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Calculate order revenue for the month
            var totalOrderRevenue = await _unitOfWork.Orders.GetTotalRevenueByMonthAsync(year, month);

            // Count total orders for the month
            var orders = await _unitOfWork.Orders.GetByDateRangeAsync(startDate, endDate.AddDays(1));
            var totalOrdersCount = orders.Count();

            // NOTE: Subscription revenue calculation is deferred to checkpoint 2
            var totalSubscriptionRevenue = 0m;

            // Create and save the report
            var report = new RevenueReport
            {
                Id = Guid.NewGuid(),
                Month = month,
                Year = year,
                TotalSubscriptionRev = totalSubscriptionRevenue,
                TotalOrderRev = totalOrderRevenue,
                TotalOrdersCount = totalOrdersCount,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.RevenueReports.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Monthly revenue report generated successfully for {Year}-{Month}", year, month);

            return MapToDto(report);
        }

        public async Task<RevenueReportDto> GetMonthlyReportAsync(int year, int month)
        {
            // Validate input
            if (year < 2000 || year > 3000)
            {
                throw new BusinessException("Invalid year provided");
            }

            if (month < 1 || month > 12)
            {
                throw new BusinessException("Invalid month provided");
            }

            var reports = await _unitOfWork.RevenueReports
                .FindAsync(r => r.Year == year && r.Month == month);
            
            var report = reports.FirstOrDefault();
            
            if (report == null)
            {
                throw new BusinessException($"Revenue report not found for {year}-{month:D2}");
            }

            return MapToDto(report);
        }

        public async Task<decimal> GetYearlyRevenueAsync(int year)
        {
            _logger.LogInformation("Calculating yearly revenue for {Year}", year);

            // Validate input
            if (year < 2000 || year > 3000)
            {
                throw new BusinessException("Invalid year provided");
            }

            var yearlyReports = await _unitOfWork.RevenueReports
                .FindAsync(r => r.Year == year);

            var totalRevenue = yearlyReports.Sum(r => r.TotalSubscriptionRev + r.TotalOrderRev);

            _logger.LogInformation("Yearly revenue calculated: {TotalRevenue} for {Year}", totalRevenue, year);

            return totalRevenue;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            _logger.LogInformation("Generating dashboard statistics");

            // Count total customers
            var customers = await _unitOfWork.Accounts.FindAsync(a => a.Role == "Customer");
            var totalCustomers = customers.Count();

            // NOTE: Active subscription count is deferred to checkpoint 2
            var activeSubscriptions = 0;

            // Count pending orders
            var pendingOrders = await _unitOfWork.Orders.FindAsync(o => o.Status == "pending");
            var pendingOrdersCount = pendingOrders.Count();

            // Calculate today revenue
            var todayStart = DateTime.Today;
            var tomorrowStart = todayStart.AddDays(1);
            var todayOrders = await _unitOfWork.Orders.GetByDateRangeAsync(todayStart, tomorrowStart);
            var todayRevenue = todayOrders.Sum(o => o.TotalAmount);

            // Calculate current month revenue
            var currentDate = DateTime.Now;
            var currentMonthRevenue = 0m;

            try
            {
                var currentMonthReport = await GetMonthlyReportAsync(currentDate.Year, currentDate.Month);
                currentMonthRevenue = currentMonthReport.TotalRevenue;
            }
            catch (BusinessException)
            {
                // Report doesn't exist yet, calculate on the fly
                currentMonthRevenue = await _unitOfWork.Orders.GetTotalRevenueByMonthAsync(currentDate.Year, currentDate.Month);
            }

            // Calculate current year revenue
            var currentYearRevenue = 0m;
            for (int month = 1; month <= currentDate.Month; month++)
            {
                currentYearRevenue += await _unitOfWork.Orders.GetTotalRevenueByMonthAsync(currentDate.Year, month);
            }

            var stats = new DashboardStatsDto
            {
                TotalCustomers = totalCustomers,
                ActiveSubscriptions = activeSubscriptions,
                PendingOrders = pendingOrdersCount,
                TodayRevenue = todayRevenue,
                CurrentMonthRevenue = currentMonthRevenue,
                CurrentYearRevenue = currentYearRevenue
            };

            _logger.LogInformation("Dashboard statistics generated successfully");

            return stats;
        }

        public async Task<List<DishRevenueDto>> GetTopSellingDishesAsync(DateTime startDate, DateTime endDate, int topCount = 5)
        {
            if (topCount <= 0)
            {
                topCount = 5;
            }

            var orders = await _unitOfWork.Orders.GetByDateRangeAsync(startDate, endDate);
            var validOrders = orders
                .Where(o => IsRevenueStatus(o.Status))
                .ToList();

            if (!validOrders.Any())
            {
                return new List<DishRevenueDto>();
            }

            var validOrderIds = validOrders.Select(o => o.Id).ToList();
            var orderDetails = await _unitOfWork.OrderDetails.FindAsync(od => validOrderIds.Contains(od.OrderId));

            if (!orderDetails.Any())
            {
                return new List<DishRevenueDto>();
            }

            var menuMeals = await _unitOfWork.MenuMeals.GetAllAsync();
            var recipes = await _unitOfWork.Recipes.GetAllAsync();

            var menuMealToRecipe = menuMeals.ToDictionary(mm => mm.Id, mm => mm.RecipeId);
            var recipeNameMap = recipes.ToDictionary(r => r.Id, r => r.RecipeName);

            var topDishes = orderDetails
                .Where(od => menuMealToRecipe.ContainsKey(od.MenuMealId))
                .GroupBy(od => menuMealToRecipe[od.MenuMealId])
                .Select(group => new DishRevenueDto
                {
                    RecipeId = group.Key,
                    RecipeName = recipeNameMap.TryGetValue(group.Key, out var recipeName) ? recipeName : "Unknown dish",
                    TotalQuantitySold = group.Sum(item => item.Quantity),
                    TotalRevenue = group.Sum(item => item.UnitPrice * item.Quantity)
                })
                .OrderByDescending(item => item.TotalQuantitySold)
                .ThenByDescending(item => item.TotalRevenue)
                .Take(topCount)
                .ToList();

            return topDishes;
        }

        public async Task<List<RevenueTrendPointDto>> GetRevenueTrendAsync(int months = 6)
        {
            if (months <= 0)
            {
                months = 6;
            }

            var points = new List<RevenueTrendPointDto>();
            var now = DateTime.Today;

            for (var i = months - 1; i >= 0; i--)
            {
                var monthDate = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var monthRevenue = await _unitOfWork.Orders.GetTotalRevenueByMonthAsync(monthDate.Year, monthDate.Month);

                points.Add(new RevenueTrendPointDto
                {
                    Label = monthDate.ToString("MM/yyyy"),
                    Revenue = monthRevenue
                });
            }

            return points;
        }

        private static bool IsRevenueStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return status is "confirmed" or "delivered" or "paid";
        }

        private static RevenueReportDto MapToDto(RevenueReport report)
        {
            return new RevenueReportDto
            {
                Id = report.Id,
                Month = report.Month,
                Year = report.Year,
                TotalSubscriptionRevenue = report.TotalSubscriptionRev,
                TotalOrderRevenue = report.TotalOrderRev,
                TotalOrdersCount = report.TotalOrdersCount
            };
        }
    }
}