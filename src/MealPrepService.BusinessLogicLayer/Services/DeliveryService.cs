using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(IUnitOfWork unitOfWork, ILogger<DeliveryService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DeliveryScheduleDto> CreateDeliveryScheduleAsync(Guid orderId, DeliveryScheduleDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            // Validate order exists and is in confirmed status
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            if (order.Status != "confirmed")
            {
                throw new BusinessException($"Cannot create delivery schedule for order {orderId}. Order status must be 'confirmed', current status: {order.Status}");
            }

            // Check if delivery schedule already exists for this order
            var existingDelivery = await _unitOfWork.DeliverySchedules.FindAsync(d => d.OrderId == orderId);
            if (existingDelivery.Any())
            {
                throw new BusinessException($"Delivery schedule already exists for order {orderId}");
            }

            // Validate delivery time is in the future
            if (dto.DeliveryTime <= DateTime.UtcNow)
            {
                throw new BusinessException("Delivery time must be in the future");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Address))
            {
                throw new BusinessException("Delivery address is required");
            }

            // Create delivery schedule entity
            var deliverySchedule = new DeliverySchedule
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                DeliveryTime = dto.DeliveryTime,
                Address = dto.Address.Trim(),
                DriverContact = dto.DriverContact?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DeliverySchedules.AddAsync(deliverySchedule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Delivery schedule created for order {OrderId} with delivery time {DeliveryTime}", 
                orderId, dto.DeliveryTime);

            return MapToDto(deliverySchedule);
        }

        public async Task<IEnumerable<DeliveryScheduleDto>> GetByAccountIdAsync(Guid accountId)
        {
            // Get all orders for the account
            var orders = await _unitOfWork.Orders.GetByAccountIdAsync(accountId);
            var orderIds = orders.Select(o => o.Id).ToList();

            if (!orderIds.Any())
            {
                return new List<DeliveryScheduleDto>();
            }

            // Get delivery schedules for these orders
            var deliverySchedules = await _unitOfWork.DeliverySchedules.FindAsync(d => orderIds.Contains(d.OrderId));
            
            var deliveryDtos = new List<DeliveryScheduleDto>();
            foreach (var delivery in deliverySchedules)
            {
                var order = orders.FirstOrDefault(o => o.Id == delivery.OrderId);
                var dto = MapToDto(delivery);
                
                if (order != null)
                {
                    dto.Order = new OrderDto
                    {
                        Id = order.Id,
                        AccountId = order.AccountId,
                        OrderDate = order.OrderDate,
                        TotalAmount = order.TotalAmount,
                        PaymentMethod = order.PaymentMethod,
                        Status = order.Status
                    };
                }
                
                deliveryDtos.Add(dto);
            }

            return deliveryDtos.OrderBy(d => d.DeliveryTime);
        }

        public async Task<IEnumerable<DeliveryScheduleDto>> GetByDeliveryManAsync(Guid deliveryManId)
        {
            // Validate delivery man exists and has correct role
            var deliveryMan = await _unitOfWork.Accounts.GetByIdAsync(deliveryManId);
            if (deliveryMan == null)
            {
                throw new BusinessException($"Account with ID {deliveryManId} not found");
            }

            if (deliveryMan.Role != "DeliveryMan")
            {
                throw new BusinessException($"Account {deliveryManId} is not a delivery man. Current role: {deliveryMan.Role}");
            }

            // For now, we'll return all delivery schedules since we don't have a direct assignment mechanism
            // In a real implementation, there would be a DeliveryManId field in DeliverySchedule
            var allDeliveries = await _unitOfWork.DeliverySchedules.GetAllAsync();
            
            var deliveryDtos = new List<DeliveryScheduleDto>();
            foreach (var delivery in allDeliveries)
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(delivery.OrderId);
                var dto = MapToDto(delivery);
                
                if (order != null)
                {
                    dto.Order = new OrderDto
                    {
                        Id = order.Id,
                        AccountId = order.AccountId,
                        OrderDate = order.OrderDate,
                        TotalAmount = order.TotalAmount,
                        PaymentMethod = order.PaymentMethod,
                        Status = order.Status
                    };
                }
                
                deliveryDtos.Add(dto);
            }

            return deliveryDtos.OrderBy(d => d.DeliveryTime);
        }

        public async Task CompleteDeliveryAsync(Guid deliveryId)
        {
            var deliverySchedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(deliveryId);
            if (deliverySchedule == null)
            {
                throw new BusinessException($"Delivery schedule with ID {deliveryId} not found");
            }

            // Get the associated order
            var order = await _unitOfWork.Orders.GetByIdAsync(deliverySchedule.OrderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {deliverySchedule.OrderId} not found");
            }

            if (order.Status == "delivered")
            {
                throw new BusinessException($"Order {deliverySchedule.OrderId} is already marked as delivered");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Update order status to delivered
                order.Status = "delivered";
                order.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Delivery {DeliveryId} completed for order {OrderId}", 
                    deliveryId, deliverySchedule.OrderId);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task UpdateDeliveryTimeAsync(Guid deliveryId, DateTime newTime)
        {
            // Validate delivery time is in the future
            if (newTime <= DateTime.UtcNow)
            {
                throw new BusinessException("Delivery time must be in the future");
            }

            var deliverySchedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(deliveryId);
            if (deliverySchedule == null)
            {
                throw new BusinessException($"Delivery schedule with ID {deliveryId} not found");
            }

            // Check if the associated order is still in a state that allows delivery time updates
            var order = await _unitOfWork.Orders.GetByIdAsync(deliverySchedule.OrderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {deliverySchedule.OrderId} not found");
            }

            if (order.Status == "delivered")
            {
                throw new BusinessException($"Cannot update delivery time for order {deliverySchedule.OrderId} - order is already delivered");
            }

            deliverySchedule.DeliveryTime = newTime;
            deliverySchedule.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DeliverySchedules.UpdateAsync(deliverySchedule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Delivery time updated for delivery {DeliveryId} to {NewTime}", 
                deliveryId, newTime);
        }

        private DeliveryScheduleDto MapToDto(DeliverySchedule deliverySchedule)
        {
            return new DeliveryScheduleDto
            {
                Id = deliverySchedule.Id,
                OrderId = deliverySchedule.OrderId,
                DeliveryTime = deliverySchedule.DeliveryTime,
                Address = deliverySchedule.Address,
                DriverContact = deliverySchedule.DriverContact
            };
        }
    }
}