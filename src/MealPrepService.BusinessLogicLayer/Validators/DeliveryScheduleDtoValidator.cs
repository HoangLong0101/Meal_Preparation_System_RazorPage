using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class DeliveryScheduleDtoValidator : AbstractValidator<DeliveryScheduleDto>
    {
        public DeliveryScheduleDtoValidator()
        {
            RuleFor(x => x.OrderId)
                .NotEmpty()
                .WithMessage("Order ID is required");

            RuleFor(x => x.DeliveryTime)
                .NotEmpty()
                .WithMessage("Delivery time is required")
                .Must(time => time > DateTime.Now)
                .WithMessage("Delivery time must be in the future");

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Delivery address is required")
                .MaximumLength(500)
                .WithMessage("Address must not exceed 500 characters");

            RuleFor(x => x.DriverContact)
                .MaximumLength(100)
                .WithMessage("Driver contact must not exceed 100 characters");
        }
    }
}