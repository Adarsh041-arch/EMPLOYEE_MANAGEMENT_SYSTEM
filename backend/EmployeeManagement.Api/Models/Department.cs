namespace EmployeeManagement.Api.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // single letter used at the start of the employee code, e.g. "I" for IT
    public string CodePrefix { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
