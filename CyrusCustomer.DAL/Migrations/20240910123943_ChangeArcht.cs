using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyrusCustomer.DAL.Migrations
{
    public partial class ChangeArcht : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerUserAssignments_CustomerId",
                table: "CustomerUserAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserAssignments_CustomerId",
                table: "CustomerUserAssignments",
                column: "CustomerId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerUserAssignments_CustomerId",
                table: "CustomerUserAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserAssignments_CustomerId",
                table: "CustomerUserAssignments",
                column: "CustomerId");
        }
    }
}
