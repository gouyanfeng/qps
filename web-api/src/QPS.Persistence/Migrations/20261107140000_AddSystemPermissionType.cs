using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QPS.Persistence.Database;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <summary>
    /// 给 SystemPermissions 增加权限类型列，并按权限码命名规律回填存量数据。
    /// 菜单：页面/路由级权限码（管理端 authMenuList.json 中挂载的 permissionCode）以及无操作后缀的分组码；
    /// 按钮：以 _ADD / _EDIT / _DELETE / _ASSIGN / _STATUS / _PRIMARY / _MANAGE / _VIEW / _TRANSFER / _TASK 结尾的操作级权限码。
    /// 例外（按实际引用位置判定）：CRM_FOLLOW_TASK、CRM_PURCHASE_DEMAND_VIEW、CRM_MATCH_OPPORTUNITY_VIEW
    /// 虽带操作后缀，但实际作为页面菜单挂载；CRM_FOLLOW 虽无操作后缀，但实际被「记录跟进」按钮使用。
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261107140000_AddSystemPermissionType")]
    public partial class AddSystemPermissionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PermissionType",
                table: "SystemPermissions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Menu");

            migrationBuilder.Sql(@"
UPDATE [SystemPermissions]
SET [PermissionType] = CASE
    WHEN [Code] IN (N'CRM_FOLLOW_TASK', N'CRM_PURCHASE_DEMAND_VIEW', N'CRM_MATCH_OPPORTUNITY_VIEW') THEN N'Menu'
    WHEN [Code] = N'CRM_FOLLOW' THEN N'Button'
    WHEN [Code] LIKE N'%[_]ADD' OR [Code] LIKE N'%[_]EDIT' OR [Code] LIKE N'%[_]DELETE'
        OR [Code] LIKE N'%[_]ASSIGN' OR [Code] LIKE N'%[_]STATUS' OR [Code] LIKE N'%[_]PRIMARY'
        OR [Code] LIKE N'%[_]MANAGE' OR [Code] LIKE N'%[_]VIEW' OR [Code] LIKE N'%[_]TRANSFER'
        OR [Code] LIKE N'%[_]TASK' THEN N'Button'
    ELSE N'Menu'
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermissionType",
                table: "SystemPermissions");
        }
    }
}
