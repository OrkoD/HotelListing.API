using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelListing.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameAdminsToHotelAdmins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Admins_AspNetUsers_UserId",
                table: "Admins");

            migrationBuilder.DropForeignKey(
                name: "FK_Admins_Hotels_HotelId",
                table: "Admins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Admins",
                table: "Admins");

            migrationBuilder.RenameTable(
                name: "Admins",
                newName: "HotelAdmins");

            migrationBuilder.RenameIndex(
                name: "IX_Admins_UserId",
                table: "HotelAdmins",
                newName: "IX_HotelAdmins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Admins_HotelId",
                table: "HotelAdmins",
                newName: "IX_HotelAdmins_HotelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HotelAdmins",
                table: "HotelAdmins",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HotelAdmins_AspNetUsers_UserId",
                table: "HotelAdmins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HotelAdmins_Hotels_HotelId",
                table: "HotelAdmins",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HotelAdmins_AspNetUsers_UserId",
                table: "HotelAdmins");

            migrationBuilder.DropForeignKey(
                name: "FK_HotelAdmins_Hotels_HotelId",
                table: "HotelAdmins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HotelAdmins",
                table: "HotelAdmins");

            migrationBuilder.RenameTable(
                name: "HotelAdmins",
                newName: "Admins");

            migrationBuilder.RenameIndex(
                name: "IX_HotelAdmins_UserId",
                table: "Admins",
                newName: "IX_Admins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_HotelAdmins_HotelId",
                table: "Admins",
                newName: "IX_Admins_HotelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Admins",
                table: "Admins",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Admins_AspNetUsers_UserId",
                table: "Admins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Admins_Hotels_HotelId",
                table: "Admins",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
