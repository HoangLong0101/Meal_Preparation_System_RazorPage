using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVnpayService _vnpayService;
        private readonly IDeliveryService _deliveryService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IUnitOfWork unitOfWork, 
            IVnpayService vnpayService,
            IDeliveryService deliveryService,
            ILogger<OrderService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _vnpayService = vnpayService ?? throw new ArgumentNullException(nameof(vnpayService));
            _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderDto> CreateOrderAsync(Guid accountId, List<OrderItemDto> items)
        {
            if (items == null || !items.Any())
            {
                throw new BusinessException("Order must contain at least one item");
            }

            // Validate account exists
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new BusinessException($"Account with ID {accountId} not found");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Create order entity
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    OrderDate = DateTime.UtcNow,
                    Status = "pending",
                    CreatedAt = DateTime.UtcNow
                };

                decimal totalAmount = 0;
                var orderDetails = new List<OrderDetail>();

                // Process each order item
                foreach (var item in items)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new BusinessException("Order item quantity must be greater than zero");
                    }

                    // Get menu meal and validate availability
                    var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(item.MenuMealId);
                    if (menuMeal == null)
                    {
                        throw new BusinessException($"Menu meal with ID {item.MenuMealId} not found");
                    }

                    if (menuMeal.AvailableQuantity < item.Quantity)
                    {
                        throw new BusinessException($"Insufficient quantity available for menu meal {item.MenuMealId}. Available: {menuMeal.AvailableQuantity}, Requested: {item.Quantity}");
                    }

                    // Reduce available quantity (inventory reduction)
                    menuMeal.AvailableQuantity -= item.Quantity;
                    menuMeal.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);

                    // Create order detail
                    var orderDetail = new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        MenuMealId = item.MenuMealId,
                        Quantity = item.Quantity,
                        UnitPrice = menuMeal.Price,
                        CreatedAt = DateTime.UtcNow
                    };

                    orderDetails.Add(orderDetail);
                    totalAmount += menuMeal.Price * item.Quantity;
                }

                order.TotalAmount = totalAmount;
                order.OrderDetails = orderDetails;

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Order {OrderId} created for account {AccountId} with total amount {TotalAmount}", 
                    order.Id, accountId, totalAmount);

                return await MapToDtoAsync(order);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<OrderDto> ProcessPaymentAsync(Guid orderId, string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new BusinessException("Payment method is required");
            }

            var validPaymentMethods = new[] { "COD", "VNPAY" };
            if (!validPaymentMethods.Contains(paymentMethod))
            {
                throw new BusinessException($"Invalid payment method: {paymentMethod}. Valid methods are: {string.Join(", ", validPaymentMethods)}");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            if (order.Status != "pending" && order.Status != "payment_failed")
            {
                throw new BusinessException($"Order {orderId} cannot be paid. Current status: {order.Status}");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                order.PaymentMethod = paymentMethod;
                order.UpdatedAt = DateTime.UtcNow;

                if (paymentMethod == "COD")
                {
                    // For Cash on Delivery, set status to pending_payment and create delivery schedule
                    order.Status = "pending_payment";
                    
                    // Create delivery schedule immediately for COD orders
                    var deliveryDto = new DeliveryScheduleDto
                    {
                        OrderId = orderId,
                        DeliveryTime = DateTime.UtcNow.AddDays(1),
                        Address = "Customer address", // Should come from customer profile or order data
                        DriverContact = "TBD"
                    };
                    
                    await _deliveryService.CreateDeliveryScheduleAsync(orderId, deliveryDto);
                    
                    _logger.LogInformation("COD order {OrderId} set to pending_payment with delivery schedule created", orderId);
                }
                else if (paymentMethod == "VNPAY")
                {
                    // For VNPAY, the payment URL will be generated separately
                    // Status remains "pending" until payment callback is received
                    order.Status = "pending";
                    
                    _logger.LogInformation("VNPAY order {OrderId} set to pending, awaiting payment callback", orderId);
                }

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return await MapToDtoAsync(order);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<OrderDto> GetByIdAsync(Guid orderId)
        {
            var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            return await MapToDtoAsync(order);
        }

        public async Task<IEnumerable<OrderDto>> GetByAccountIdAsync(Guid accountId)
        {
            var orders = await _unitOfWork.Orders.GetByAccountIdAsync(accountId);
            var orderDtos = new List<OrderDto>();

            foreach (var order in orders)
            {
                orderDtos.Add(await MapToDtoAsync(order));
            }

            return orderDtos;
        }

        public async Task<OrderDto> ProcessVnpayCallbackAsync(VnpayCallbackDto callbackDto)
        {
            var callbackResult = await _vnpayService.ProcessCallbackAsync(callbackDto);
            
            if (!callbackResult.IsSuccess)
            {
                throw new BusinessException($"Invalid VNPAY callback: {callbackResult.Message}");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(callbackResult.OrderId);
            
            if (order == null)
            {
                throw new BusinessException("Order not found");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (callbackResult.ResponseCode == "00") // Success
                {
                    order.Status = "confirmed";
                    order.VnpayTransactionId = callbackResult.TransactionId;
                    order.PaymentConfirmedAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;
                    
                    // Create delivery schedule for successful payment
                    var deliveryDto = new DeliveryScheduleDto
                    {
                        OrderId = order.Id,
                        DeliveryTime = DateTime.UtcNow.AddDays(1),
                        Address = "Customer address", // Should come from customer profile
                        DriverContact = "TBD"
                    };
                    
                    await _deliveryService.CreateDeliveryScheduleAsync(order.Id, deliveryDto);
                    
                    _logger.LogInformation("VNPAY payment successful for order {OrderId}, transaction {TransactionId}", 
                        order.Id, callbackResult.TransactionId);
                }
                else
                {
                    order.Status = "payment_failed";
                    order.UpdatedAt = DateTime.UtcNow;
                    
                    // Restore menu meal quantities
                    var orderWithDetails = await _unitOfWork.Orders.GetWithDetailsAsync(order.Id);
                    if (orderWithDetails?.OrderDetails != null)
                    {
                        foreach (var detail in orderWithDetails.OrderDetails)
                        {
                            var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(detail.MenuMealId);
                            if (menuMeal != null)
                            {
                                menuMeal.AvailableQuantity += detail.Quantity;
                                menuMeal.UpdatedAt = DateTime.UtcNow;
                                await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);
                            }
                        }
                    }
                    
                    _logger.LogWarning("VNPAY payment failed for order {OrderId}, response code {ResponseCode}: {Message}", 
                        order.Id, callbackResult.ResponseCode, callbackResult.Message);
                }
                
                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                
                return await MapToDtoAsync(order);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<OrderDto> ConfirmCashPaymentAsync(Guid orderId, Guid deliveryManId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            
            if (order == null)
            {
                throw new BusinessException("Order not found");
            }
            
            if (order.PaymentMethod != "COD")
            {
                throw new BusinessException("Order is not a Cash on Delivery order");
            }
            
            if (order.Status != "pending_payment")
            {
                throw new BusinessException($"Cannot confirm payment for order with status: {order.Status}");
            }
            
            await _unitOfWork.BeginTransactionAsync();
            
            try
            {
                order.Status = "confirmed";
                order.PaymentConfirmedAt = DateTime.UtcNow;
                order.PaymentConfirmedBy = deliveryManId;
                order.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                
                _logger.LogInformation("Cash payment confirmed for COD order {OrderId} by delivery man {DeliveryManId}", 
                    orderId, deliveryManId);
                
                return await MapToDtoAsync(order);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new BusinessException("Status is required");
            }

            var validStatuses = new[] { "pending", "paid", "payment_failed", "confirmed", "delivered" };
            if (!validStatuses.Contains(status))
            {
                throw new BusinessException($"Invalid status: {status}. Valid statuses are: {string.Join(", ", validStatuses)}");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
        }

        private async Task<bool> ProcessPaymentWithGateway(decimal amount, string paymentMethod)
        {
            // Placeholder for payment gateway integration
            // In a real implementation, this would integrate with a payment processor
            await Task.Delay(100); // Simulate network call
            
            // For demo purposes, assume payment succeeds 90% of the time
            var random = new Random();
            return random.NextDouble() > 0.1;
        }

        private async Task<OrderDto> MapToDtoAsync(Order order)
        {
            var orderDetails = new List<OrderDetailDto>();

            if (order.OrderDetails != null)
            {
                foreach (var detail in order.OrderDetails)
                {
                    var menuMeal = detail.MenuMeal ?? await _unitOfWork.MenuMeals.GetByIdAsync(detail.MenuMealId);
                    
                    var orderDetailDto = new OrderDetailDto
                    {
                        Id = detail.Id,
                        OrderId = detail.OrderId,
                        MenuMealId = detail.MenuMealId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice
                    };

                    if (menuMeal != null)
                    {
                        orderDetailDto.MenuMeal = new MenuMealDto
                        {
                            Id = menuMeal.Id,
                            MenuId = menuMeal.MenuId,
                            RecipeId = menuMeal.RecipeId,
                            RecipeName = menuMeal.Recipe?.RecipeName ?? string.Empty,
                            Price = menuMeal.Price,
                            AvailableQuantity = menuMeal.AvailableQuantity,
                            IsSoldOut = menuMeal.AvailableQuantity == 0
                        };
                    }

                    orderDetails.Add(orderDetailDto);
                }
            }

            DeliveryScheduleDto? deliveryScheduleDto = null;
            if (order.DeliverySchedule != null)
            {
                deliveryScheduleDto = new DeliveryScheduleDto
                {
                    Id = order.DeliverySchedule.Id,
                    OrderId = order.DeliverySchedule.OrderId,
                    DeliveryTime = order.DeliverySchedule.DeliveryTime,
                    Address = order.DeliverySchedule.Address,
                    DriverContact = order.DeliverySchedule.DriverContact
                };
            }

            return new OrderDto
            {
                Id = order.Id,
                AccountId = order.AccountId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                VnpayTransactionId = order.VnpayTransactionId,
                PaymentConfirmedAt = order.PaymentConfirmedAt,
                PaymentConfirmedBy = order.PaymentConfirmedBy,
                OrderDetails = orderDetails,
                DeliverySchedule = deliveryScheduleDto
            };
        }
    }
}