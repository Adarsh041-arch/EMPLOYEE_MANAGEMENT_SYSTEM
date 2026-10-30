namespace EmployeeManagement.Api.DTOs;

public class TransferRequest
{
    public int NewDepartmentId { get; set; }
    public DateOnly? TransferDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class TransferResponse
{
    public int EmployeeId { get; set; }
    public string OldEmployeeCode { get; set; } = string.Empty;
    public string NewEmployeeCode { get; set; } = string.Empty;
    public string OldDepartment { get; set; } = string.Empty;
    public string NewDepartment { get; set; } = string.Empty;
}

public class TransferHistoryItem
{
    public int Id { get; set; }
    public string OldDepartment { get; set; } = string.Empty;
    public string NewDepartment { get; set; } = string.Empty;
    public string OldEmployeeCode { get; set; } = string.Empty;
    public string NewEmployeeCode { get; set; } = string.Empty;
    public DateOnly TransferDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? TransferredBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
