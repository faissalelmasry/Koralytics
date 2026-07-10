using FluentValidation;
using Koralytics.Application.DTOs.Tournament;

namespace Koralytics.Application.Validators.Tournament
{
    public class CreateTournamentValidator : AbstractValidator<CreateTournamentDto>
    {
        public CreateTournamentValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tournament name is required.")
                .MinimumLength(3).WithMessage("Tournament name must be at least 3 characters.")
                .MaximumLength(150).WithMessage("Tournament name must not exceed 150 characters.");

            RuleFor(x => x.AgeGroupId)
                .GreaterThan(0).WithMessage("A valid AgeGroup must be selected.");

            RuleFor(x => x.Format)
                .IsInEnum().WithMessage("Invalid match format. Must be FiveSide, SevenSide, or ElevenSide.");

            RuleFor(x => x.Structure)
                .IsInEnum().WithMessage("Invalid tournament structure. Must be Knockout, GroupAndKnockout, or League.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required.")
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
        }
    }
}