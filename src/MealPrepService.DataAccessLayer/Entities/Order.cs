namespace MealPrepService.DataAccessLayer.Entities;

public class Order : BaseEntity
{
    public Guid AccountId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = string.Empty; // pending, pending_payment, paid, payment_failed, confirmed, delivered
    public string? VnpayTransactionId { get; set; } // For VNPAY transactions
    public DateTime? PaymentConfirmedAt { get; set; } // When payment was confirmed
    public Guid? PaymentConfirmedBy { get; set; } // Delivery man ID for COD confirmations
    
    // Navigation properties
    public Account Account { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public DeliverySchedule? DeliverySchedule { get; set; }
}
