using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Controllers;

// dropdown data for the frontend
[ApiController]
[Route("api")]
[Authorize]
public class LookupsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LookupsController(AppDbContext db) => _db = db;

    [HttpGet("departments")]
    public async Task<List<LookupItem>> GetDepartments() =>
        await _db.Departments.OrderBy(d => d.Name).Select(d => new LookupItem(d.Id, d.Name)).ToListAsync();

    [HttpGet("roles")]
    public async Task<List<LookupItem>> GetRoles() =>
        await _db.Roles.OrderBy(r => r.Name).Select(r => new LookupItem(r.Id, r.Name)).ToListAsync();
}
