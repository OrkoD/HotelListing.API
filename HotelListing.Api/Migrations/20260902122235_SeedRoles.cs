using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HotelListing.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "4e3f4ba8-aea3-4a0c-92d6-048686e09153", "bd001357-fc51-4c0a-b385-408ca45b4efb", "Admin", "ADMIN" },
                    { "5028f678-7dea-4ca8-a1a5-a800ce770106", "25cb677d-53ab-4e25-8983-6c0e58997592", "User", "USER" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "4e3f4ba8-aea3-4a0c-92d6-048686e09153");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "5028f678-7dea-4ca8-a1a5-a800ce770106");
        }
    }
}
