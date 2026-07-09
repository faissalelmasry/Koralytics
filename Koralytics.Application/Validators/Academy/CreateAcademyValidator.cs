using FluentValidation;
using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Validators.Academies
{
    public class CreateAcademyValidator : AbstractValidator<CreateAcademyDto>
    {
        public CreateAcademyValidator()
        {
            RuleFor(x => x.AcademyRequestId)
                .GreaterThan(0).WithMessage("A valid AcademyRequestId is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Academy name is required.")
                .MinimumLength(2).WithMessage("Academy name must be at least 2 characters.")
                .MaximumLength(100).WithMessage("Academy name must not exceed 100 characters.");

            RuleFor(x => x.FoundedAt)
                .NotEmpty().WithMessage("Founded date is required.")
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("Founded date cannot be in the future.");

            RuleFor(x => x.AdminUserId)
                .GreaterThan(0).WithMessage("A valid AdminUserId is required.");

            RuleFor(x => x.PrimaryColor)
                .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
                .WithMessage("PrimaryColor must be a valid hex color (e.g. #FF5733).")
                .When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));

            RuleFor(x => x.SecondaryColor)
                .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
                .WithMessage("SecondaryColor must be a valid hex color (e.g. #FF5733).")
                .When(x => !string.IsNullOrWhiteSpace(x.SecondaryColor));
        }
    }
}
