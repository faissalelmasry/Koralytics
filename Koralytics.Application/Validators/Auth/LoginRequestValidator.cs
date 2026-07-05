using FluentValidation;

using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koralytics.Application.Validators.Auth
{
    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator() 
        {
            RuleFor(x => x.EmailOrUserName)
                .NotEmpty().WithMessage("Email or username is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
                
        }
    }
}
