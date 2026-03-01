using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class AIConfigurationService : IAIConfigurationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AIConfigurationService> _logger;

        public AIConfigurationService(
            IUnitOfWork unitOfWork,
            ILogger<AIConfigurationService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AIConfiguration> GetConfigurationAsync()
        {
            var configs = await _unitOfWork.AIConfigurations.GetAllAsync();
            var config = configs.FirstOrDefault();

            if (config == null)
            {
                // Create default configuration
                config = new AIConfiguration
                {
                    IsEnabled = true,
                    MinRecommendations = 5,
                    MaxRecommendations = 10,
                    RecommendationCacheDurationMinutes = 60,
                    ConfigurationJson = "{}",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _unitOfWork.AIConfigurations.AddAsync(config);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Default AI configuration created");
            }

            return config;
        }

        public async Task<AIConfiguration> UpdateConfigurationAsync(AIConfiguration config, string adminUsername)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrWhiteSpace(adminUsername))
            {
                throw new ArgumentException("Admin username is required", nameof(adminUsername));
            }

            var existing = await _unitOfWork.AIConfigurations.GetByIdAsync(config.Id);
            if (existing == null)
            {
                throw new NotFoundException($"AI configuration with ID {config.Id} not found");
            }

            // Validate configuration values
            if (config.MinRecommendations < 1 || config.MinRecommendations > config.MaxRecommendations)
            {
                throw new ValidationException("MinRecommendations must be between 1 and MaxRecommendations");
            }

            if (config.MaxRecommendations < config.MinRecommendations || config.MaxRecommendations > 20)
            {
                throw new ValidationException("MaxRecommendations must be between MinRecommendations and 20");
            }

            if (config.RecommendationCacheDurationMinutes < 0)
            {
                throw new ValidationException("RecommendationCacheDurationMinutes must be non-negative");
            }

            existing.IsEnabled = config.IsEnabled;
            existing.MinRecommendations = config.MinRecommendations;
            existing.MaxRecommendations = config.MaxRecommendations;
            existing.RecommendationCacheDurationMinutes = config.RecommendationCacheDurationMinutes;
            existing.ConfigurationJson = config.ConfigurationJson ?? "{}";
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = adminUsername;

            await _unitOfWork.AIConfigurations.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("AI configuration updated by {AdminUsername}. IsEnabled: {IsEnabled}", 
                adminUsername, existing.IsEnabled);

            return existing;
        }

        public async Task<IEnumerable<AIOperationLog>> GetOperationLogsAsync(int pageNumber, int pageSize, string? filterStatus = null)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1 || pageSize > 100)
            {
                pageSize = 50;
            }

            var query = _unitOfWork.AIOperationLogs.AsQueryable();
            
            // Apply status filter if provided
            if (!string.IsNullOrWhiteSpace(filterStatus))
            {
                query = query.Where(log => log.Status.Equals(filterStatus, StringComparison.OrdinalIgnoreCase));
            }

            // Order by timestamp descending and apply pagination
            var pagedLogs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return pagedLogs;
        }
    }
}
