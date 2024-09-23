using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyrusCustomer.DAL.Migrations
{
    public partial class RemoveCredential : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Credentials",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AspNetUsers");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Credentials",
                columns: new[] { "Id", "CustomerId", "Email", "Name", "Password" },
                values: new object[,]
                {
                    { 1, 0, "admin@Cyrus.com", "Admin", "Cyrus@2024" },
                    { 2, 0, "tony@Cyrus.com", "Tony", "Cyrus@2024" },
                    { 3, 0, "mahmoud@Cyrus.com", "Mahmoud", "Cyrus@2024" },
                    { 4, 0, "mina@Cyrus.com", "Mina", "Cyrus@2024" },
                    { 5, 0, "mohamad@Cyrus.com", "Mohamad", "Cyrus@2024" },
                    { 6, 0, "amr@Cyrus.com", "Amro", "Cyrus@2024" },
                    { 7, 0, "youssef@Cyrus.com", "Youssef", "Cyrus@2024" },
                    { 8, 0, "sameh@Cyrus.com", "Sameh", "Cyrus@2024" }
                });
        }
    }
}
