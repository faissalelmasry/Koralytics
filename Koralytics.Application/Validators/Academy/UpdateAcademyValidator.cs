using FluentValidation;
using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Validators.Academies
{
    public class UpdateAcademyValidator : AbstractValidator<UpdateAcademyDto>
    {
        public UpdateAcademyValidator()
        {
            RuleFor(x => x.Name)
                .MinimumLength(2).WithMessage("Academy name must be at least 2 characters.")
                .MaximumLength(100).WithMessage("Academy name must not exceed 100 characters.")
                .When(x => x.Name is not null);

            RuleFor(x => x.PrimaryColor)
                .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
                .WithMessage("PrimaryColor must be a valid hex color (e.g. #FF5733).")
                .When(x => !string.IsNullOrWhiteSpace(x.PrimaryColor));

            RuleFor(x => x.SecondaryColor)
                .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
                .WithMessage("SecondaryColor must be a valid hex color (e.g. #FF5733).")
                .When(x => !string.IsNullOrWhiteSpace(x.SecondaryColor));

            // At least one field must be provided for an update
            RuleFor(x => x)
                .Must(x => x.Name is not null ||
                           x.LogoUrl is not null ||
                           x.PrimaryColor is not null ||
                           x.SecondaryColor is not null)
                .WithMessage("At least one field (Name, LogoUrl, PrimaryColor, SecondaryColor) must be provided.");
        }
    }
}
