using FluentValidation;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Validators.Auth
{
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequestDto>
    {
        public ChangePasswordValidator() 
        {
            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Old password is required.");
                

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithState(x => "New password is required.")
                .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
                .MaximumLength(100).WithMessage("New password must not exceed 100 characters.")
                .Matches(@"[A-Z]+").WithMessage("New password must contain at least one uppercase letter.")
                .Matches(@"[a-z]+").WithMessage("New password must contain at least one lowercase letter.")
                .Matches(@"\d+").WithMessage("New password must contain at least one digit.")
                .Matches(@"[\W_]+").WithMessage("New password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Please confirm your new password.")
                .Equal(x => x.NewPassword).WithMessage("The confirmed password does not match the new password.");
        }
    }
}
