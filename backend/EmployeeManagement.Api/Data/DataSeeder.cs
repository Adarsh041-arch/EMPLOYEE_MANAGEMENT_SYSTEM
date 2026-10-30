using EmployeeManagement.Api.DTOs;
using EmployeeManagement.Api.Models;
using EmployeeManagement.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Data;

// Adds sample employees, users and transfers the first time the app runs against an
// empty database. Departments, roles and code sequences come from the migration.
// Employees go through EmployeeService so codes are generated and Aadhaar/PAN are
// encrypted exactly like they would be through the API.
public class DataSeeder
{
    private readonly AppDbContext _db;
    private readonly EmployeeService _employees;
    private readonly TransferService _transfers;

    public DataSeeder(AppDbContext db, EmployeeService employees, TransferService transfers)
    {
        _db = db;
        _employees = employees;
        _transfers = transfers;
    }

    private record SeedEmployee(string Name, string Gender, int Dept, int Role, string Joined, decimal Salary,
        string Status, string Dob, string City, string Aadhaar, string Pan);

    // Dept: 1 IT, 2 HR, 3 Finance, 4 Sales, 5 Admin
    // Role: 1 Developer, 2 Manager, 3 Analyst, 4 Executive, 5 HR Executive
    private static readonly SeedEmployee[] SampleEmployees =
    {
        new("Rahul Sharma",    "Male",   1, 1, "2022-07-11",  650000, "Active",   "1997-04-18", "Pune",      "4521-8834-1290", "BQRPS4521K"),
        new("Priya Nair",      "Female", 1, 1, "2023-01-16",  480000, "Active",   "1999-09-02", "Kochi",     "3367-1209-5548", "CJMPN8812L"),
        new("Arjun Mehta",     "Male",   1, 2, "2019-03-04", 1850000, "Active",   "1986-12-25", "Mumbai",    "7781-4402-9915", "AKZPM2290Q"),
        new("Sneha Kulkarni",  "Female", 1, 3, "2021-10-01",  720000, "OnLeave",  "1995-06-30", "Nagpur",    "2290-6671-3304", "DFHPK6634B"),
        new("Karan Singh",     "Male",   1, 1, "2024-02-12",  350000, "Active",   "2001-01-14", "Jaipur",    "5512-0098-7761", "EGLPS1123M"),
        new("Ananya Iyer",     "Female", 2, 5, "2020-08-24",  420000, "Active",   "1994-03-11", "Chennai",   "6634-2201-8892", "FHKPI7745N"),
        new("Rohit Verma",     "Male",   2, 2, "2018-05-14", 1250000, "Active",   "1985-07-07", "Lucknow",   "1120-7743-6658", "GJLPV3367C"),
        new("Meera Joshi",     "Female", 2, 5, "2023-06-05",  280000, "Inactive", "2000-11-19", "Indore",    "8845-3312-0076", "HKMPJ9901D"),
        new("Vikram Rao",      "Male",   3, 2, "2017-11-20", 2100000, "Active",   "1983-02-28", "Hyderabad", "9903-5567-2214", "JLNPR4478E"),
        new("Pooja Desai",     "Female", 3, 3, "2021-04-12",  560000, "Active",   "1996-08-08", "Surat",     "4478-9012-3365", "KMOPD5589F"),
        new("Amit Patel",      "Male",   3, 3, "2022-09-19",  390000, "Active",   "1998-05-21", "Ahmedabad", "3301-4456-7789", "LNQPP6690G"),
        new("Neha Gupta",      "Female", 3, 4, "2020-01-06",  175000, "OnLeave",  "1993-10-03", "Delhi",     "2219-8834-5501", "MOQPG7701H"),
        new("Suresh Reddy",    "Male",   4, 2, "2016-07-01", 1500000, "Active",   "1981-04-15", "Bengaluru", "6678-1123-4490", "NPRPR8812J"),
        new("Divya Menon",     "Female", 4, 4, "2022-03-28",  310000, "Active",   "1997-12-12", "Thrissur",  "5567-2234-8801", "OQSPM9923K"),
        new("Manish Yadav",    "Male",   4, 4, "2023-08-14",  240000, "Active",   "2000-06-06", "Patna",     "7789-0012-3346", "PRTPY1134L"),
        new("Kavya Shetty",    "Female", 4, 4, "2019-12-02",  520000, "Inactive", "1992-01-27", "Mangaluru", "1123-5678-9902", "QSUPS2245M"),
        new("Imran Khan",      "Male",   4, 3, "2021-06-21",  880000, "Active",   "1990-09-09", "Bhopal",    "8890-1234-5567", "RTVPK3356N"),
        new("Lakshmi Pillai",  "Female", 5, 2, "2018-02-19", 1100000, "Active",   "1984-05-05", "Trivandrum","4456-7890-1123", "SUWPP4467P"),
        new("Deepak Chauhan",  "Male",   5, 4, "2024-05-06",  210000, "Active",   "2002-03-03", "Dehradun",  "3345-6789-0012", "TVXPC5578Q"),
        new("Ritika Bansal",   "Female", 5, 3, "2022-11-07",  460000, "OnLeave",  "1996-02-14", "Chandigarh","2234-5678-9901", "UWYPB6689R"),
        new("Sameer Kapoor",   "Other",  1, 3, "2020-10-12",  990000, "Active",   "1991-07-30", "Noida",     "1987-6543-2109", "VXZPK7790S"),
        new("Farah Ali",       "Female", 2, 4, "2024-01-22",  150000, "Active",   "2001-08-17", "Aligarh",   "9876-5432-1098", "WYAPA8801T"),
    };

    public async Task SeedAsync()
    {
        if (await _db.Employees.AnyAsync())
            return;

        var seedUser = new CurrentUser(0, "seed", UserRoles.Admin, null);
        var ids = new Dictionary<string, int>();

        for (var i = 0; i < SampleEmployees.Length; i++)
        {
            var s = SampleEmployees[i];
            var first = s.Name.Split(' ')[0].ToLowerInvariant();
            var last = s.Name.Split(' ')[1].ToLowerInvariant();
            var created = await _employees.CreateAsync(new EmployeeRequest
            {
                FullName = s.Name,
                Email = $"{first}.{last}@company.in",
                Mobile = "98" + (45210000 + i * 7919).ToString("D8"),
                Gender = s.Gender,
                Aadhaar = s.Aadhaar,
                Pan = s.Pan,
                DateOfBirth = DateOnly.Parse(s.Dob),
                Address = $"{10 + i * 3}, Main Road, {s.City}",
                DepartmentId = s.Dept,
                RoleId = s.Role,
                JoiningDate = DateOnly.Parse(s.Joined),
                AnnualSalary = s.Salary,
                Status = s.Status
            }, seedUser);
            ids[s.Name] = created.Id;
        }

        var admin = new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), Role = UserRoles.Admin };
        _db.Users.AddRange(
            admin,
            new User { Username = "manager", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"), Role = UserRoles.Manager, EmployeeId = ids["Vikram Rao"] },
            new User { Username = "employee", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"), Role = UserRoles.Employee, EmployeeId = ids["Priya Nair"] });
        await _db.SaveChangesAsync();

        await _transfers.TransferAsync(ids["Rahul Sharma"],
            new TransferRequest { NewDepartmentId = 2, TransferDate = new DateOnly(2024, 4, 1), Reason = "Moved to HR to handle the HRMS rollout" }, admin.Id);
        await _transfers.TransferAsync(ids["Amit Patel"],
            new TransferRequest { NewDepartmentId = 4, TransferDate = new DateOnly(2024, 9, 16), Reason = "Requested move to sales analytics" }, admin.Id);
        await _transfers.TransferAsync(ids["Kavya Shetty"],
            new TransferRequest { NewDepartmentId = 5, TransferDate = new DateOnly(2023, 2, 1), Reason = "Office administration needed support" }, admin.Id);
        await _transfers.TransferAsync(ids["Kavya Shetty"],
            new TransferRequest { NewDepartmentId = 4, TransferDate = new DateOnly(2023, 11, 6), Reason = "Returned to sales team" }, admin.Id);
    }
}
