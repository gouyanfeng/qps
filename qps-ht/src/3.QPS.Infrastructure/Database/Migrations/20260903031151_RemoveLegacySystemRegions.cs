using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacySystemRegions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemRegions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemRegions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRegions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemRegions_SystemRegions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SystemRegions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemRegions_ParentId",
                table: "SystemRegions",
                column: "ParentId");
        }
    }
}
