using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == req.Username);

        // same message for unknown user and wrong password
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new AppException(StatusCodes.Status401Unauthorized, "Invalid username or password.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        if (user.EmployeeId.HasValue)
            claims.Add(new Claim("employeeId", user.EmployeeId.Value.ToString()));

        var expires = DateTime.UtcNow.AddHours(_config.GetValue("Jwt:ExpiryHours", 8));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = user.Username,
            Role = user.Role,
            EmployeeId = user.EmployeeId,
            ExpiresAt = expires
        };
    }
}
