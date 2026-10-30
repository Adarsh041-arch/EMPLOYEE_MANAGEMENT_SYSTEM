using EmployeeManagement.Api.DTOs;
using FluentValidation;

namespace EmployeeManagement.Api.Validators;

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.NewDepartmentId).GreaterThan(0).WithMessage("New department is required.");
        RuleFor(x => x.TransferDate).NotNull().WithMessage("Transfer date is required.");
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r)).WithMessage("Transfer reason is required.")
            .MaximumLength(300);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
