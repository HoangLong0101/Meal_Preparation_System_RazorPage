using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class MenuMealDtoValidator : AbstractValidator<MenuMealDto>
    {
        public MenuMealDtoValidator()
        {
            RuleFor(x => x.RecipeId)
                .NotEmpty()
                .WithMessage("Recipe ID is required");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than 0")
                .LessThanOrEqualTo(999.99m)
                .WithMessage("Price must not exceed $999.99");

            RuleFor(x => x.AvailableQuantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Available quantity must be non-negative")
                .LessThanOrEqualTo(1000)
                .WithMessage("Available quantity must not exceed 1000");
        }
    }

    public class DailyMenuDtoValidator : AbstractValidator<DailyMenuDto>
    {
        public DailyMenuDtoValidator()
        {
            RuleFor(x => x.MenuDate)
                .NotEmpty()
                .WithMessage("Menu date is required")
                .Must(date => date >= DateTime.Today)
                .WithMessage("Menu date cannot be in the past");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required")
                .Must(BeValidStatus)
                .WithMessage("Status must be 'draft' or 'active'");
        }

        private bool BeValidStatus(string status)
        {
            var validStatuses = new[] { "draft", "active" };
            return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
        }
    }
}