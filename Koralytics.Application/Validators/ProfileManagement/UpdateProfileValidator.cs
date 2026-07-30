using FluentValidation;
using Koralytics.Application.DTOs.ProfileManagement;
using System.IO;
using System.Linq;

namespace Koralytics.Application.Validators.ProfileManagement
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequestDto>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

            RuleFor(x => x.Nationality)
                .MaximumLength(50).WithMessage("Nationality must not exceed 50 characters.")
                .When(x => x.Nationality != null);

            RuleFor(x => x.PreferredFoot)
                .IsInEnum().WithMessage("Invalid preferred foot value.")
                .When(x => x.PreferredFoot.HasValue);

            RuleFor(x => x.WeakFootRating)
                .InclusiveBetween(1, 5).WithMessage("Weak foot rating must be between 1 and 5.")
                .When(x => x.WeakFootRating.HasValue);

            RuleFor(x => x.PlayStyleTag)
                .MaximumLength(50).WithMessage("Play style tag must not exceed 50 characters.")
                .When(x => x.PlayStyleTag != null);

            RuleFor(x => x.HeightCm)
                .InclusiveBetween(50, 220).WithMessage("Height must be between 50 and 220 cm.")
                .When(x => x.HeightCm.HasValue);

            RuleFor(x => x.WeightKg)
                .InclusiveBetween(20, 150).WithMessage("Weight must be between 20 and 150 kg.")
                .When(x => x.WeightKg.HasValue);

            RuleFor(x => x.Positions)
                .Must(positions => positions!.Count > 0)
                .WithMessage("Positions list cannot be empty when updating positions.")
                .Must(positions => positions!.Count <= 5)
                .WithMessage("A player can have at most 5 positions.")
                .Must(positions => positions!.Count(p => p.IsPrimary) == 1)
                .WithMessage("Exactly one position must be marked as primary when updating positions.")
                .Must(positions => positions!.Select(p => p.Position.Trim().ToUpperInvariant()).Distinct().Count() == positions!.Count)
                .WithMessage("Duplicate positions are not allowed.")
                .When(x => x.Positions != null);

            RuleForEach(x => x.Positions)
                .ChildRules(position =>
                {
                    position.RuleFor(p => p.Position)
                        .NotEmpty().WithMessage("Position code cannot be empty.")
                        .MaximumLength(10).WithMessage("Position code must not exceed 10 characters.");
                })
                .When(x => x.Positions != null);
        }
    }

    public class UpdateProfileImageValidator : AbstractValidator<UpdateProfileImageDto>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public UpdateProfileImageValidator()
        {
            RuleFor(x => x.Image)
                .NotNull().WithMessage("Image file is required.")
                .Must(file => file.Length > 0).WithMessage("Image file cannot be empty.")
                .Must(file => AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
                .WithMessage("Invalid image file format. Allowed formats: .jpg, .jpeg, .png, .webp.")
                .Must(file => file.Length <= 5 * 1024 * 1024)
                .WithMessage("Image file size must not exceed 5 MB.");
        }
    }
}
