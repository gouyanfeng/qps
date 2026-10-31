using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameRegionCodesToNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CityCode",
                table: "MallAddresses");

            migrationBuilder.DropColumn(
                name: "DistrictCode",
                table: "MallAddresses");

            migrationBuilder.DropColumn(
                name: "ProvinceCode",
                table: "MallAddresses");

            migrationBuilder.RenameColumn(
                name: "ProvinceCodes",
                table: "MallFreightRegions",
                newName: "ProvinceNames");

            migrationBuilder.RenameColumn(
                name: "CityCodes",
                table: "MallFreightRegions",
                newName: "CityNames");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProvinceNames",
                table: "MallFreightRegions",
                newName: "ProvinceCodes");

            migrationBuilder.RenameColumn(
                name: "CityNames",
                table: "MallFreightRegions",
                newName: "CityCodes");

            migrationBuilder.AddColumn<string>(
                name: "CityCode",
                table: "MallAddresses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DistrictCode",
                table: "MallAddresses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProvinceCode",
                table: "MallAddresses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }
    }
}
