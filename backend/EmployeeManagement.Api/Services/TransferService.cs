using System.Data;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class TransferService
{
    private readonly AppDbContext _db;
    private readonly EmployeeCodeGenerator _codeGenerator;

    public TransferService(AppDbContext db, EmployeeCodeGenerator codeGenerator)
    {
        _db = db;
        _codeGenerator = codeGenerator;
    }

    public async Task<TransferResponse> TransferAsync(int employeeId, TransferRequest req, int? userId)
    {
        // everything below either fully happens or not at all
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // UPDLOCK so a second transfer (or edit) of the same employee waits here until we
        // commit, instead of both taking shared locks and deadlocking on the update
        var emp = await _db.Employees
            .FromSqlInterpolated($"SELECT * FROM Employees WITH (UPDLOCK, ROWLOCK) WHERE Id = {employeeId}")
            .Include(e => e.Department)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Employee {employeeId} was not found.");

        var newDept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == req.NewDepartmentId)
            ?? throw BadRequestException.ForField("NewDepartmentId", "New department does not exist.");

        if (newDept.Id == emp.DepartmentId)
            throw BadRequestException.ForField("NewDepartmentId", "Employee is already in this department.");

        if (req.TransferDate < emp.JoiningDate)
            throw BadRequestException.ForField("TransferDate",
                $"Transfer date can't be before the joining date ({emp.JoiningDate:dd/MM/yyyy}).");

        var oldCode = emp.EmployeeCode;
        var oldDept = emp.Department;
        var newCode = await _codeGenerator.NextCodeAsync(newDept.Id);

        _db.EmployeeTransferHistory.Add(new EmployeeTransferHistory
        {
            EmployeeId = emp.Id,
            OldDepartmentId = oldDept.Id,
            NewDepartmentId = newDept.Id,
            OldEmployeeCode = oldCode,
            NewEmployeeCode = newCode,
            TransferDate = req.TransferDate!.Value,
            Reason = req.Reason.Trim(),
            TransferredByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        // the internal Id stays the same, only the department and code change
        emp.DepartmentId = newDept.Id;
        emp.EmployeeCode = newCode;
        emp.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new TransferResponse
        {
            EmployeeId = emp.Id,
            OldEmployeeCode = oldCode,
            NewEmployeeCode = newCode,
            OldDepartment = oldDept.Name,
            NewDepartment = newDept.Name
        };
    }

    public async Task<List<TransferHistoryItem>> GetHistoryAsync(int employeeId, CurrentUser user)
    {
        if (user.IsEmployee && user.EmployeeId != employeeId)
            throw new ForbiddenException("You can only view your own transfer history.");

        if (!await _db.Employees.AnyAsync(e => e.Id == employeeId))
            throw new NotFoundException($"Employee {employeeId} was not found.");

        return await _db.EmployeeTransferHistory.AsNoTracking()
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.TransferDate).ThenByDescending(t => t.Id)
            .Select(t => new TransferHistoryItem
            {
                Id = t.Id,
                OldDepartment = t.OldDepartment.Name,
                NewDepartment = t.NewDepartment.Name,
                OldEmployeeCode = t.OldEmployeeCode,
                NewEmployeeCode = t.NewEmployeeCode,
                TransferDate = t.TransferDate,
                Reason = t.Reason,
                TransferredBy = t.TransferredBy != null ? t.TransferredBy.Username : null,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }
}
