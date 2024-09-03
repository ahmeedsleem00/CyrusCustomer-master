using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyrusCustomer.DAL.Migrations
{
    public partial class AddNewInTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountOfBranches",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Month",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Year",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountOfBranches",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Customers");
        }
    }
}
