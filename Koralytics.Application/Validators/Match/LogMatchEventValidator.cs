using FluentValidation;
using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Validators.Match
{
    public class LogMatchEventValidator : AbstractValidator<LogMatchEventDto>
    {
        public LogMatchEventValidator()
        {
            RuleFor(x => x.TeamId)
                .GreaterThan(0).WithMessage("TeamId must be greater than 0.");

            RuleFor(x => x.PlayerId)
                .GreaterThan(0).WithMessage("PlayerId must be greater than 0.");

            RuleFor(x => x.EventType)
                .IsInEnum().WithMessage("Invalid match event type.");

            RuleFor(x => x.Minute)
                .InclusiveBetween(0, 130).WithMessage("Minute must be between 0 and 130.");

            RuleFor(x => x)
                .Must(x => x.PlayerId != x.AssistPlayerId)
                .When(x => x.AssistPlayerId.HasValue)
                .WithMessage("Player and assist player must be different.");
        }
    }
}
