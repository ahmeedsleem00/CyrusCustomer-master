using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyrusCustomer.DAL.Migrations
{
    public partial class AddIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CustomerUserAssignments_CustomerId",
                table: "CustomerUserAssignments",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerUserAssignments_Customers_CustomerId",
                table: "CustomerUserAssignments",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerUserAssignments_Customers_CustomerId",
                table: "CustomerUserAssignments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerUserAssignments_CustomerId",
                table: "CustomerUserAssignments");
        }
    }
}
