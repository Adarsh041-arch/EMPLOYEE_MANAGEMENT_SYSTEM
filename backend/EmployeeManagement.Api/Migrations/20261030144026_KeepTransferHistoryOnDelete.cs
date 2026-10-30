using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class KeepTransferHistoryOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeTransferHistory_Employees_EmployeeId",
                table: "EmployeeTransferHistory");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeTransferHistory_Employees_EmployeeId",
                table: "EmployeeTransferHistory",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeTransferHistory_Employees_EmployeeId",
                table: "EmployeeTransferHistory");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeTransferHistory_Employees_EmployeeId",
                table: "EmployeeTransferHistory",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
