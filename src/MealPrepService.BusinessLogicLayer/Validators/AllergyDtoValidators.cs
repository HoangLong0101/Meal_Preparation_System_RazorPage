using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class CreateAllergyDtoValidator : AbstractValidator<CreateAllergyDto>
    {
        public CreateAllergyDtoValidator()
        {
            RuleFor(x => x.AllergyName)
                .NotEmpty()
                .WithMessage("Allergy name is required")
                .MaximumLength(100)
                .WithMessage("Allergy name must not exceed 100 characters")
                .Matches("^[a-zA-Z\\s\\-]+$")
                .WithMessage("Allergy name can only contain letters, spaces, and hyphens");
        }
    }

    public class UpdateAllergyDtoValidator : AbstractValidator<UpdateAllergyDto>
    {
        public UpdateAllergyDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Allergy ID is required");

            RuleFor(x => x.AllergyName)
                .NotEmpty()
                .WithMessage("Allergy name is required")
                .MaximumLength(100)
                .WithMessage("Allergy name must not exceed 100 characters")
                .Matches("^[a-zA-Z\\s\\-]+$")
                .WithMessage("Allergy name can only contain letters, spaces, and hyphens");
        }
    }
}