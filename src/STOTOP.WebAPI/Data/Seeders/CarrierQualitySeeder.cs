using STOTOP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace STOTOP.WebAPI.Data.Seeders;

/// <summary>
/// 网点质控看板（Plan 3 看板 + 主数据认领）菜单与权限种子。
/// FID 段 1880-1899，作为 F类型=菜单 的二级分组容器挂在质量中心模块(1800)下（与 quality:rules/review 同构）。
/// </summary>
public static class CarrierQualitySeeder
{
    private const string Module = "CarrierQuality";

    public static void Migrate(STOTOPDbContext ctx)
    {
        MigrationRunner.RunMigrations(ctx, Module, new List<MigrationStep>
        {
            new(1, "网点质控看板：菜单组(菜单容器)+4子菜单+查看权限+admin角色绑定", MigrateV1),
        });
    }

    private static void MigrateV1(STOTOPDbContext ctx)
    {
        if (!SeederHelper.IsSqlServer(ctx)) return;
        ctx.Database.ExecuteSqlRaw(@"
-- === 网点质控看板 菜单组（挂质量中心模块 1800 下，类型=菜单作为二级分组容器，与 quality:rules/review 等同构；route/component 留空，靠子菜单出页面）===
IF NOT EXISTS (SELECT 1 FROM [SYS功能权限] WHERE [F编码]=N'quality:carrier' OR [FID]=1880)
    INSERT INTO [SYS功能权限] ([FID],[F名称],[F编码],[F类型],[F父ID],[F路由],[F组件路径],[F图标],[F排序],[F状态],[F是否可见])
    VALUES (1880, N'网点质控看板', N'quality:carrier', N'菜单', 1800, NULL, NULL, N'CarOutlined', 20, 1, 1);

-- === 4 子菜单 ===
IF NOT EXISTS (SELECT 1 FROM [SYS功能权限] WHERE [F编码]=N'quality:carrier:network' OR [FID]=1881)
    INSERT INTO [SYS功能权限] ([FID],[F名称],[F编码],[F类型],[F父ID],[F路由],[F组件路径],[F图标],[F排序],[F状态],[F是否可见])
    VALUES (1881, N'网点总览', N'quality:carrier:network', N'菜单', 1880, N'/quality/carrier/network', N'quality/carrier/NetworkOverview', NULL, 1, 1, 1);

IF NOT EXISTS (SELECT 1 FROM [SYS功能权限] WHERE [F编码]=N'quality:carrier:employee' OR [FID]=1882)
    INSERT INTO [SYS功能权限] ([FID],[F名称],[F编码],[F类型],[F父ID],[F路由],[F组件路径],[F图标],[F排序],[F状态],[F是否可见])
    VALUES (1882, N'员工质量', N'quality:carrier:employee', N'菜单', 1880, N'/quality/carrier/employee', N'quality/carrier/EmployeeQuality', NULL, 2, 1, 1);

IF NOT EXISTS (SELECT 1 FROM [SYS功能权限] WHERE [F编码]=N'quality:carrier:issues' OR [FID]=1883)
    INSERT INTO [SYS功能权限] ([FID],[F名称],[F编码],[F类型],[F父ID],[F路由],[F组件路径],[F图标],[F排序],[F状态],[F是否可见])
    VALUES (1883, N'问题件追踪', N'quality:carrier:issues', N'菜单', 1880, N'/quality/carrier/issues', N'quality/carrier/IssueTracking', NULL, 3, 1, 1);

IF NOT EXISTS (SELECT 1 FROM [SYS功能权限] WHERE [F编码]=N'quality:carrier:claim' OR [FID]=1884)
    INSERT INTO [SYS功能权限] ([FID],[F名称],[F编码],[F类型],[F父ID],[F路由],[F组件路径],[F图标],[F排序],[F状态],[F是否可见])
    VALUES (1884, N'主数据认领', N'quality:carrier:claim', N'菜单', 1880, N'/quality/carrier/claim', N'quality/carrier/MasterDataClaim', NULL, 4, 1, 1);

-- === 看板只读功能权限（不可见按钮行，供 [RequirePermission] 消费）===
IF NOT EXISTS (SELECT 1 FROM [SYS功能权限] WHERE [F编码]=N'quality:carrier:view' OR [FID]=1885)
    INSERT INTO [SYS功能权限] ([FID],[F名称],[F编码],[F类型],[F父ID],[F路由],[F组件路径],[F图标],[F排序],[F状态],[F是否可见])
    VALUES (1885, N'网点质控看板-查看', N'quality:carrier:view', N'按钮', 1880, NULL, NULL, NULL, 10, 1, 0);

-- === admin 角色(FID=1)绑定全部 6 个权限 ===
INSERT INTO [SYS角色权限] ([F角色ID],[F权限ID])
SELECT 1, p.FID FROM [SYS功能权限] p
WHERE p.[F编码] IN (N'quality:carrier', N'quality:carrier:network', N'quality:carrier:employee', N'quality:carrier:issues', N'quality:carrier:claim', N'quality:carrier:view')
AND NOT EXISTS (SELECT 1 FROM [SYS角色权限] WHERE [F角色ID]=1 AND [F权限ID]=p.FID);
");
    }
}
