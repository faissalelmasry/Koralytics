using FluentValidation;
using Koralytics.Application.DTOs.SystemAdmin;

namespace Koralytics.Application.Validators.SystemAdmin
{
    public class UserListRequestValidator : AbstractValidator<UserListRequestDto>
    {
        public UserListRequestValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("Page number must be greater than 0.");
            RuleFor(x => x.PageSize).GreaterThan(0).WithMessage("Page size must be greater than 0.")
                                    .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.");
        }
    }

    public class UpdateUserRolesValidator : AbstractValidator<UpdateUserRolesDto>
    {
        public UpdateUserRolesValidator()
        {
            RuleFor(x => x.Roles)
                .NotNull().WithMessage("Roles list cannot be null.")
                .Must(r => r != null && r.Count > 0).WithMessage("At least one role must be provided.");
        }
    }
}
