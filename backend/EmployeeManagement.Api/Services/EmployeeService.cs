using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Services;

public class EmployeeService
{
    private static readonly int[] AllowedPageSizes = { 5, 10, 20 };

    private readonly AppDbContext _db;
    private readonly EmployeeCodeGenerator _codeGenerator;
    private readonly SensitiveDataProtector _protector;

    public EmployeeService(AppDbContext db, EmployeeCodeGenerator codeGenerator, SensitiveDataProtector protector)
    {
        _db = db;
        _codeGenerator = codeGenerator;
        _protector = protector;
    }

    public async Task<PagedResult<EmployeeListItem>> QueryAsync(EmployeeQuery q, CurrentUser user)
    {
        if (!AllowedPageSizes.Contains(q.PageSize))
            throw BadRequestException.ForField("pageSize", "Page size must be 5, 10 or 20.");
        if (q.PageNumber < 1)
            throw BadRequestException.ForField("pageNumber", "Page number must be 1 or more.");
        if (q.MinSalary.HasValue && q.MaxSalary.HasValue && q.MinSalary > q.MaxSalary)
            throw BadRequestException.ForField("minSalary", "Minimum salary cannot be more than maximum salary.");

        var query = _db.Employees.AsNoTracking().AsQueryable();

        // employees can only ever see themselves
        if (user.IsEmployee)
            query = query.Where(e => e.Id == user.EmployeeId);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            // Aadhaar and PAN are deliberately not part of the search
            var s = q.Search.Trim();
            query = query.Where(e =>
                e.EmployeeCode.Contains(s) ||
                e.FullName.Contains(s) ||
                e.Email.Contains(s) ||
                e.Mobile.Contains(s) ||
                e.Department.Name.Contains(s) ||
                e.Role.Name.Contains(s) ||
                e.Status.Contains(s) ||
                (e.PersonalDetails != null && e.PersonalDetails.Address.Contains(s)));
        }

        if (q.DepartmentId.HasValue) query = query.Where(e => e.DepartmentId == q.DepartmentId);
        if (q.RoleId.HasValue) query = query.Where(e => e.RoleId == q.RoleId);
        if (!string.IsNullOrWhiteSpace(q.Status)) query = query.Where(e => e.Status == q.Status);
        if (!string.IsNullOrWhiteSpace(q.Gender)) query = query.Where(e => e.Gender == q.Gender);
        if (q.JoiningFrom.HasValue) query = query.Where(e => e.JoiningDate >= q.JoiningFrom);
        if (q.JoiningTo.HasValue) query = query.Where(e => e.JoiningDate <= q.JoiningTo);
        if (q.MinSalary.HasValue) query = query.Where(e => e.AnnualSalary >= q.MinSalary);
        if (q.MaxSalary.HasValue) query = query.Where(e => e.AnnualSalary <= q.MaxSalary);

        var total = await query.CountAsync();

        var items = await ApplySort(query, q.SortBy, q.SortDir)
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(ToListItem)
            .ToListAsync();

        return new PagedResult<EmployeeListItem>
        {
            Items = items,
            TotalCount = total,
            PageNumber = q.PageNumber,
            PageSize = q.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)q.PageSize)
        };
    }

    private static IQueryable<Employee> ApplySort(IQueryable<Employee> query, string? sortBy, string? sortDir)
    {
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        if (sortDir != null && !desc && !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase))
            throw BadRequestException.ForField("sortDir", "Sort direction must be asc or desc.");

        // only these columns can be sorted on; Id is added as a tie breaker so paging is stable
        IOrderedQueryable<Employee> sorted = (sortBy?.ToLowerInvariant() ?? "code") switch
        {
            "code" => desc ? query.OrderByDescending(e => e.EmployeeCode) : query.OrderBy(e => e.EmployeeCode),
            "name" => desc ? query.OrderByDescending(e => e.FullName) : query.OrderBy(e => e.FullName),
            "department" => desc ? query.OrderByDescending(e => e.Department.Name) : query.OrderBy(e => e.Department.Name),
            "role" => desc ? query.OrderByDescending(e => e.Role.Name) : query.OrderBy(e => e.Role.Name),
            "email" => desc ? query.OrderByDescending(e => e.Email) : query.OrderBy(e => e.Email),
            "mobile" => desc ? query.OrderByDescending(e => e.Mobile) : query.OrderBy(e => e.Mobile),
            "salary" => desc ? query.OrderByDescending(e => e.AnnualSalary) : query.OrderBy(e => e.AnnualSalary),
            "joiningdate" => desc ? query.OrderByDescending(e => e.JoiningDate) : query.OrderBy(e => e.JoiningDate),
            "status" => desc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            _ => throw BadRequestException.ForField("sortBy",
                "Sort by must be one of: code, name, department, role, email, mobile, salary, joiningDate, status.")
        };
        return sorted.ThenBy(e => e.Id);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Employee, EmployeeListItem>> ToListItem = e => new EmployeeListItem
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName = e.FullName,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department.Name,
        RoleId = e.RoleId,
        RoleName = e.Role.Name,
        Email = e.Email,
        Mobile = e.Mobile,
        Gender = e.Gender,
        JoiningDate = e.JoiningDate,
        AnnualSalary = e.AnnualSalary,
        Status = e.Status
    };

    public async Task<EmployeeDetails> GetByIdAsync(int id, CurrentUser user)
    {
        CheckCanView(id, user);

        var emp = await _db.Employees.AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Role)
            .Include(e => e.PersonalDetails)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException($"Employee {id} was not found.");

        return ToDetails(emp, user);
    }

    public async Task<PersonalDetailsResponse> GetPersonalDetailsAsync(int id, CurrentUser user)
    {
        CheckCanView(id, user);

        if (!await _db.Employees.AnyAsync(e => e.Id == id))
            throw new NotFoundException($"Employee {id} was not found.");

        var details = await _db.EmployeePersonalDetails.AsNoTracking().FirstOrDefaultAsync(p => p.EmployeeId == id)
            ?? throw new NotFoundException($"Personal details for employee {id} were not found.");

        return ToPersonalDetails(details, user);
    }

    public async Task<EmployeeDetails> CreateAsync(EmployeeRequest req, CurrentUser user)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        await CheckLookupsAndEmail(req, email, excludeId: null);

        await using var tx = await _db.Database.BeginTransactionAsync();

        var now = DateTime.UtcNow;
        var emp = new Employee
        {
            EmployeeCode = await _codeGenerator.NextCodeAsync(req.DepartmentId),
            DepartmentId = req.DepartmentId,
            CreatedAt = now
        };
        ApplyRequest(emp, req, email, now);
        emp.PersonalDetails = new EmployeePersonalDetails();
        ApplyPersonalDetails(emp.PersonalDetails, req);

        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetByIdAsync(emp.Id, user);
    }

    public async Task<EmployeeDetails> UpdateAsync(int id, EmployeeRequest req, CurrentUser user)
    {
        var emp = await _db.Employees.Include(e => e.PersonalDetails).FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException($"Employee {id} was not found.");

        if (!user.IsAdmin)
        {
            if (user.EmployeeId == id)
                throw new ForbiddenException("You can't edit your own employee record. Ask an admin.");
            if (req.Status == EmployeeStatus.Inactive && emp.Status != EmployeeStatus.Inactive)
                throw new ForbiddenException("Only an admin can deactivate an employee.");
        }

        if (emp.DepartmentId != req.DepartmentId)
            throw BadRequestException.ForField("DepartmentId",
                "Department can't be changed here. Use the transfer option so the employee code and history are updated.");

        var email = req.Email.Trim().ToLowerInvariant();
        await CheckLookupsAndEmail(req, email, excludeId: id);

        ApplyRequest(emp, req, email, DateTime.UtcNow);
        emp.PersonalDetails ??= new EmployeePersonalDetails();
        ApplyPersonalDetails(emp.PersonalDetails, req);

        await _db.SaveChangesAsync();
        return await GetByIdAsync(id, user);
    }

    public async Task DeleteAsync(int id, bool hard)
    {
        var emp = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new NotFoundException($"Employee {id} was not found.");

        if (!hard)
        {
            emp.Status = EmployeeStatus.Inactive;
            emp.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return;
        }

        // transfer history has to be kept, so only employees that were never transferred can be removed
        if (await _db.EmployeeTransferHistory.AnyAsync(t => t.EmployeeId == id))
            throw new AppException(StatusCodes.Status409Conflict,
                "This employee has transfer history, so they can't be deleted permanently. Deactivate them instead.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        // unlink the login account (if any) first
        await _db.Users.Where(u => u.EmployeeId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.EmployeeId, (int?)null));
        _db.Employees.Remove(emp);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static void CheckCanView(int id, CurrentUser user)
    {
        if (user.IsEmployee && user.EmployeeId != id)
            throw new ForbiddenException("You can only view your own employee record.");
    }

    private async Task CheckLookupsAndEmail(EmployeeRequest req, string email, int? excludeId)
    {
        if (!await _db.Departments.AnyAsync(d => d.Id == req.DepartmentId))
            throw BadRequestException.ForField("DepartmentId", "Department does not exist.");
        if (!await _db.Roles.AnyAsync(r => r.Id == req.RoleId))
            throw BadRequestException.ForField("RoleId", "Role does not exist.");
        if (await _db.Employees.AnyAsync(e => e.Email == email && e.Id != excludeId))
            throw new ConflictException("Email", "Another employee already uses this email address.");
    }

    private static void ApplyRequest(Employee emp, EmployeeRequest req, string email, DateTime now)
    {
        emp.FullName = req.FullName.Trim();
        emp.Email = email;
        emp.Mobile = req.Mobile;
        emp.Gender = req.Gender;
        emp.RoleId = req.RoleId;
        emp.JoiningDate = req.JoiningDate!.Value;
        emp.AnnualSalary = req.AnnualSalary;
        emp.Status = req.Status;
        emp.UpdatedAt = now;
    }

    private void ApplyPersonalDetails(EmployeePersonalDetails details, EmployeeRequest req)
    {
        details.DateOfBirth = req.DateOfBirth!.Value;
        details.Address = req.Address.Trim();

        // empty on update means "keep what is stored" (the validator only allows that for updates)
        if (!string.IsNullOrEmpty(req.Aadhaar))
        {
            var aadhaar = SensitiveDataProtector.NormalizeAadhaar(req.Aadhaar);
            details.AadhaarEncrypted = _protector.Encrypt(aadhaar);
            details.AadhaarLast4 = aadhaar[^4..];
        }
        if (!string.IsNullOrEmpty(req.Pan))
        {
            details.PanEncrypted = _protector.Encrypt(req.Pan);
            details.PanMasked = SensitiveDataProtector.MaskPan(req.Pan);
        }

        if (string.IsNullOrEmpty(details.AadhaarEncrypted) || string.IsNullOrEmpty(details.PanEncrypted))
            throw BadRequestException.ForField(string.IsNullOrEmpty(details.AadhaarEncrypted) ? "Aadhaar" : "Pan",
                "Aadhaar and PAN are required.");
    }

    private EmployeeDetails ToDetails(Employee e, CurrentUser user) => new()
    {
        Id = e.Id,
        EmployeeCode = e.EmployeeCode,
        FullName = e.FullName,
        DepartmentId = e.DepartmentId,
        DepartmentName = e.Department.Name,
        RoleId = e.RoleId,
        RoleName = e.Role.Name,
        Email = e.Email,
        Mobile = e.Mobile,
        Gender = e.Gender,
        JoiningDate = e.JoiningDate,
        AnnualSalary = e.AnnualSalary,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        PersonalDetails = e.PersonalDetails == null ? null : ToPersonalDetails(e.PersonalDetails, user)
    };

    private PersonalDetailsResponse ToPersonalDetails(EmployeePersonalDetails p, CurrentUser user)
    {
        // only Admin sees the full Aadhaar / PAN
        return new PersonalDetailsResponse
        {
            DateOfBirth = p.DateOfBirth,
            Address = p.Address,
            Aadhaar = user.IsAdmin
                ? SensitiveDataProtector.FormatAadhaar(_protector.Decrypt(p.AadhaarEncrypted))
                : SensitiveDataProtector.MaskAadhaar(p.AadhaarLast4),
            Pan = user.IsAdmin ? _protector.Decrypt(p.PanEncrypted) : p.PanMasked,
            IsMasked = !user.IsAdmin
        };
    }
}
