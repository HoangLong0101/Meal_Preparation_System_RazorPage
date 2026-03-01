namespace MealPrepService.BusinessLogicLayer.Exceptions
{
    public class ConstraintViolationException : Exception
    {
        public string ConstraintName { get; }

        public ConstraintViolationException(string constraintName, string message) 
            : base(message)
        {
            ConstraintName = constraintName;
        }

        public ConstraintViolationException(string message) : base(message)
        {
            ConstraintName = string.Empty;
        }

        public ConstraintViolationException(string message, Exception innerException) 
            : base(message, innerException)
        {
            ConstraintName = string.Empty;
        }
    }
}