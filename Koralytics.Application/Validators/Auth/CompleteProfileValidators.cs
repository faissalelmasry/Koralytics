using FluentValidation;
using Koralytics.Application.DTOs.AuthDTOs.RegisterDTOs;

namespace Koralytics.Application.Validators.Auth
{
    public class CompleteProfileBaseValidator<T> : AbstractValidator<T> where T : CompleteProfileBaseDto
    {
        public CompleteProfileBaseValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("User name is required.")
                .Matches(@"^[a-zA-Z0-9_]{3,20}$").WithMessage("Invalid user name format.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format.");
        }
    }

    public class CompleteProfileAsPlayerValidator : CompleteProfileBaseValidator<CompleteProfileAsPlayerDto>
    {
        public CompleteProfileAsPlayerValidator()
        {
            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Date of birth is required.");
        }
    }

    public class CompleteProfileAsCoachValidator : CompleteProfileBaseValidator<CompleteProfileAsCoachDto>
    {
        public CompleteProfileAsCoachValidator()
        {
        }
    }

    public class CompleteProfileAsAcademyAdminValidator : CompleteProfileBaseValidator<CompleteProfileAsAcademyAdminDto>
    {
        public CompleteProfileAsAcademyAdminValidator()
        {
            RuleFor(x => x.AcademyId)
                .GreaterThan(0).WithMessage("AcademyId is required.");
        }
    }

    public class CompleteProfileAsParentValidator : CompleteProfileBaseValidator<CompleteProfileAsParentDto>
    {
        public CompleteProfileAsParentValidator()
        {
            RuleFor(x => x.ChildPlayerId)
                .GreaterThan(0).WithMessage("ChildPlayerId is required.");
        }
    }

    public class CompleteProfileAsScouterValidator : CompleteProfileBaseValidator<CompleteProfileAsScouterDto>
    {
        public CompleteProfileAsScouterValidator()
        {
        }
    }
}
