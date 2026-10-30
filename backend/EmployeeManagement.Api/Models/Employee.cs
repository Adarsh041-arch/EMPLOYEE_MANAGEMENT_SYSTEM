namespace EmployeeManagement.Api.Models;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateOnly JoiningDate { get; set; }
    public decimal AnnualSalary { get; set; }
    public string Status { get; set; } = EmployeeStatus.Active;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public EmployeePersonalDetails? PersonalDetails { get; set; }
    public ICollection<EmployeeTransferHistory> Transfers { get; set; } = new List<EmployeeTransferHistory>();
}

public static class EmployeeStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string OnLeave = "OnLeave";

    public static readonly string[] All = { Active, Inactive, OnLeave };
}

public static class Genders
{
    public static readonly string[] All = { "Male", "Female", "Other" };
}
