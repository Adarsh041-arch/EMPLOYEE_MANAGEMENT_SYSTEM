using System.Net.Http.Headers;
using System.Net.Http.Json;
using EmployeeManagement.Api.Data;
using EmployeeManagement.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Tests;

// Runs the real API against a separate SQL Server database that is dropped and
// re-created (and re-seeded) once per test run.
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ConnectionString =
        "Server=.\\SQLEXPRESS;Database=EmployeeManagement_Test;Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting (rather than ConfigureAppConfiguration) so the values are visible
        // to the code in Program.cs that runs before builder.Build()
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", "test-signing-key-that-is-long-enough-for-hmac-sha256");
        builder.UseSetting("Encryption:Key", "dGVzdC1rZXktZm9yLXVuaXQtdGVzdHMtMzJieXRlcyE=");
        builder.UseSetting("SeedOnStartup", "false");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public async Task<HttpClient> ClientFor(string username, string password)
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Username = username, Password = password });
        res.EnsureSuccessStatusCode();
        var login = await res.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    public Task<HttpClient> AdminClient() => ClientFor("admin", "Admin@123");
    public Task<HttpClient> ManagerClient() => ClientFor("manager", "Manager@123");
    public Task<HttpClient> EmployeeClient() => ClientFor("employee", "Employee@123");
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory> { }
