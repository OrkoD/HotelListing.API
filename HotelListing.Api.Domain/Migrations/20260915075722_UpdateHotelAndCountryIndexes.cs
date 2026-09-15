using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelListing.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHotelAndCountryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hotels_Rating",
                table: "Hotels");

            migrationBuilder.DropIndex(
                name: "IX_Countries_ShortName",
                table: "Countries");

            migrationBuilder.RenameIndex(
                name: "IX_Hotels_CountryId_Name",
                table: "Hotels",
                newName: "IX_Hotels_CountryId_Name_Unique");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_CountryId_Rating",
                table: "Hotels",
                columns: new[] { "CountryId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_Name",
                table: "Hotels",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_ShortName",
                table: "Countries",
                column: "ShortName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hotels_CountryId_Rating",
                table: "Hotels");

            migrationBuilder.DropIndex(
                name: "IX_Hotels_Name",
                table: "Hotels");

            migrationBuilder.DropIndex(
                name: "IX_Countries_ShortName",
                table: "Countries");

            migrationBuilder.RenameIndex(
                name: "IX_Hotels_CountryId_Name_Unique",
                table: "Hotels",
                newName: "IX_Hotels_CountryId_Name");

            migrationBuilder.CreateIndex(
                name: "IX_Hotels_Rating",
                table: "Hotels",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_ShortName",
                table: "Countries",
                column: "ShortName");
        }
    }
}
