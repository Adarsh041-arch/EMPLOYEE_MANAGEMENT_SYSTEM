namespace EmployeeManagement.Api.Models;

public class EmployeeTransferHistory
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int OldDepartmentId { get; set; }
    public Department OldDepartment { get; set; } = null!;
    public int NewDepartmentId { get; set; }
    public Department NewDepartment { get; set; } = null!;

    public string OldEmployeeCode { get; set; } = string.Empty;
    public string NewEmployeeCode { get; set; } = string.Empty;
    public DateOnly TransferDate { get; set; }
    public string Reason { get; set; } = string.Empty;

    public int? TransferredByUserId { get; set; }
    public User? TransferredBy { get; set; }

    public DateTime CreatedAt { get; set; }
}
