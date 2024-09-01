using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyrusCustomer.DAL.Migrations
{
    public partial class changeTableName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VersionUpdated",
                table: "Customers",
                newName: "ResponsiblePerson");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResponsiblePerson",
                table: "Customers",
                newName: "VersionUpdated");
        }
    }
}
