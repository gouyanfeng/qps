using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePurchaseDemandMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE dbo.SystemPermissions SET Name = N'采购列表' WHERE Id = 'A8623F7A-A38A-52F8-A14B-B4F9BC7C86E5'");
            migrationBuilder.Sql("UPDATE dbo.SystemPermissions SET Name = N'维护采购列表' WHERE Id = 'B935D9CC-D9AE-5605-9F90-7562992792BC'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE dbo.SystemPermissions SET Name = N'采购需求' WHERE Id = 'A8623F7A-A38A-52F8-A14B-B4F9BC7C86E5'");
            migrationBuilder.Sql("UPDATE dbo.SystemPermissions SET Name = N'维护采购需求' WHERE Id = 'B935D9CC-D9AE-5605-9F90-7562992792BC'");
        }
    }
}
