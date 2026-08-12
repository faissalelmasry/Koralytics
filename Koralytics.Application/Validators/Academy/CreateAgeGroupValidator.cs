using FluentValidation;
using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Validators.Academies
{
    public class CreateAgeGroupValidator : AbstractValidator<CreateAgeGroupDto>
    {
        public CreateAgeGroupValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Age group name is required.")
                .MaximumLength(50).WithMessage("Age group name must not exceed 50 characters.");

            RuleFor(x => x.MinAge)
                .InclusiveBetween(5, 50)
                .WithMessage("Minimum age must be between 5 and 50.");

            RuleFor(x => x.MaxAge)
                .InclusiveBetween(5, 50)
                .WithMessage("Maximum age must be between 5 and 50.")
                .GreaterThan(x => x.MinAge)
                .WithMessage("Maximum age must be greater than minimum age.");
        }
    }
}
