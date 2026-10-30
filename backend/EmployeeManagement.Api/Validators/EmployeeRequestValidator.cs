using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using FluentValidation;

namespace EmployeeManagement.Api.Validators;

public class EmployeeRequestValidator : AbstractValidator<EmployeeRequest>
{
    public EmployeeRequestValidator() : this(idNumbersOptional: false) { }

    protected internal EmployeeRequestValidator(bool idNumbersOptional)
    {
        RuleFor(x => x.FullName)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Full name is required.")
            .MaximumLength(100)
            .Matches("^[A-Za-z ]+$").WithMessage("Full name can only contain letters and spaces.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(150)
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Enter a valid email address.");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(@"^\d{10}$").WithMessage("Mobile number must be exactly 10 digits.");

        RuleFor(x => x.Aadhaar)
            .NotEmpty().WithMessage("Aadhaar number is required.")
            .Must(a => System.Text.RegularExpressions.Regex.IsMatch(SensitiveDataProtector.NormalizeAadhaar(a), @"^\d{12}$"))
            .WithMessage("Aadhaar must be exactly 12 digits (XXXX-XXXX-XXXX).")
            .When(x => !idNumbersOptional || !string.IsNullOrEmpty(x.Aadhaar));

        RuleFor(x => x.Pan)
            .NotEmpty().WithMessage("PAN is required.")
            .Matches("^[A-Z]{5}[0-9]{4}[A-Z]$").WithMessage("PAN must be in the format ABCDE1234F.")
            .When(x => !idNumbersOptional || !string.IsNullOrEmpty(x.Pan));

        RuleFor(x => x.DateOfBirth)
            .NotNull().WithMessage("Date of birth is required.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.Today)).WithMessage("Date of birth cannot be in the future.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => Genders.All.Contains(g)).WithMessage("Gender must be Male, Female or Other.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300);

        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Department is required.");
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("Role is required.");

        RuleFor(x => x.JoiningDate).NotNull().WithMessage("Joining date is required.");

        RuleFor(x => x.AnnualSalary)
            .GreaterThan(0).WithMessage("Annual salary must be greater than zero.")
            .LessThan(10_000_000_000m).WithMessage("Annual salary is too large.")
            .PrecisionScale(12, 2, true).WithMessage("Annual salary can have at most 2 decimal places.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => EmployeeStatus.All.Contains(s)).WithMessage("Status must be Active, Inactive or OnLeave.");
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        Include(new EmployeeRequestValidator(idNumbersOptional: true));
    }
}
