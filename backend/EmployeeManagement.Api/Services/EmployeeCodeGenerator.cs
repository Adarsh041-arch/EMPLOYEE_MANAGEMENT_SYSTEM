using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.Middleware;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class EmployeeCodeGenerator
{
    private readonly AppDbContext _db;

    public EmployeeCodeGenerator(AppDbContext db) => _db = db;

    // Must be called inside a transaction. The UPDATE takes a row lock on the
    // department's sequence row, so a second request for the same department waits
    // until the first one commits (or rolls back) - no two employees get the same number.
    public async Task<string> NextCodeAsync(int departmentId)
    {
        if (_db.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Employee codes must be generated inside a transaction.");

        var prefix = await _db.Departments
            .Where(d => d.Id == departmentId)
            .Select(d => d.CodePrefix)
            .SingleOrDefaultAsync()
            ?? throw BadRequestException.ForField("DepartmentId", "Department does not exist.");

        var numbers = await _db.Database
            .SqlQuery<int>($"UPDATE EmployeeCodeSequences WITH (ROWLOCK) SET LastNumber = LastNumber + 1 OUTPUT inserted.LastNumber AS [Value] WHERE DepartmentId = {departmentId}")
            .ToListAsync();

        if (numbers.Count != 1)
            throw new InvalidOperationException($"No code sequence found for department {departmentId}.");

        return $"{prefix}{numbers[0]:D4}";
    }
}
