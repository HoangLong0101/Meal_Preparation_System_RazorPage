using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class FridgeItemDtoValidator : AbstractValidator<FridgeItemDto>
    {
        public FridgeItemDtoValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty()
                .WithMessage("Account ID is required");

            RuleFor(x => x.IngredientId)
                .NotEmpty()
                .WithMessage("Ingredient ID is required");

            RuleFor(x => x.CurrentAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Current amount must be non-negative");

            RuleFor(x => x.ExpiryDate)
                .NotEmpty()
                .WithMessage("Expiry date is required");

            RuleFor(x => x.IngredientName)
                .NotEmpty()
                .WithMessage("Ingredient name is required");

            RuleFor(x => x.Unit)
                .NotEmpty()
                .WithMessage("Unit is required");
        }
    }
}