namespace MealPrepService.BusinessLogicLayer.DTOs
{
    public class VnpayCallbackResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public string ResponseCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}