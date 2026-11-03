using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallTagType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallTags_Code",
                table: "MallTags");

            migrationBuilder.DropIndex(
                name: "IX_MallTags_Name",
                table: "MallTags");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "MallTags",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Product");

            migrationBuilder.CreateIndex(
                name: "IX_MallTags_Type_Code",
                table: "MallTags",
                columns: new[] { "Type", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallTags_Type_Name",
                table: "MallTags",
                columns: new[] { "Type", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.Sql("""
                INSERT INTO [MallTags]
                    ([Id], [Type], [Code], [Name], [Color], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
                VALUES
                    ('B7A10000-0000-4000-8000-000000000001', N'Review', N'REVIEW-TAG-01', N'药材道地', N'#2E7D32', 1, 1, GETDATE(), N'system', GETDATE(), N'system', 0),
                    ('B7A10000-0000-4000-8000-000000000002', N'Review', N'REVIEW-TAG-02', N'包装精美', N'#2E7D32', 2, 1, GETDATE(), N'system', GETDATE(), N'system', 0),
                    ('B7A10000-0000-4000-8000-000000000003', N'Review', N'REVIEW-TAG-03', N'物流快', N'#2E7D32', 3, 1, GETDATE(), N'system', GETDATE(), N'system', 0),
                    ('B7A10000-0000-4000-8000-000000000004', N'Review', N'REVIEW-TAG-04', N'效果显著', N'#2E7D32', 4, 1, GETDATE(), N'system', GETDATE(), N'system', 0),
                    ('B7A10000-0000-4000-8000-000000000005', N'Review', N'REVIEW-TAG-05', N'客服专业', N'#2E7D32', 5, 1, GETDATE(), N'system', GETDATE(), N'system', 0),
                    ('B7A10000-0000-4000-8000-000000000006', N'Review', N'REVIEW-TAG-06', N'性价比高', N'#2E7D32', 6, 1, GETDATE(), N'system', GETDATE(), N'system', 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [MallTags]
                WHERE [Id] IN (
                    'B7A10000-0000-4000-8000-000000000001',
                    'B7A10000-0000-4000-8000-000000000002',
                    'B7A10000-0000-4000-8000-000000000003',
                    'B7A10000-0000-4000-8000-000000000004',
                    'B7A10000-0000-4000-8000-000000000005',
                    'B7A10000-0000-4000-8000-000000000006');
                """);

            migrationBuilder.DropIndex(
                name: "IX_MallTags_Type_Code",
                table: "MallTags");

            migrationBuilder.DropIndex(
                name: "IX_MallTags_Type_Name",
                table: "MallTags");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MallTags");

            migrationBuilder.CreateIndex(
                name: "IX_MallTags_Code",
                table: "MallTags",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallTags_Name",
                table: "MallTags",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
