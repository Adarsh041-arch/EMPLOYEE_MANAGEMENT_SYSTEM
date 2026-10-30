using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmployeeManagement.Api.DTOs;

namespace EmployeeManagement.Tests;

[Collection("api")]
public class EmployeeApiTests
{
    private readonly ApiFactory _factory;
    private static int _counter;

    public EmployeeApiTests(ApiFactory factory) => _factory = factory;

    // unique name/email/mobile per call so tests don't collide with each other or the seed data
    private static EmployeeRequest NewEmployee(int departmentId = 1)
    {
        var n = Interlocked.Increment(ref _counter);
        var letters = new string(n.ToString("D4").Select(c => (char)('a' + (c - '0'))).ToArray());
        return new EmployeeRequest
        {
            FullName = "Test Person " + letters,
            Email = $"test.person{n}@example.com",
            Mobile = "70000" + n.ToString("D5"),
            Gender = "Female",
            Aadhaar = "8765-4321-" + n.ToString("D4"),
            Pan = "QWERT" + n.ToString("D4") + "Z",
            DateOfBirth = new DateOnly(1995, 3, 2),
            Address = "Test Lane, Mumbai",
            DepartmentId = departmentId,
            RoleId = 1,
            JoiningDate = new DateOnly(2024, 6, 1),
            AnnualSalary = 600000,
            Status = "Active"
        };
    }

    private static async Task<EmployeeDetails> Create(HttpClient client, EmployeeRequest req)
    {
        var res = await client.PostAsJsonAsync("/api/employees", req);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<EmployeeDetails>())!;
    }

    private static async Task<PagedResult<EmployeeListItem>> List(HttpClient client, string query)
    {
        var res = await client.GetAsync("/api/employees?" + query);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<PagedResult<EmployeeListItem>>())!;
    }

    private static int CodeNumber(string code) => int.Parse(code[1..]);

    [Fact]
    public async Task Requests_without_a_token_get_401()
    {
        var res = await _factory.CreateClient().GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_gets_401()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "admin", Password = "nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Seed_data_has_twenty_plus_employees_and_some_transfers()
    {
        var admin = await _factory.AdminClient();
        var all = await List(admin, "pageSize=5");
        Assert.True(all.TotalCount >= 20);

        // the seeder transfers Rahul Sharma from IT to HR
        var rahul = (await List(admin, "search=Rahul%20Sharma&pageSize=5")).Items.Single();
        Assert.StartsWith("H", rahul.EmployeeCode);
        var history = await admin.GetFromJsonAsync<List<TransferHistoryItem>>($"/api/employees/{rahul.Id}/transfers");
        Assert.Equal("I0001", history!.Single().OldEmployeeCode);
    }

    [Fact]
    public async Task New_employee_gets_next_code_for_their_department()
    {
        var admin = await _factory.AdminClient();
        var first = await Create(admin, NewEmployee(departmentId: 3));
        var second = await Create(admin, NewEmployee(departmentId: 3));

        Assert.Matches("^F\\d{4}$", first.EmployeeCode);
        Assert.Equal(CodeNumber(first.EmployeeCode) + 1, CodeNumber(second.EmployeeCode));
    }

    [Fact]
    public async Task Parallel_creates_in_one_department_never_share_a_code()
    {
        var admin = await _factory.AdminClient();
        var tasks = Enumerable.Range(0, 10).Select(_ => admin.PostAsJsonAsync("/api/employees", NewEmployee(departmentId: 4)));
        var responses = await Task.WhenAll(tasks);

        var codes = new List<string>();
        foreach (var res in responses)
        {
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            codes.Add((await res.Content.ReadFromJsonAsync<EmployeeDetails>())!.EmployeeCode);
        }
        Assert.Equal(10, codes.Distinct().Count());
    }

    [Fact]
    public async Task Transfer_changes_code_and_department_but_keeps_id()
    {
        var admin = await _factory.AdminClient();
        var emp = await Create(admin, NewEmployee(departmentId: 1));
        Assert.StartsWith("I", emp.EmployeeCode);

        var res = await admin.PostAsJsonAsync($"/api/employees/{emp.Id}/transfer", new TransferRequest
        {
            NewDepartmentId = 2,
            TransferDate = new DateOnly(2025, 1, 15),
            Reason = "Moving to recruitment team"
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var transfer = (await res.Content.ReadFromJsonAsync<TransferResponse>())!;
        Assert.Equal(emp.EmployeeCode, transfer.OldEmployeeCode);
        Assert.StartsWith("H", transfer.NewEmployeeCode);

        var after = (await admin.GetFromJsonAsync<EmployeeDetails>($"/api/employees/{emp.Id}"))!;
        Assert.Equal(emp.Id, after.Id);
        Assert.Equal(transfer.NewEmployeeCode, after.EmployeeCode);
        Assert.Equal("HR", after.DepartmentName);

        // list shows the new code too
        var found = await List(admin, "search=" + transfer.NewEmployeeCode);
        Assert.Contains(found.Items, i => i.Id == emp.Id);

        var history = (await admin.GetFromJsonAsync<List<TransferHistoryItem>>($"/api/employees/{emp.Id}/transfers"))!;
        var row = Assert.Single(history);
        Assert.Equal("IT", row.OldDepartment);
        Assert.Equal("HR", row.NewDepartment);
        Assert.Equal(transfer.NewEmployeeCode, row.NewEmployeeCode);
        Assert.Equal("admin", row.TransferredBy);
    }

    [Fact]
    public async Task Transfer_to_same_department_is_rejected()
    {
        var admin = await _factory.AdminClient();
        var emp = await Create(admin, NewEmployee(departmentId: 5));
        var res = await admin.PostAsJsonAsync($"/api/employees/{emp.Id}/transfer", new TransferRequest
        {
            NewDepartmentId = 5,
            TransferDate = new DateOnly(2025, 1, 15),
            Reason = "No change"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var after = (await admin.GetFromJsonAsync<EmployeeDetails>($"/api/employees/{emp.Id}"))!;
        Assert.Equal(emp.EmployeeCode, after.EmployeeCode);
    }

    [Fact]
    public async Task Duplicate_email_gets_409()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        await Create(admin, req);

        var again = NewEmployee();
        again.Email = req.Email.ToUpperInvariant();
        var res = await admin.PostAsJsonAsync("/api/employees", again);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Invalid_fields_get_400_with_field_errors()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        req.Pan = "abc";
        req.AnnualSalary = 0;
        req.DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(1));

        var res = await admin.PostAsJsonAsync("/api/employees", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Pan", out _));
        Assert.True(errors.TryGetProperty("AnnualSalary", out _));
        Assert.True(errors.TryGetProperty("DateOfBirth", out _));
    }

    [Fact]
    public async Task Unknown_employee_gets_404()
    {
        var admin = await _factory.AdminClient();
        var res = await admin.GetAsync("/api/employees/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Employee_role_cannot_write_or_read_others()
    {
        var employee = await _factory.EmployeeClient();
        var create = await employee.PostAsJsonAsync("/api/employees", NewEmployee());
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        // the list only contains the user's own record
        var own = await List(employee, "pageSize=20");
        var me = Assert.Single(own.Items);
        Assert.Equal(HttpStatusCode.OK, (await employee.GetAsync($"/api/employees/{me.Id}")).StatusCode);

        var admin = await _factory.AdminClient();
        var other = await Create(admin, NewEmployee());
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync($"/api/employees/{other.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.DeleteAsync($"/api/employees/{other.Id}")).StatusCode);
    }

    [Fact]
    public async Task Aadhaar_and_pan_are_masked_unless_admin()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        var emp = await Create(admin, req);

        var full = (await admin.GetFromJsonAsync<PersonalDetailsResponse>($"/api/employees/{emp.Id}/personal-details"))!;
        Assert.Equal(req.Aadhaar, full.Aadhaar);
        Assert.Equal(req.Pan, full.Pan);
        Assert.False(full.IsMasked);

        var manager = await _factory.ManagerClient();
        var masked = (await manager.GetFromJsonAsync<PersonalDetailsResponse>($"/api/employees/{emp.Id}/personal-details"))!;
        Assert.Equal("XXXX-XXXX-" + req.Aadhaar[^4..], masked.Aadhaar);
        Assert.DoesNotContain(req.Pan[2..7], masked.Pan);
        Assert.True(masked.IsMasked);
    }

    [Fact]
    public async Task Global_search_does_not_look_at_aadhaar_or_pan()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        await Create(admin, req);

        var byAadhaar = await List(admin, "search=" + req.Aadhaar.Replace("-", ""));
        var byPan = await List(admin, "search=" + req.Pan);
        Assert.Equal(0, byAadhaar.TotalCount);
        Assert.Equal(0, byPan.TotalCount);
    }

    [Fact]
    public async Task Search_covers_address_role_department_and_status()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee(departmentId: 3);
        req.Address = "Unique Street Xyzzy";
        var emp = await Create(admin, req);

        Assert.Contains((await List(admin, "search=Xyzzy")).Items, i => i.Id == emp.Id);
        Assert.All((await List(admin, "search=Finance&pageSize=20")).Items,
            i => Assert.True(i.DepartmentName == "Finance" || i.FullName.Contains("Finance")));
        Assert.All((await List(admin, "search=OnLeave&pageSize=20")).Items, i => Assert.Equal("OnLeave", i.Status));
    }

    [Fact]
    public async Task Filters_combine_and_paging_totals_are_consistent()
    {
        var admin = await _factory.AdminClient();
        const string filters = "status=Active&minSalary=300000&maxSalary=1500000&sortBy=salary&sortDir=desc";

        var big = await List(admin, filters + "&pageSize=20");
        var page1 = await List(admin, filters + "&pageSize=5&pageNumber=1");
        var page2 = await List(admin, filters + "&pageSize=5&pageNumber=2");

        Assert.Equal(big.TotalCount, page1.TotalCount);
        Assert.Equal((int)Math.Ceiling(big.TotalCount / 5.0), page1.TotalPages);
        Assert.Equal(big.Items.Take(10).Select(i => i.Id), page1.Items.Concat(page2.Items).Select(i => i.Id));
        Assert.All(big.Items, i =>
        {
            Assert.Equal("Active", i.Status);
            Assert.InRange(i.AnnualSalary, 300000, 1500000);
        });
        // sorted by salary, highest first
        Assert.Equal(big.Items.Select(i => i.AnnualSalary).OrderByDescending(s => s), big.Items.Select(i => i.AnnualSalary));
    }

    [Fact]
    public async Task Department_role_gender_and_joining_date_filters_work()
    {
        var admin = await _factory.AdminClient();
        var result = await List(admin, "departmentId=1&roleId=1&gender=Male&joiningFrom=2020-01-01&joiningTo=2023-12-31&pageSize=20");
        Assert.All(result.Items, i =>
        {
            Assert.Equal(1, i.DepartmentId);
            Assert.Equal(1, i.RoleId);
            Assert.Equal("Male", i.Gender);
            Assert.InRange(i.JoiningDate, new DateOnly(2020, 1, 1), new DateOnly(2023, 12, 31));
        });
    }

    [Fact]
    public async Task Example_query_from_the_spec_works()
    {
        var admin = await _factory.AdminClient();
        var res = await admin.GetAsync("/api/employees?search=Rahul&departmentId=1&minSalary=300000&maxSalary=500000&pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [InlineData("pageSize=7")]
    [InlineData("pageNumber=0")]
    [InlineData("sortBy=aadhaar")]
    public async Task Bad_paging_or_sort_values_get_400(string query)
    {
        var admin = await _factory.AdminClient();
        var res = await admin.GetAsync("/api/employees?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_changes_fields_but_not_the_code()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        var emp = await Create(admin, req);

        req.AnnualSalary = 750000;
        req.Status = "OnLeave";
        var res = await admin.PutAsJsonAsync($"/api/employees/{emp.Id}", req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = (await res.Content.ReadFromJsonAsync<EmployeeDetails>())!;
        Assert.Equal(750000, updated.AnnualSalary);
        Assert.Equal("OnLeave", updated.Status);
        Assert.Equal(emp.EmployeeCode, updated.EmployeeCode);
    }

    [Fact]
    public async Task Update_without_aadhaar_and_pan_keeps_the_stored_values()
    {
        // managers only ever see masked values, so they can't send them back
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        var emp = await Create(admin, req);

        var manager = await _factory.ManagerClient();
        var update = NewEmployee();
        update.FullName = req.FullName;
        update.Email = req.Email;
        update.Aadhaar = "";
        update.Pan = "";
        update.Address = "New Address, Pune";
        var res = await manager.PutAsJsonAsync($"/api/employees/{emp.Id}", update);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var details = (await admin.GetFromJsonAsync<PersonalDetailsResponse>($"/api/employees/{emp.Id}/personal-details"))!;
        Assert.Equal(req.Aadhaar, details.Aadhaar);
        Assert.Equal(req.Pan, details.Pan);
        Assert.Equal("New Address, Pune", details.Address);
    }

    [Fact]
    public async Task Create_still_requires_aadhaar_and_pan()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        req.Aadhaar = "";
        req.Pan = "";
        var res = await admin.PostAsJsonAsync("/api/employees", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Changing_department_through_update_is_rejected()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee(departmentId: 1);
        var emp = await Create(admin, req);
        req.DepartmentId = 2;
        var res = await admin.PutAsJsonAsync($"/api/employees/{emp.Id}", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_deactivates_and_hard_delete_removes()
    {
        var admin = await _factory.AdminClient();
        var emp = await Create(admin, NewEmployee());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/employees/{emp.Id}")).StatusCode);
        var after = (await admin.GetFromJsonAsync<EmployeeDetails>($"/api/employees/{emp.Id}"))!;
        Assert.Equal("Inactive", after.Status);

        var manager = await _factory.ManagerClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.DeleteAsync($"/api/employees/{emp.Id}?hard=true")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/employees/{emp.Id}?hard=true")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/employees/{emp.Id}")).StatusCode);
    }

    [Fact]
    public async Task Transfer_date_before_joining_date_is_rejected()
    {
        var admin = await _factory.AdminClient();
        var emp = await Create(admin, NewEmployee(departmentId: 1)); // joined 2024-06-01

        var res = await admin.PostAsJsonAsync($"/api/employees/{emp.Id}/transfer", new TransferRequest
        {
            NewDepartmentId = 2,
            TransferDate = new DateOnly(2024, 5, 31),
            Reason = "Too early"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty("TransferDate", out _));

        var after = (await admin.GetFromJsonAsync<EmployeeDetails>($"/api/employees/{emp.Id}"))!;
        Assert.Equal(emp.EmployeeCode, after.EmployeeCode);
    }

    [Fact]
    public async Task Hard_delete_is_refused_when_employee_has_transfer_history()
    {
        var admin = await _factory.AdminClient();
        var emp = await Create(admin, NewEmployee(departmentId: 1));
        var transfer = await admin.PostAsJsonAsync($"/api/employees/{emp.Id}/transfer", new TransferRequest
        {
            NewDepartmentId = 3,
            TransferDate = new DateOnly(2025, 2, 1),
            Reason = "Moving to finance"
        });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);

        var res = await admin.DeleteAsync($"/api/employees/{emp.Id}?hard=true");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);

        // employee and history are both still there
        var history = (await admin.GetFromJsonAsync<List<TransferHistoryItem>>($"/api/employees/{emp.Id}/transfers"))!;
        Assert.Single(history);
    }

    [Fact]
    public async Task Parallel_transfers_of_one_employee_never_give_500()
    {
        var admin = await _factory.AdminClient();
        var manager = await _factory.ManagerClient();

        for (var round = 0; round < 3; round++)
        {
            var emp = await Create(admin, NewEmployee(departmentId: 1));
            var responses = await Task.WhenAll(
                admin.PostAsJsonAsync($"/api/employees/{emp.Id}/transfer",
                    new TransferRequest { NewDepartmentId = 2, TransferDate = new DateOnly(2025, 3, 1), Reason = "First" }),
                manager.PostAsJsonAsync($"/api/employees/{emp.Id}/transfer",
                    new TransferRequest { NewDepartmentId = 2, TransferDate = new DateOnly(2025, 3, 1), Reason = "Second" }));

            Assert.All(responses, r => Assert.NotEqual(HttpStatusCode.InternalServerError, r.StatusCode));
            // both asked for HR, so only one of them can succeed
            Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));

            var history = (await admin.GetFromJsonAsync<List<TransferHistoryItem>>($"/api/employees/{emp.Id}/transfers"))!;
            Assert.Single(history);
        }
    }

    [Fact]
    public async Task Manager_cannot_deactivate_through_update()
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        var emp = await Create(admin, req);

        var manager = await _factory.ManagerClient();
        req.Status = "Inactive";
        var res = await manager.PutAsJsonAsync($"/api/employees/{emp.Id}", req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        // other edits are still fine
        req.Status = "OnLeave";
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/employees/{emp.Id}", req)).StatusCode);
    }

    [Fact]
    public async Task Manager_cannot_edit_their_own_record()
    {
        var manager = await _factory.ManagerClient();
        var me = (await List(manager, "search=Vikram%20Rao&pageSize=5")).Items.Single();
        var details = (await manager.GetFromJsonAsync<EmployeeDetails>($"/api/employees/{me.Id}"))!;

        var res = await manager.PutAsJsonAsync($"/api/employees/{me.Id}", new UpdateEmployeeRequest
        {
            FullName = details.FullName,
            Email = details.Email,
            Mobile = details.Mobile,
            Gender = details.Gender,
            DateOfBirth = details.PersonalDetails!.DateOfBirth,
            Address = details.PersonalDetails.Address,
            DepartmentId = details.DepartmentId,
            RoleId = details.RoleId,
            JoiningDate = details.JoiningDate,
            AnnualSalary = details.AnnualSalary * 2,
            Status = details.Status
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(450000.555)]
    public async Task Salary_with_more_than_two_decimals_gets_400(double salary)
    {
        var admin = await _factory.AdminClient();
        var req = NewEmployee();
        req.AnnualSalary = (decimal)salary;
        var res = await admin.PostAsJsonAsync("/api/employees", req);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Lookups_return_departments_and_roles()
    {
        var employee = await _factory.EmployeeClient();
        var departments = (await employee.GetFromJsonAsync<List<LookupItem>>("/api/departments"))!;
        var roles = (await employee.GetFromJsonAsync<List<LookupItem>>("/api/roles"))!;
        Assert.Equal(5, departments.Count);
        Assert.True(roles.Count >= 3);
    }
}
