namespace EmployeeManagement.Api.DTOs;

// used for both create and update - the employee code is never sent by the client
public class EmployeeRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Aadhaar { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int RoleId { get; set; }
    public DateOnly? JoiningDate { get; set; }
    public decimal AnnualSalary { get; set; }
    public string Status { get; set; } = "Active";
}

// On update Aadhaar and PAN can be left empty to keep the stored values
// (non-admin users only ever see masked values, so they can't send them back)
public class UpdateEmployeeRequest : EmployeeRequest { }

public class EmployeeListItem
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateOnly JoiningDate { get; set; }
    public decimal AnnualSalary { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EmployeeDetails : EmployeeListItem
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public PersonalDetailsResponse? PersonalDetails { get; set; }
}

public class PersonalDetailsResponse
{
    public DateOnly DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;

    // full value for Admin, masked (XXXX-XXXX-1234 / ABXXXXX34F) for everyone else
    public string Aadhaar { get; set; } = string.Empty;
    public string Pan { get; set; } = string.Empty;
    public bool IsMasked { get; set; }
}

public class EmployeeQuery
{
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public int? RoleId { get; set; }
    public string? Status { get; set; }
    public string? Gender { get; set; }
    public DateOnly? JoiningFrom { get; set; }
    public DateOnly? JoiningTo { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
