using FluentValidation;
using Koralytics.Application.DTOs.Academies;

namespace Koralytics.Application.Validators.Academies
{
    public class AddLocationValidator : AbstractValidator<AddLocationDto>
    {
        public AddLocationValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Location name is required.")
                .MaximumLength(100).WithMessage("Location name must not exceed 100 characters.");

            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Address is required.")
                .MaximumLength(250).WithMessage("Address must not exceed 250 characters.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100).WithMessage("City must not exceed 100 characters.");
        }
    }
}
