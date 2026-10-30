using EmployeeManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeePersonalDetails> EmployeePersonalDetails => Set<EmployeePersonalDetails>();
    public DbSet<EmployeeCodeSequence> EmployeeCodeSequences => Set<EmployeeCodeSequence>();
    public DbSet<EmployeeTransferHistory> EmployeeTransferHistory => Set<EmployeeTransferHistory>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(e =>
        {
            e.Property(d => d.Name).HasMaxLength(50).IsRequired();
            e.Property(d => d.CodePrefix).HasMaxLength(1).IsFixedLength().IsRequired();
            e.HasIndex(d => d.Name).IsUnique();
            e.HasIndex(d => d.CodePrefix).IsUnique();
            e.HasData(
                new Department { Id = 1, Name = "IT", CodePrefix = "I" },
                new Department { Id = 2, Name = "HR", CodePrefix = "H" },
                new Department { Id = 3, Name = "Finance", CodePrefix = "F" },
                new Department { Id = 4, Name = "Sales", CodePrefix = "S" },
                new Department { Id = 5, Name = "Admin", CodePrefix = "A" });
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(r => r.Name).IsUnique();
            e.HasData(
                new Role { Id = 1, Name = "Developer" },
                new Role { Id = 2, Name = "Manager" },
                new Role { Id = 3, Name = "Analyst" },
                new Role { Id = 4, Name = "Executive" },
                new Role { Id = 5, Name = "HR Executive" });
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(x => x.EmployeeCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.Mobile).HasMaxLength(10).IsRequired();
            e.Property(x => x.Gender).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasMaxLength(10).IsRequired();
            e.Property(x => x.AnnualSalary).HasColumnType("decimal(12,2)");

            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.FullName);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.JoiningDate);
            e.HasIndex(x => x.AnnualSalary);

            e.HasOne(x => x.Department).WithMany(d => d.Employees)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Role).WithMany(r => r.Employees)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Employees_AnnualSalary", "[AnnualSalary] > 0");
                t.HasCheckConstraint("CK_Employees_Status", "[Status] IN ('Active','Inactive','OnLeave')");
                t.HasCheckConstraint("CK_Employees_Gender", "[Gender] IN ('Male','Female','Other')");
            });
        });

        modelBuilder.Entity<EmployeePersonalDetails>(e =>
        {
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.Address).HasMaxLength(300).IsRequired();
            e.Property(x => x.AadhaarEncrypted).HasMaxLength(200).IsRequired();
            e.Property(x => x.AadhaarLast4).HasMaxLength(4).IsFixedLength().IsRequired();
            e.Property(x => x.PanEncrypted).HasMaxLength(200).IsRequired();
            e.Property(x => x.PanMasked).HasMaxLength(10).IsRequired();
            e.HasOne(x => x.Employee).WithOne(emp => emp.PersonalDetails)
                .HasForeignKey<EmployeePersonalDetails>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeCodeSequence>(e =>
        {
            e.HasKey(x => x.DepartmentId);
            e.HasOne(x => x.Department).WithOne()
                .HasForeignKey<EmployeeCodeSequence>(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasData(
                new EmployeeCodeSequence { DepartmentId = 1, LastNumber = 0 },
                new EmployeeCodeSequence { DepartmentId = 2, LastNumber = 0 },
                new EmployeeCodeSequence { DepartmentId = 3, LastNumber = 0 },
                new EmployeeCodeSequence { DepartmentId = 4, LastNumber = 0 },
                new EmployeeCodeSequence { DepartmentId = 5, LastNumber = 0 });
        });

        modelBuilder.Entity<EmployeeTransferHistory>(e =>
        {
            e.Property(x => x.OldEmployeeCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.NewEmployeeCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.EmployeeId);

            e.HasOne(x => x.Employee).WithMany(emp => emp.Transfers)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OldDepartment).WithMany()
                .HasForeignKey(x => x.OldDepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NewDepartment).WithMany()
                .HasForeignKey(x => x.NewDepartmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TransferredBy).WithMany()
                .HasForeignKey(x => x.TransferredByUserId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.HasOne(x => x.Employee).WithMany()
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
