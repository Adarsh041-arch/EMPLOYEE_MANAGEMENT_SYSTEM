using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>Returns a JWT. Send it as "Authorization: Bearer {token}".</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) =>
        Ok(await _auth.LoginAsync(request));
}
