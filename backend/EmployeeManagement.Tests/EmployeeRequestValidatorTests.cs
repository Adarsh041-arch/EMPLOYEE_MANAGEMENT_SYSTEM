using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Validators;

namespace EmployeeManagement.Tests;

public class EmployeeRequestValidatorTests
{
    private readonly EmployeeRequestValidator _validator = new();

    private static EmployeeRequest ValidRequest() => new()
    {
        FullName = "Rahul Sharma",
        Email = "rahul.sharma@example.com",
        Mobile = "9876543210",
        Gender = "Male",
        Aadhaar = "1234-5678-9012",
        Pan = "ABCDE1234F",
        DateOfBirth = new DateOnly(1998, 5, 14),
        Address = "12 MG Road, Pune",
        DepartmentId = 1,
        RoleId = 1,
        JoiningDate = new DateOnly(2023, 1, 10),
        AnnualSalary = 450000,
        Status = "Active"
    };

    private List<string> ErrorsFor(EmployeeRequest request) =>
        _validator.Validate(request).Errors.Select(e => e.PropertyName).ToList();

    [Fact]
    public void Valid_request_passes()
    {
        Assert.Empty(ErrorsFor(ValidRequest()));
    }

    [Fact]
    public void Aadhaar_without_dashes_is_accepted()
    {
        var r = ValidRequest();
        r.Aadhaar = "123456789012";
        Assert.Empty(ErrorsFor(r));
    }

    [Theory]
    [InlineData("12345678901")]
    [InlineData("1234-5678-901A")]
    [InlineData("1234567890123")]
    [InlineData("")]
    public void Bad_aadhaar_is_rejected(string aadhaar)
    {
        var r = ValidRequest();
        r.Aadhaar = aadhaar;
        Assert.Contains(nameof(EmployeeRequest.Aadhaar), ErrorsFor(r));
    }

    [Theory]
    [InlineData("abcde1234f")]
    [InlineData("ABCD1234FF")]
    [InlineData("ABCDE12345")]
    [InlineData("")]
    public void Bad_pan_is_rejected(string pan)
    {
        var r = ValidRequest();
        r.Pan = pan;
        Assert.Contains(nameof(EmployeeRequest.Pan), ErrorsFor(r));
    }

    [Theory]
    [InlineData("987654321")]
    [InlineData("98765432100")]
    [InlineData("98765abcde")]
    public void Mobile_must_be_ten_digits(string mobile)
    {
        var r = ValidRequest();
        r.Mobile = mobile;
        Assert.Contains(nameof(EmployeeRequest.Mobile), ErrorsFor(r));
    }

    [Theory]
    [InlineData("Rahul123")]
    [InlineData("Rahul_Sharma")]
    [InlineData("   ")]
    [InlineData("")]
    public void Name_must_be_letters_and_spaces(string name)
    {
        var r = ValidRequest();
        r.FullName = name;
        Assert.Contains(nameof(EmployeeRequest.FullName), ErrorsFor(r));
    }

    [Fact]
    public void Bad_email_is_rejected()
    {
        var r = ValidRequest();
        r.Email = "rahul.example.com";
        Assert.Contains(nameof(EmployeeRequest.Email), ErrorsFor(r));
    }

    [Fact]
    public void Future_date_of_birth_is_rejected()
    {
        var r = ValidRequest();
        r.DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        Assert.Contains(nameof(EmployeeRequest.DateOfBirth), ErrorsFor(r));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Salary_must_be_positive(decimal salary)
    {
        var r = ValidRequest();
        r.AnnualSalary = salary;
        Assert.Contains(nameof(EmployeeRequest.AnnualSalary), ErrorsFor(r));
    }

    [Fact]
    public void Missing_required_fields_are_reported()
    {
        var errors = ErrorsFor(new EmployeeRequest { Status = "" });
        Assert.Contains(nameof(EmployeeRequest.DateOfBirth), errors);
        Assert.Contains(nameof(EmployeeRequest.JoiningDate), errors);
        Assert.Contains(nameof(EmployeeRequest.DepartmentId), errors);
        Assert.Contains(nameof(EmployeeRequest.RoleId), errors);
        Assert.Contains(nameof(EmployeeRequest.Address), errors);
        Assert.Contains(nameof(EmployeeRequest.Gender), errors);
        Assert.Contains(nameof(EmployeeRequest.Status), errors);
    }

    [Fact]
    public void Unknown_status_and_gender_are_rejected()
    {
        var r = ValidRequest();
        r.Status = "Retired";
        r.Gender = "Unknown";
        var errors = ErrorsFor(r);
        Assert.Contains(nameof(EmployeeRequest.Status), errors);
        Assert.Contains(nameof(EmployeeRequest.Gender), errors);
    }
}
