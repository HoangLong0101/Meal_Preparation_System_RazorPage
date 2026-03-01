using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class UpdateIngredientDtoValidator : AbstractValidator<UpdateIngredientDto>
    {
        public UpdateIngredientDtoValidator()
        {
            RuleFor(x => x.IngredientName)
                .NotEmpty()
                .WithMessage("Ingredient name is required")
                .MaximumLength(100)
                .WithMessage("Ingredient name must not exceed 100 characters");

            RuleFor(x => x.Unit)
                .NotEmpty()
                .WithMessage("Unit is required")
                .MaximumLength(20)
                .WithMessage("Unit must not exceed 20 characters");

            RuleFor(x => x.CaloPerUnit)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Calories per unit must be non-negative")
                .LessThanOrEqualTo(10000)
                .WithMessage("Calories per unit must be 10000 or less");
        }
    }
}