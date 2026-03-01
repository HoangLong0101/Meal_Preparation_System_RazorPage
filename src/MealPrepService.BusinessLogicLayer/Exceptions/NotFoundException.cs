namespace MealPrepService.BusinessLogicLayer.Exceptions
{
    public class NotFoundException : Exception
    {
        public string EntityName { get; }
        public object EntityId { get; }

        public NotFoundException(string entityName, object entityId) 
            : base($"{entityName} with ID '{entityId}' was not found.")
        {
            EntityName = entityName;
            EntityId = entityId;
        }

        public NotFoundException(string message) : base(message)
        {
            EntityName = string.Empty;
            EntityId = string.Empty;
        }

        public NotFoundException(string message, Exception innerException) 
            : base(message, innerException)
        {
            EntityName = string.Empty;
            EntityId = string.Empty;
        }
    }
}