using FluentValidation;
using Koralytics.Application.DTOs.Match;

namespace Koralytics.Application.Validators.Match
{
    public class CreateSessionMatchValidator : AbstractValidator<CreateSessionMatchDto>
    {
        public CreateSessionMatchValidator()
        {
            RuleFor(x => x.SessionId)
                .GreaterThan(0).WithMessage("SessionId must be greater than 0.");

            RuleFor(x => x.HomePlayers)
                .NotEmpty().WithMessage("HomePlayers must contain at least one player.");

            RuleFor(x => x.AwayPlayers)
                .NotEmpty().WithMessage("AwayPlayers must contain at least one player.");

            RuleFor(x => x)
                .Must(x => !x.HomePlayers.Any(h => x.AwayPlayers.Any(a => a.PlayerId == h.PlayerId)))
                .WithMessage("A player cannot be on both home and away sides.");

            RuleFor(x => x.Format)
                .IsInEnum().WithMessage("Invalid match format.");

            RuleFor(x => x.MatchDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Match date must be in the future.");

            RuleFor(x => x.Location)
                .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");

            RuleForEach(x => x.HomePlayers).ChildRules(player =>
            {
                player.RuleFor(p => p.PlayerId)
                    .GreaterThan(0).WithMessage("PlayerId must be greater than 0.");

                player.RuleFor(p => p.JerseyNumber)
                    .InclusiveBetween(1, 99)
                    .When(p => p.JerseyNumber.HasValue)
                    .WithMessage("Jersey number must be between 1 and 99.");
            });

            RuleForEach(x => x.AwayPlayers).ChildRules(player =>
            {
                player.RuleFor(p => p.PlayerId)
                    .GreaterThan(0).WithMessage("PlayerId must be greater than 0.");

                player.RuleFor(p => p.JerseyNumber)
                    .InclusiveBetween(1, 99)
                    .When(p => p.JerseyNumber.HasValue)
                    .WithMessage("Jersey number must be between 1 and 99.");
            });

            RuleFor(x => x)
                .Must(x =>
                {
                    var formatStartingCount = x.Format switch
                    {
                        Domain.Enums.MatchFormat.FiveSide => 5,
                        Domain.Enums.MatchFormat.SevenSide => 7,
                        Domain.Enums.MatchFormat.ElevenSide => 11,
                        _ => throw new ValidationException($"Invalid match format: {x.Format}")
                    };

                    return x.HomePlayers.Count(p => p.IsStarting) == formatStartingCount
                        && x.AwayPlayers.Count(p => p.IsStarting) == formatStartingCount;
                })
                .WithMessage(x =>
                {
                    var count = x.Format switch
                    {
                        Domain.Enums.MatchFormat.FiveSide => 5,
                        Domain.Enums.MatchFormat.SevenSide => 7,
                        Domain.Enums.MatchFormat.ElevenSide => 11,
                        _ => 0
                    };
                    return $"Each side must have exactly {count} starting players.";
                });
        }
    }
}
