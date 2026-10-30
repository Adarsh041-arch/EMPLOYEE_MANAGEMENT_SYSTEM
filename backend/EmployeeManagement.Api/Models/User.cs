namespace EmployeeManagement.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Employee;

    // set for users with the Employee role so they can see their own record
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public const string AdminOrManager = Admin + "," + Manager;
}
