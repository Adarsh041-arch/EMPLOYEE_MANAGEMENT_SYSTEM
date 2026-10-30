namespace EmployeeManagement.Api.Models;

public class EmployeeCodeSequence
{
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public int LastNumber { get; set; }
}
