using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employees;
    private readonly TransferService _transfers;

    public EmployeesController(EmployeeService employees, TransferService transfers)
    {
        _employees = employees;
        _transfers = transfers;
    }

    private CurrentUser Me => CurrentUser.From(User);

    /// <summary>Search, filter, sort and page through employees.</summary>
    /// <remarks>
    /// sortBy: code, name, department, role, email, mobile, salary, joiningDate, status.
    /// sortDir: asc or desc. pageSize: 5, 10 or 20.
    /// Employees (role) only get their own record back.
    /// </remarks>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EmployeeListItem>>> GetAll([FromQuery] EmployeeQuery query) =>
        Ok(await _employees.QueryAsync(query, Me));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDetails>> GetById(int id) =>
        Ok(await _employees.GetByIdAsync(id, Me));

    /// <summary>Aadhaar and PAN are only shown in full to Admin users.</summary>
    [HttpGet("{id:int}/personal-details")]
    public async Task<ActionResult<PersonalDetailsResponse>> GetPersonalDetails(int id) =>
        Ok(await _employees.GetPersonalDetailsAsync(id, Me));

    /// <summary>Creates an employee. The employee code is generated from the department.</summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.AdminOrManager)]
    public async Task<ActionResult<EmployeeDetails>> Create(EmployeeRequest request)
    {
        var created = await _employees.CreateAsync(request, Me);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an employee. Department changes have to go through the transfer endpoint.</summary>
    /// <remarks>Leave Aadhaar / PAN empty to keep the values already stored.</remarks>
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRoles.AdminOrManager)]
    public async Task<ActionResult<EmployeeDetails>> Update(int id, UpdateEmployeeRequest request) =>
        Ok(await _employees.UpdateAsync(id, request, Me));

    /// <summary>Marks the employee Inactive. Pass hard=true to remove the record completely.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Delete(int id, [FromQuery] bool hard = false)
    {
        await _employees.DeleteAsync(id, hard);
        return NoContent();
    }

    /// <summary>Moves the employee to another department and gives them a new code.</summary>
    [HttpPost("{id:int}/transfer")]
    [Authorize(Roles = UserRoles.AdminOrManager)]
    public async Task<ActionResult<TransferResponse>> Transfer(int id, TransferRequest request) =>
        Ok(await _transfers.TransferAsync(id, request, Me.UserId));

    [HttpGet("{id:int}/transfers")]
    public async Task<ActionResult<List<TransferHistoryItem>>> GetTransfers(int id) =>
        Ok(await _transfers.GetHistoryAsync(id, Me));
}
