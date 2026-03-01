using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class CreateRecipeDtoValidator : AbstractValidator<CreateRecipeDto>
    {
        public CreateRecipeDtoValidator()
        {
            RuleFor(x => x.RecipeName)
                .NotEmpty()
                .WithMessage("Recipe name is required")
                .MaximumLength(200)
                .WithMessage("Recipe name must not exceed 200 characters");

            RuleFor(x => x.Instructions)
                .NotEmpty()
                .WithMessage("Recipe instructions are required")
                .MaximumLength(5000)
                .WithMessage("Instructions must not exceed 5000 characters");

            RuleFor(x => x.TotalCalories)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Total calories must be non-negative")
                .LessThanOrEqualTo(10000)
                .WithMessage("Total calories must not exceed 10000");

            RuleFor(x => x.ProteinG)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Protein must be non-negative")
                .LessThanOrEqualTo(1000)
                .WithMessage("Protein must not exceed 1000g");

            RuleFor(x => x.FatG)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Fat must be non-negative")
                .LessThanOrEqualTo(1000)
                .WithMessage("Fat must not exceed 1000g");

            RuleFor(x => x.CarbsG)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Carbs must be non-negative")
                .LessThanOrEqualTo(1000)
                .WithMessage("Carbs must not exceed 1000g");

            When(x => x.Ingredients != null && x.Ingredients.Any(), () =>
            {
                RuleForEach(x => x.Ingredients)
                    .SetValidator(new CreateRecipeIngredientDtoValidator());
            });
        }
    }

    public class CreateRecipeIngredientDtoValidator : AbstractValidator<CreateRecipeIngredientDto>
    {
        public CreateRecipeIngredientDtoValidator()
        {
            RuleFor(x => x.IngredientName)
                .NotEmpty()
                .WithMessage("Ingredient name is required");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Ingredient amount must be greater than 0")
                .LessThanOrEqualTo(10000)
                .WithMessage("Ingredient amount must not exceed 10000");

            RuleFor(x => x.Unit)
                .NotEmpty()
                .WithMessage("Unit is required")
                .MaximumLength(50)
                .WithMessage("Unit must not exceed 50 characters");
        }
    }
}