using FluentValidation;

namespace Koralytics.Application.Validators.Tournament
{
    public class RegisterSquadValidator : AbstractValidator<List<int>>
    {
        public RegisterSquadValidator()
        {
            RuleFor(x => x)
                .NotEmpty().WithMessage("Player list cannot be empty.")
                .Must(x => x.Count >= 5).WithMessage("Squad must have at least 5 players.")
                .Must(x => x.Distinct().Count() == x.Count)
                    .WithMessage("Duplicate players found in squad.");

            RuleForEach(x => x)
                .GreaterThan(0).WithMessage("Each player Id must be a valid positive number.");
        }
    }
}