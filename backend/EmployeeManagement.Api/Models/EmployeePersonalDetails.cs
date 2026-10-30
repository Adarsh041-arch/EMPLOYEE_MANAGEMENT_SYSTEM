namespace EmployeeManagement.Api.Models;

public class EmployeePersonalDetails
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;

    // Aadhaar and PAN are stored encrypted. The last 4 digits / masked PAN are kept
    // separately so we can show them without decrypting every row.
    public string AadhaarEncrypted { get; set; } = string.Empty;
    public string AadhaarLast4 { get; set; } = string.Empty;
    public string PanEncrypted { get; set; } = string.Empty;
    public string PanMasked { get; set; } = string.Empty;
}
