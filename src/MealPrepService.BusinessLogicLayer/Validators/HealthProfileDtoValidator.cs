using FluentValidation;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Validators
{
    public class HealthProfileDtoValidator : AbstractValidator<HealthProfileDto>
    {
        public HealthProfileDtoValidator()
        {
            RuleFor(x => x.Age)
                .GreaterThan(0)
                .WithMessage("Age must be greater than 0")
                .LessThanOrEqualTo(150)
                .WithMessage("Age must be 150 or less");

            RuleFor(x => x.Weight)
                .GreaterThan(0)
                .WithMessage("Weight must be greater than 0")
                .LessThanOrEqualTo(1000)
                .WithMessage("Weight must be 1000 or less");

            RuleFor(x => x.Height)
                .GreaterThan(0)
                .WithMessage("Height must be greater than 0")
                .LessThanOrEqualTo(300)
                .WithMessage("Height must be 300 or less");

            RuleFor(x => x.Gender)
                .NotEmpty()
                .WithMessage("Gender is required")
                .Must(BeValidGender)
                .WithMessage("Gender must be 'Male', 'Female', or 'Other'");

            RuleFor(x => x.HealthNotes)
                .MaximumLength(1000)
                .WithMessage("Health notes must not exceed 1000 characters");
        }

        private bool BeValidGender(string gender)
        {
            var validGenders = new[] { "Male", "Female", "Other" };
            return validGenders.Contains(gender, StringComparer.OrdinalIgnoreCase);
        }
    }
}