using FluentValidation;
using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Validators.Academies
{
    public class CreateTeamValidator : AbstractValidator<CreateTeamDto>
    {
        public CreateTeamValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Team name is required.")
                .MinimumLength(2).WithMessage("Team name must be at least 2 characters.")
                .MaximumLength(100).WithMessage("Team name must not exceed 100 characters.");

            RuleFor(x => x.AgeGroupId)
                .GreaterThan(0).WithMessage("A valid AgeGroupId is required.");

            RuleFor(x => x.LocationId)
                .GreaterThan(0).WithMessage("A valid LocationId is required.");
        }
    }
}
