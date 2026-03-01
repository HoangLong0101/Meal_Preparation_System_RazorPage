using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class MealPlanDtoValidator : AbstractValidator<MealPlanDto>
    {
        public MealPlanDtoValidator()
        {
            RuleFor(x => x.PlanName)
                .NotEmpty()
                .WithMessage("Plan name is required")
                .MaximumLength(200)
                .WithMessage("Plan name must not exceed 200 characters");

            RuleFor(x => x.StartDate)
                .NotEmpty()
                .WithMessage("Start date is required");

            RuleFor(x => x.EndDate)
                .NotEmpty()
                .WithMessage("End date is required")
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be greater than or equal to start date");

            RuleFor(x => x.AccountId)
                .NotEmpty()
                .WithMessage("Account ID is required");

            // Validate that the date range is not too long (e.g., max 90 days)
            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= 90)
                .WithMessage("Meal plan duration cannot exceed 90 days");
        }
    }
}