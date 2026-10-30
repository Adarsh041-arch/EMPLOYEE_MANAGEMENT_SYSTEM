using System.Security.Claims;
using EmployeeManagement.Api.Models;

namespace EmployeeManagement.Api.Services;

public record CurrentUser(int UserId, string Username, string Role, int? EmployeeId)
{
    public bool IsAdmin => Role == UserRoles.Admin;
    public bool IsEmployee => Role == UserRoles.Employee;

    public static CurrentUser From(ClaimsPrincipal principal)
    {
        var employeeId = principal.FindFirstValue("employeeId");
        return new CurrentUser(
            int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
            principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Role) ?? UserRoles.Employee,
            int.TryParse(employeeId, out var id) ? id : null);
    }
}
