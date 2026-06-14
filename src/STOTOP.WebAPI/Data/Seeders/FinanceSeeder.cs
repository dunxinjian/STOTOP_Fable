using Microsoft.EntityFrameworkCore;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.Finance.Entities;
using STOTOP.Module.System.Entities;

namespace STOTOP.WebAPI.Data.Seeders;

public static class FinanceSeeder
{
    private const string Module = "Finance";

    /// <summary>
    /// 统一走 SeederHelper.ExecuteRawSql（纯 ADO，不经 EF 的 String.Format），
    /// SQL 字面量中的大括号（JSON、模板占位符）无需转义。
    /// </summary>
    private static void ExecSql(STOTOPDbContext ctx, string sql)
    {
        SeederHelper.ExecuteRawSql(ctx, sql);
    }

    public static void Migrate(STOTOPDbContext ctx)
    {
        var steps = new List<MigrationStep>
        {
            new(1, "数据库基线 (2026-05-26)", MigrateV1),
            new(2, "清理残留废弃列 (2026-05-26)", MigrateV2),
            new(3, "损益项字段数据迁移 (2026-05-27)", MigrateV3),
            new(4, "指标分区标记收敛为根级 (2026-06-12)", MigrateV4),
        };
        MigrationRunner.RunMigrations(ctx, Module, steps);
    }

    private static void MigrateV1(STOTOPDbContext ctx)
    {
        if (!SeederHelper.IsSqlServer(ctx)) return;

        // FIN账套
        ExecSql(ctx, @"
        SET IDENTITY_INSERT [FIN账套] ON;
        
        IF NOT EXISTS (SELECT 1 FROM [FIN账套] WHERE [FID] = 1)
        
        INSERT INTO [FIN账套] ([FID], [F名称], [F编码], [F法人名称], [F说明], [F是否默认], [F状态], [F排序], [F起始年份], [F起始月份], [F组织ID], [F创建时间], [F更新时间]) VALUES (1, N'石家庄申通2025', N'SJZ2025', N'石家庄申通', N'石家庄申通2025年度账套', 1, 1, 1, 2025, 1, 2, N'2026-04-20 09:08:02.793', N'2026-05-07 11:13:31.148');
        
        IF NOT EXISTS (SELECT 1 FROM [FIN账套] WHERE [FID] = 2)
        
        INSERT INTO [FIN账套] ([FID], [F名称], [F编码], [F法人名称], [F说明], [F是否默认], [F状态], [F排序], [F起始年份], [F起始月份], [F组织ID], [F创建时间], [F更新时间]) VALUES (2, N'太仓美申2025', N'TC2025', N'太仓美申', N'太仓美申2025年度账套', 0, 1, 2, 2025, 1, 192, N'2026-04-20 09:08:02.793', N'2026-04-20 09:08:02.793');
        
        SET IDENTITY_INSERT [FIN账套] OFF;
        
        ");

        // FIN科目模板
        ExecSql(ctx, @"
        SET IDENTITY_INSERT [FIN科目模板] ON;
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目模板] WHERE [FID] = 1)
        
        INSERT INTO [FIN科目模板] ([FID], [F编码], [F名称], [F说明], [F是否预置], [F启用状态], [F组织ID], [F创建时间], [F更新时间]) VALUES (1, N'SJZ_ST', N'石家庄申通', NULL, 1, 1, 0, N'2026-04-20 09:08:02.090', N'2026-04-20 09:08:02.090');
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目模板] WHERE [FID] = 2)
        
        INSERT INTO [FIN科目模板] ([FID], [F编码], [F名称], [F说明], [F是否预置], [F启用状态], [F组织ID], [F创建时间], [F更新时间]) VALUES (2, N'TC_MS', N'太仓美申', NULL, 1, 1, 0, N'2026-04-20 09:08:02.090', N'2026-04-20 09:08:02.090');
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目模板] WHERE [FID] = 3)
        
        INSERT INTO [FIN科目模板] ([FID], [F编码], [F名称], [F说明], [F是否预置], [F启用状态], [F组织ID], [F创建时间], [F更新时间]) VALUES (3, N'express-delivery', N'快递行业标准模板', N'源自石家庄申通2025账套快照', 1, 1, 0, N'2026-05-19 00:41:06.333', N'2026-05-19 00:41:06.333');
        
        SET IDENTITY_INSERT [FIN科目模板] OFF;
        
        ");

        // FIN科目
        ExecSql(ctx, @"
        SET IDENTITY_INSERT [FIN科目] ON;
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200427)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200427, N'1001', N'库存现金', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200428)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200428, N'1002', N'银行存款', N'流动资产', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200429)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200429, N'100201', N'中行对公-友谊北支行', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200430)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200430, N'100202', N'中行对公-机场路支行', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200431)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200431, N'100203', N'工行对公-长安支行', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200432)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200432, N'100204', N'邮储对公-裕东支行', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200433)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200433, N'100205', N'农行对公-广安支行', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200434)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200434, N'100206', N'建行-张彦', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200435)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200435, N'100207', N'农行-张彦', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200436)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200436, N'100208', N'支付宝-张彦', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200437)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200437, N'100209', N'支付宝-对公', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200438)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200438, N'100210', N'中行-名门', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200439)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200439, N'100211', N'中行-鼎坚', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200440)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200440, N'100212', N'中行-博雅', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200441)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200441, N'100213', N'中行-友谊', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200442)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200442, N'100214', N'中行-谊北', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200443)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200443, N'100215', N'中行-天佑', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200444)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200444, N'100216', N'中行-新华', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200445)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200445, N'100217', N'中行-长兴', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200446)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200446, N'100218', N'中行-柳源', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200447)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200447, N'100219', N'中行-银都', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200448)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200448, N'100220', N'中行-建设', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200449)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200449, N'100221', N'中行-民生', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200450)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200450, N'100222', N'中行-红旗', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200451)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200451, N'100223', N'中行-汇华', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200452)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200452, N'100224', N'中行-鑫科', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200453)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200453, N'100225', N'中行-瑞府', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200454)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200454, N'100226', N'中行-翟营', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200455)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200455, N'100227', N'中行-东开', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200456)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200456, N'100228', N'中行-高新', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200457)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200457, N'100229', N'中行-鹿泉', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200458)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200458, N'100230', N'中行-凌透', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200459)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200459, N'100231', N'中行-西营', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200460)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200460, N'100232', N'中行-建华', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200461)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200461, N'100233', N'中行-平安', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200462)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200462, N'100234', N'中行-正定', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200463)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200463, N'100235', N'中行-宁晋', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200464)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200464, N'100236', N'中行-栾城', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200465)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200465, N'100237', N'中行-时光', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200466)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200466, N'100238', N'中行-炼油厂', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200467)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200467, N'100239', N'兴业-藁城', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200468)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200468, N'100240', N'兴业-炼油厂', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200469)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200469, N'100241', N'中行-藁定', N'流动资产', N'借', 2, 200428, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200470)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200470, N'1012', N'其他货币资金', N'流动资产', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200471)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200471, N'101201', N'申通总部-石家庄', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200472)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200472, N'101202', N'申通总部-名门', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200473)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200473, N'101203', N'申通总部-鼎坚', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200474)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200474, N'101204', N'申通总部-博雅', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200475)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200475, N'101205', N'申通总部-友谊', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200476)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200476, N'101206', N'申通总部-谊北', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200477)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200477, N'101207', N'申通总部-天佑', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200478)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200478, N'101208', N'申通总部-新华', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200479)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200479, N'101209', N'申通总部-长兴', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200480)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200480, N'101210', N'申通总部-柳源', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200481)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200481, N'101211', N'申通总部-银都', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200482)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200482, N'101212', N'申通总部-建设', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200483)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200483, N'101213', N'申通总部-民生', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200484)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200484, N'101214', N'申通总部-红旗', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200485)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200485, N'101215', N'申通总部-汇华', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200486)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200486, N'101216', N'申通总部-鑫科', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200487)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200487, N'101217', N'申通总部-瑞府', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200488)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200488, N'101218', N'申通总部-翟营', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200489)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200489, N'101219', N'申通总部-东开', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200490)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200490, N'101220', N'申通总部-高新', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200491)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200491, N'101221', N'申通总部-鹿泉', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200492)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200492, N'101222', N'申通总部-凌透', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200493)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200493, N'101223', N'申通总部-西营', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200494)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200494, N'101224', N'申通总部-建华', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200495)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200495, N'101225', N'申通总部-平安', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200496)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200496, N'101226', N'申通总部-正定', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200497)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200497, N'101227', N'申通总部-宁晋', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200498)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200498, N'101228', N'申通总部-栾城', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200499)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200499, N'101229', N'申通总部-时光', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200500)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200500, N'101230', N'申通总部-藁城', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200501)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200501, N'101231', N'申通总部-炼油厂', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200502)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200502, N'101232', N'申通总部-藁定', N'流动资产', N'借', 2, 200470, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200503)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200503, N'1101', N'短期投资', N'流动资产', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200504)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200504, N'110101', N'股票', N'流动资产', N'借', 2, 200503, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200505)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200505, N'110102', N'债券', N'流动资产', N'借', 2, 200503, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200506)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200506, N'110103', N'基金', N'流动资产', N'借', 2, 200503, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200507)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200507, N'1121', N'应收票据', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200508)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200508, N'1122', N'应收账款', N'流动资产', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200509)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200509, N'112201', N'月结账款', N'流动资产', N'借', 2, 200508, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200510)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200510, N'112202', N'客户账款', N'流动资产', N'借', 2, 200508, 1, N'客户', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200511)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200511, N'112210', N'其他账款', N'流动资产', N'借', 2, 200508, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200512)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200512, N'1123', N'预付账款', N'流动资产', N'借', 1, 0, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200513)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200513, N'1131', N'应收股利', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200514)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200514, N'1132', N'应收利息', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200515)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200515, N'1221', N'其他应收款', N'流动资产', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200516)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200516, N'122101', N'借款', N'流动资产', N'借', 2, 200515, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200517)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200517, N'122102', N'押金', N'流动资产', N'借', 2, 200515, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200518)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200518, N'12210201', N'房租押金', N'流动资产', N'借', 3, 200517, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200519)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200519, N'12210210', N'其他押金', N'流动资产', N'借', 3, 200517, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200520)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200520, N'122103', N'李青', N'流动资产', N'借', 2, 200515, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200521)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200521, N'122104', N'保证金', N'流动资产', N'借', 2, 200515, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200522)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200522, N'122105', N'其他项目垫付款', N'流动资产', N'借', 2, 200515, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200523)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200523, N'122106', N'美申集团', N'流动资产', N'借', 2, 200515, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200524)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200524, N'1401', N'材料采购', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200525)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200525, N'1402', N'在途物资', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200526)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200526, N'1403', N'原材料', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200527)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200527, N'1404', N'材料成本差异', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200528)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200528, N'1405', N'库存商品', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200529)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200529, N'1406', N'发出商品', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200530)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200530, N'1407', N'商品进销差价', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200531)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200531, N'1408', N'委托加工物资', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200532)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200532, N'1411', N'周转材料', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200533)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200533, N'1421', N'消耗性生物资产', N'流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200534)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200534, N'1501', N'待摊费用', N'非流动资产', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200535)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200535, N'150101', N'房租费', N'非流动资产', N'借', 2, 200534, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200536)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200536, N'150102', N'物业费', N'非流动资产', N'借', 2, 200534, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200537)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200537, N'150103', N'停车费', N'非流动资产', N'借', 2, 200534, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200538)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200538, N'150104', N'信息系统费用', N'非流动资产', N'借', 2, 200534, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200539)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200539, N'150105', N'采暖费', N'非流动资产', N'借', 2, 200534, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200540)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200540, N'150106', N'宽带费', N'非流动资产', N'借', 2, 200534, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200541)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200541, N'1502', N'长期债券投资减值准备', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200542)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200542, N'1511', N'长期股权投资', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200543)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200543, N'1512', N'长期股权投资减值准备', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200544)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200544, N'1521', N'投资性房地产', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200545)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200545, N'1522', N'投资性房地产累计折旧', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200546)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200546, N'1523', N'投资性房地产减值准备', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200547)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200547, N'1526', N'投资性房地产清理', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200548)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200548, N'1531', N'长期应收款', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200549)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200549, N'1601', N'固定资产', N'非流动资产', N'借', 1, 0, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200550)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200550, N'1602', N'累计折旧', N'非流动资产', N'贷', 1, 0, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200551)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200551, N'1603', N'固定资产减值准备', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200552)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200552, N'1604', N'在建工程', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200553)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200553, N'1605', N'工程物资', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200554)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200554, N'1606', N'固定资产清理', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200555)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200555, N'1621', N'生产性生物资产', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200556)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200556, N'1622', N'生产性生物资产累计折旧', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200557)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200557, N'1623', N'生产性生物资产减值准备', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200558)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200558, N'1701', N'无形资产', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200559)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200559, N'1702', N'累计摊销', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200560)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200560, N'1703', N'无形资产减值准备', N'非流动资产', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200561)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200561, N'1706', N'无形资产清理', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200562)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200562, N'1801', N'长期待摊费用', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200563)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200563, N'1901', N'待处理财产损溢', N'非流动资产', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200564)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200564, N'2001', N'短期借款', N'流动负债', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200565)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200565, N'200101', N'河北银行', N'流动负债', N'贷', 2, 200564, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200566)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200566, N'200102', N'邮储银行', N'流动负债', N'贷', 2, 200564, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200567)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200567, N'200103', N'申通总部', N'流动负债', N'贷', 2, 200564, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200568)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200568, N'200104', N'工行长安支行', N'流动负债', N'贷', 2, 200564, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200569)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200569, N'200105', N'其他', N'流动负债', N'贷', 2, 200564, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200570)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200570, N'2201', N'应付票据', N'流动负债', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200571)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200571, N'2202', N'应付账款', N'流动负债', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200572)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200572, N'220201', N'其他应付款', N'流动负债', N'贷', 2, 200571, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200573)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200573, N'2203', N'预收账款', N'流动负债', N'贷', 1, 0, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200574)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200574, N'2211', N'应付职工薪酬', N'流动负债', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200575)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200575, N'221101', N'工资【正式工】', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200576)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200576, N'221102', N'工资【临时工】', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200577)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200577, N'221103', N'职工福利', N'流动负债', N'贷', 2, 200574, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200578)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200578, N'22110301', N'社会保险', N'流动负债', N'贷', 3, 200577, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200579)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200579, N'22110302', N'商业保险', N'流动负债', N'贷', 3, 200577, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200580)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200580, N'221105', N'住房公积金', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200581)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200581, N'221106', N'工会经费', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200582)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200582, N'221107', N'职工教育经费', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200583)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200583, N'221108', N'非货币性福利', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200584)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200584, N'221109', N'辞退福利', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200585)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200585, N'221199', N'其他', N'流动负债', N'贷', 2, 200574, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200586)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200586, N'2221', N'应交税费', N'流动负债', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200587)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200587, N'222101', N'应交增值税', N'流动负债', N'贷', 2, 200586, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200588)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200588, N'22210101', N'进项税额', N'流动负债', N'借', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200589)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200589, N'22210102', N'销项税额抵减', N'流动负债', N'借', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200590)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200590, N'22210103', N'已交税金', N'流动负债', N'借', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200591)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200591, N'22210104', N'转出未交增值税', N'流动负债', N'借', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200592)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200592, N'22210105', N'减免税款', N'流动负债', N'借', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200593)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200593, N'22210106', N'出口抵内销', N'流动负债', N'借', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200594)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200594, N'22210107', N'销项税额', N'流动负债', N'贷', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200595)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200595, N'22210108', N'出口退税', N'流动负债', N'贷', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200596)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200596, N'22210109', N'进项税额转出', N'流动负债', N'贷', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200597)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200597, N'22210110', N'转出多交增值税', N'流动负债', N'贷', 3, 200587, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200598)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200598, N'222102', N'未交增值税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200599)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200599, N'222103', N'预交增值税', N'流动负债', N'借', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200600)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200600, N'222104', N'待抵扣进项税额', N'流动负债', N'借', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200601)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200601, N'222105', N'待认证进项税额', N'流动负债', N'借', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200602)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200602, N'222106', N'待转销项税额', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200603)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200603, N'222107', N'增值税留抵税额', N'流动负债', N'借', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200604)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200604, N'222108', N'简易计税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200605)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200605, N'222109', N'转出金融商品增值税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200606)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200606, N'222110', N'代扣代交增值税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200607)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200607, N'222111', N'消费税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200608)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200608, N'222112', N'城市维护建设税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200609)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200609, N'222113', N'教育费附加', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200610)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200610, N'222114', N'地方教育附加', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200611)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200611, N'222115', N'土地增值税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200612)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200612, N'222116', N'土地使用税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200613)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200613, N'222117', N'房产税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200614)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200614, N'222118', N'车船税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200615)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200615, N'222119', N'资源税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200616)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200616, N'222120', N'矿产资源补偿费', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200617)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200617, N'222121', N'排污费', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200618)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200618, N'222124', N'企业所得税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200619)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200619, N'222125', N'代扣代交所得税', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200620)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200620, N'222199', N'其他', N'流动负债', N'贷', 2, 200586, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200621)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200621, N'2231', N'应付利息', N'流动负债', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200622)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200622, N'2232', N'应付利润', N'流动负债', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200623)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200623, N'2241', N'其他应付款', N'流动负债', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200624)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200624, N'224102', N'风险保证金', N'流动负债', N'贷', 2, 200623, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200625)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200625, N'224104', N'三轮车保险赔偿款', N'流动负债', N'贷', 2, 200623, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200626)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200626, N'224106', N'生育津贴', N'流动负债', N'贷', 2, 200623, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200627)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200627, N'224107', N'代理点保证金', N'流动负债', N'贷', 2, 200623, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200628)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200628, N'2401', N'递延收益', N'非流动负债', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200629)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200629, N'2501', N'长期借款', N'非流动负债', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200630)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200630, N'2701', N'长期应付款', N'非流动负债', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200631)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200631, N'3001', N'实收资本', N'所有者权益', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200632)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200632, N'300101', N'李青', N'所有者权益', N'贷', 2, 200631, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200633)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200633, N'300102', N'敦新建', N'所有者权益', N'贷', 2, 200631, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200634)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200634, N'3002', N'资本公积', N'所有者权益', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200635)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200635, N'3101', N'盈余公积', N'所有者权益', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200636)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200636, N'310101', N'法定盈余公积', N'所有者权益', N'贷', 2, 200635, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200637)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200637, N'310102', N'任意盈余公积', N'所有者权益', N'贷', 2, 200635, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200638)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200638, N'3103', N'本年利润', N'所有者权益', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200639)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200639, N'3104', N'利润分配', N'所有者权益', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200640)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200640, N'310401', N'提取法定盈余公积', N'所有者权益', N'借', 2, 200639, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200641)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200641, N'310402', N'提取任意盈余公积', N'所有者权益', N'借', 2, 200639, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200642)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200642, N'310403', N'应付利润', N'所有者权益', N'借', 2, 200639, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200643)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200643, N'310404', N'转作资本的利润', N'所有者权益', N'借', 2, 200639, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200644)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200644, N'310405', N'未分配利润', N'所有者权益', N'贷', 2, 200639, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200645)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200645, N'4001', N'生产成本', N'成本', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200646)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200646, N'4101', N'制造费用', N'成本', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200647)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200647, N'4201', N'劳务成本', N'成本', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200648)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200648, N'4301', N'研发支出', N'成本', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200649)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200649, N'4401', N'工程施工', N'成本', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200650)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200650, N'440101', N'合同成本', N'成本', N'借', 2, 200649, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200651)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200651, N'440102', N'间接费用', N'成本', N'借', 2, 200649, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200652)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200652, N'440103', N'合同毛利', N'成本', N'借', 2, 200649, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200653)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200653, N'4402', N'工程结算', N'成本', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200654)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200654, N'4403', N'机械作业', N'成本', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200655)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200655, N'5001', N'主营业务收入', N'营业收入', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200656)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200656, N'500101', N'出港现付收入', N'营业收入', N'贷', 2, 200655, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200657)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200657, N'500102', N'出港月结收入', N'营业收入', N'贷', 2, 200655, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200658)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200658, N'500103', N'派费', N'营业收入', N'贷', 2, 200655, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200659)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200659, N'50010301', N'基础派费', N'营业收入', N'贷', 3, 200658, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200660)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200660, N'50010302', N'补贴派费', N'营业收入', N'贷', 3, 200658, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200661)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200661, N'50010303', N'大货业务', N'营业收入', N'贷', 3, 200658, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200662)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200662, N'50010304', N'调整派费', N'营业收入', N'贷', 3, 200658, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200663)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200663, N'50010305', N'周期性派费', N'营业收入', N'贷', 3, 200658, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200664)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200664, N'50010306', N'按需派费', N'营业收入', N'贷', 3, 200658, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200665)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200665, N'50010307', N'小件员权益', N'营业收入', N'贷', 3, 200658, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200666)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200666, N'500104', N'面单', N'营业收入', N'贷', 2, 200655, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200667)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200667, N'50010401', N'政策考核', N'营业收入', N'贷', 3, 200666, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200668)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200668, N'50010402', N'超商活动返利', N'营业收入', N'贷', 3, 200666, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200669)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200669, N'50010403', N'一口价', N'营业收入', N'贷', 3, 200666, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200670)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200670, N'500105', N'中转', N'营业收入', N'贷', 2, 200655, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200671)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200671, N'50010501', N'操作费', N'营业收入', N'贷', 3, 200670, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200672)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200672, N'50010502', N'全网出港费', N'营业收入', N'贷', 3, 200670, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200673)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200673, N'50010503', N'中转费', N'营业收入', N'贷', 3, 200670, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200674)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200674, N'500106', N'客服赔受款', N'营业收入', N'贷', 2, 200655, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200675)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200675, N'500107', N'考核激励', N'营业收入', N'贷', 2, 200655, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200676)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200676, N'50010701', N'KPI-网管', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200677)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200677, N'50010702', N'按需派送考核激励', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200678)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200678, N'50010703', N'操作规范考核-网管', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200679)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200679, N'50010704', N'操作规范投诉考核-客服', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200680)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200680, N'50010705', N'车辆考核-运营', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200681)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200681, N'50010706', N'电话考核-客服', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200682)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200682, N'50010707', N'扶持基金-网管', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200683)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200683, N'50010708', N'工单考核-客服', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200684)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200684, N'50010709', N'末端类-网管', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200685)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200685, N'50010710', N'省区综合考核-客服', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200686)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200686, N'50010711', N'省区综合考核-网管', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200687)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200687, N'50010712', N'时效件投诉考核-客服', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200688)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200688, N'50010713', N'时效考核-网管', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200689)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200689, N'50010714', N'时效考核-运营', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200690)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200690, N'50010715', N'小件员权益-网管', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200691)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200691, N'50010716', N'虚假问题件考核-客服', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200692)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200692, N'50010717', N'质量考核-运营', N'营业收入', N'贷', 3, 200675, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200693)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200693, N'500108', N'服务费', N'营业收入', N'贷', 2, 200655, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200694)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200694, N'50010801', N'按需服务', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200695)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200695, N'50010802', N'操作费-运营', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200696)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200696, N'50010803', N'航空提货互补', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200697)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200697, N'50010804', N'后勤', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200698)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200698, N'50010805', N'环保袋', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200699)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200699, N'50010806', N'进港包牌', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200700)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200700, N'50010807', N'客服服务费', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200701)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200701, N'50010808', N'网点赋能-网建', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200702)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200702, N'50010809', N'网点收费-网建', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200703)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200703, N'50010810', N'网点线路跑车', N'营业收入', N'贷', 3, 200693, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200704)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200704, N'500109', N'承包网点服务费收入', N'营业收入', N'贷', 2, 200655, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200705)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200705, N'500110', N'承接其他业务服务费收入', N'营业收入', N'贷', 2, 200655, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200706)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200706, N'500111', N'到付收入', N'营业收入', N'贷', 2, 200655, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200707)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200707, N'500199', N'其他收入', N'营业收入', N'贷', 2, 200655, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200708)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200708, N'5051', N'其他业务收入', N'营业收入', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200709)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200709, N'505101', N'食堂收入', N'营业收入', N'贷', 2, 200708, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200710)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200710, N'505102', N'快件理赔受款', N'营业收入', N'贷', 2, 200708, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200711)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200711, N'505103', N'其他收入', N'营业收入', N'贷', 2, 200708, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200712)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200712, N'5101', N'公允价值变动损益', N'其他收益', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200713)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200713, N'5111', N'投资损益', N'其他收益', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200714)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200714, N'5121', N'资产处置损益', N'其他收益', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200715)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200715, N'5151', N'其他收益', N'其他收益', N'贷', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200716)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200716, N'5301', N'营业外收入', N'其他收益', N'贷', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200717)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200717, N'530101', N'盘盈收益', N'其他收益', N'贷', 2, 200716, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200718)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200718, N'530102', N'非流动资产处置净收益', N'其他收益', N'贷', 2, 200716, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200719)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200719, N'530103', N'无法支付的应付账款', N'其他收益', N'贷', 2, 200716, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200720)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200720, N'530107', N'政府补助', N'其他收益', N'贷', 2, 200716, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200721)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200721, N'530108', N'违约金收益', N'其他收益', N'贷', 2, 200716, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200722)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200722, N'530109', N'捐赠收益', N'其他收益', N'贷', 2, 200716, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200723)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200723, N'530199', N'其他', N'其他收益', N'贷', 2, 200716, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200724)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200724, N'5401', N'主营业务成本', N'营业成本', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200725)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200725, N'540101', N'面单', N'营业成本', N'借', 2, 200724, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200726)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200726, N'54010101', N'面单费', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200727)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200727, N'54010102', N'政策返利', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200728)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200728, N'54010103', N'政策考核', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200729)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200729, N'54010104', N'超商活动返利', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200730)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200730, N'54010105', N'一口价', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200731)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200731, N'54010106', N'出港补贴派费', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200732)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200732, N'54010107', N'出港大货派费', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200733)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200733, N'54010108', N'出港基础派费', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200734)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200734, N'54010109', N'出港周期派费', N'营业成本', N'借', 3, 200725, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200735)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200735, N'540102', N'辅料', N'营业成本', N'借', 2, 200724, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200736)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200736, N'540103', N'中转', N'营业成本', N'借', 2, 200724, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200737)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200737, N'54010301', N'中转费', N'营业成本', N'借', 3, 200736, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200738)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200738, N'54010302', N'中转费加收', N'营业成本', N'借', 3, 200736, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200739)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200739, N'54010303', N'中转费考核', N'营业成本', N'借', 3, 200736, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200740)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200740, N'54010304', N'全网出港费', N'营业成本', N'借', 3, 200736, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200741)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200741, N'54010305', N'操作费', N'营业成本', N'借', 3, 200736, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200742)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200742, N'540104', N'派费', N'营业成本', N'借', 2, 200724, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200743)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200743, N'54010401', N'非专营网点考核', N'营业成本', N'借', 3, 200742, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200744)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200744, N'54010402', N'小件员权益', N'营业成本', N'借', 3, 200742, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200745)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200745, N'540105', N'服务费', N'营业成本', N'借', 2, 200724, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200746)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200746, N'54010501', N'按需服务', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200747)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200747, N'54010502', N'操作费-运营', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200748)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200748, N'54010503', N'航空提货互补', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200749)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200749, N'54010504', N'后勤', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200750)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200750, N'54010505', N'环保袋', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200751)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200751, N'54010506', N'进港包牌', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200752)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200752, N'54010507', N'经营支持服务费', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200753)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200753, N'54010508', N'客服服务费', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200754)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200754, N'54010509', N'客服托管服务费', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200755)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200755, N'54010510', N'客服系统服务费', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200756)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200756, N'54010511', N'汽运线路收费', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200757)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200757, N'54010512', N'通讯', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200758)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200758, N'54010513', N'网点线路跑车', N'营业成本', N'借', 3, 200745, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200759)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200759, N'540106', N'客服赔受款', N'营业成本', N'借', 2, 200724, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200760)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200760, N'540107', N'考核激励', N'营业成本', N'借', 2, 200724, 0, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200761)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200761, N'54010701', N'操作规范考核-网管', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200762)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200762, N'54010702', N'操作规范投诉考核-客服', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200763)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200763, N'54010703', N'车辆考核-运营', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200764)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200764, N'54010704', N'电话考核-客服', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200765)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200765, N'54010705', N'工单考核-客服', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200766)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200766, N'54010706', N'末端类-网管', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200767)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200767, N'54010707', N'签收率考核-客服', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200768)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200768, N'54010708', N'省区综合考核-客服', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200769)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200769, N'54010709', N'时效件投诉考核-客服', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200770)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200770, N'54010710', N'时效考核-网管', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200771)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200771, N'54010711', N'时效考核-运营', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200772)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200772, N'54010712', N'虚假问题件考核-客服', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200773)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200773, N'54010713', N'质量考核-运营', N'营业成本', N'借', 3, 200760, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200774)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200774, N'540108', N'运输成本', N'营业成本', N'借', 2, 200724, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200775)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200775, N'54010801', N'燃油费', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200776)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200776, N'54010802', N'过路费', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200777)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200777, N'54010803', N'维修费', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200778)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200778, N'54010804', N'违章罚款', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200779)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200779, N'54010805', N'保险费', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200780)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200780, N'54010806', N'检车费', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200781)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200781, N'54010807', N'进门/停车费', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200782)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200782, N'54010808', N'租车费用', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200783)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200783, N'54010809', N'新能源车电费', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200784)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200784, N'54010810', N'购置税', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200785)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200785, N'54010811', N'其他费用', N'营业成本', N'借', 3, 200774, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200786)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200786, N'540199', N'其他成本', N'营业成本', N'借', 2, 200724, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200787)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200787, N'5402', N'其他业务成本', N'营业成本', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200788)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200788, N'540201', N'食堂采购成本', N'营业成本', N'借', 2, 200787, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200789)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200789, N'540203', N'快件理赔赔款', N'营业成本', N'借', 2, 200787, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200790)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200790, N'5403', N'税金及附加', N'营业税金及附加', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200791)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200791, N'540301', N'消费税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200792)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200792, N'540302', N'城建税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200793)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200793, N'540303', N'教育费附加', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200794)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200794, N'540304', N'地方教育附加', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200795)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200795, N'540305', N'土地增值税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200796)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200796, N'540306', N'土地使用税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200797)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200797, N'540307', N'房产税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200798)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200798, N'540308', N'车船税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200799)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200799, N'540309', N'印花税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200800)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200800, N'540310', N'资源税', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200801)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200801, N'540311', N'矿产资源补偿费', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200802)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200802, N'540312', N'排污费', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200803)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200803, N'540399', N'其他', N'营业税金及附加', N'借', 2, 200790, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200804)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200804, N'5601', N'销售费用', N'期间费用', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200805)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200805, N'560101', N'薪酬', N'期间费用', N'借', 2, 200804, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200806)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200806, N'56010101', N'工资【正式工】', N'期间费用', N'借', 3, 200805, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200807)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200807, N'56010102', N'工资【临时工】', N'期间费用', N'借', 3, 200805, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200808)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200808, N'56010103', N'奖金', N'期间费用', N'借', 3, 200805, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200809)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200809, N'56010104', N'福利', N'期间费用', N'借', 3, 200805, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200810)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200810, N'5601010401', N'社会保险', N'期间费用', N'借', 4, 200809, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200811)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200811, N'5601010402', N'商业保险', N'期间费用', N'借', 4, 200809, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200812)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200812, N'5601010403', N'工伤补助', N'期间费用', N'借', 4, 200809, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200813)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200813, N'5601010499', N'其他福利', N'期间费用', N'借', 4, 200809, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200814)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200814, N'560102', N'折旧', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200815)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200815, N'560103', N'摊销', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200816)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200816, N'560104', N'租金', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200817)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200817, N'560105', N'水电费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200818)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200818, N'560106', N'物管费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200819)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200819, N'560107', N'维修费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200820)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200820, N'560108', N'办公费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200821)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200821, N'560109', N'通讯费', N'期间费用', N'借', 2, 200804, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200822)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200822, N'56010901', N'电话费', N'期间费用', N'借', 3, 200821, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200823)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200823, N'56010902', N'宽带费', N'期间费用', N'借', 3, 200821, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200824)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200824, N'560110', N'交通费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200825)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200825, N'560111', N'差旅费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200826)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200826, N'560112', N'会务费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200827)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200827, N'560113', N'物流费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200828)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200828, N'560114', N'招待费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200829)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200829, N'560115', N'宣传费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200830)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200830, N'560116', N'装修费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200831)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200831, N'560117', N'监控费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200832)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200832, N'560118', N'信息系统费用', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200833)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200833, N'560119', N'招聘费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200834)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200834, N'560120', N'培训费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200835)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200835, N'560121', N'外包服务费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200836)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200836, N'560122', N'事故赔偿费', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200837)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200837, N'560123', N'客户返款', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200838)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200838, N'560124', N'业务用设备', N'期间费用', N'借', 2, 200804, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200839)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200839, N'56012401', N'蓝牙秤', N'期间费用', N'借', 3, 200838, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200840)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200840, N'56012402', N'巴枪', N'期间费用', N'借', 3, 200838, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200841)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200841, N'56012403', N'热敏打印机', N'期间费用', N'借', 3, 200838, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200842)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200842, N'560125', N'驿站费用', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200843)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200843, N'560131', N'快递柜', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200844)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200844, N'560199', N'其他', N'期间费用', N'借', 2, 200804, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200845)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200845, N'5602', N'管理费用', N'期间费用', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200846)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200846, N'560201', N'薪酬', N'期间费用', N'借', 2, 200845, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200847)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200847, N'56020101', N'工资【正式工】', N'期间费用', N'借', 3, 200846, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200848)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200848, N'56020102', N'工资【临时工】', N'期间费用', N'借', 3, 200846, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200849)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200849, N'56020103', N'奖金', N'期间费用', N'借', 3, 200846, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200850)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200850, N'56020104', N'福利', N'期间费用', N'借', 3, 200846, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200851)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200851, N'5602010401', N'社会保险', N'期间费用', N'借', 4, 200850, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200852)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200852, N'5602010402', N'商业保险', N'期间费用', N'借', 4, 200850, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200853)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200853, N'5602010403', N'工伤补助', N'期间费用', N'借', 4, 200850, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200854)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200854, N'5602010499', N'其他福利', N'期间费用', N'借', 4, 200850, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200855)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200855, N'560202', N'折旧', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200856)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200856, N'560203', N'摊销', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200857)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200857, N'560204', N'租金', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200858)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200858, N'560205', N'水电费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200859)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200859, N'560206', N'物管费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200860)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200860, N'560207', N'维修费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200861)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200861, N'560208', N'办公费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200862)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200862, N'560209', N'通讯费', N'期间费用', N'借', 2, 200845, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200863)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200863, N'56020901', N'电话费', N'期间费用', N'借', 3, 200862, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200864)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200864, N'56020902', N'宽带费', N'期间费用', N'借', 3, 200862, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200865)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200865, N'560210', N'交通费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200866)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200866, N'560211', N'差旅费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200867)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200867, N'560212', N'会务费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200868)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200868, N'560213', N'装修费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200869)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200869, N'560214', N'招待费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200870)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200870, N'560215', N'宣传费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200871)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200871, N'560216', N'咨询费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200872)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200872, N'560217', N'信息系统费用', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200873)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200873, N'560218', N'监控费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200874)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200874, N'560219', N'招聘费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200875)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200875, N'560220', N'培训费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200876)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200876, N'560221', N'开办费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200877)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200877, N'560222', N'研发费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200878)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200878, N'560223', N'外包服务费', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200879)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200879, N'560299', N'其他', N'期间费用', N'借', 2, 200845, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200880)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200880, N'5603', N'财务费用', N'期间费用', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200881)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200881, N'560301', N'利息', N'期间费用', N'借', 2, 200880, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200882)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200882, N'560302', N'手续费', N'期间费用', N'借', 2, 200880, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200883)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200883, N'560303', N'汇兑损益', N'期间费用', N'借', 2, 200880, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200884)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200884, N'560399', N'其他', N'期间费用', N'借', 2, 200880, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200885)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200885, N'5701', N'资产减值损失', N'其他损失', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200886)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200886, N'5711', N'营业外支出', N'其他损失', N'借', 1, 0, 0, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200887)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200887, N'571101', N'盘亏毁损', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200888)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200888, N'571102', N'非流动资产处置净损失', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200889)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200889, N'571103', N'坏账损失', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200890)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200890, N'571104', N'长期债券损失', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200891)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200891, N'571105', N'长期股权损失', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200892)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200892, N'571106', N'不可抗力损失', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200893)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200893, N'571107', N'税收滞纳金', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200894)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200894, N'571108', N'违约金损失', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200895)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200895, N'571109', N'捐赠支出', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200896)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200896, N'571110', N'诉讼赔偿款', N'其他损失', N'借', 2, 200886, 1, N'部门', NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200897)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200897, N'571199', N'其他', N'其他损失', N'借', 2, 200886, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200898)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200898, N'5801', N'所得税费用', N'所得税费用', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200899)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200899, N'5901', N'以前年度损益调整', N'以前年度损益调整', N'借', 1, 0, 1, NULL, NULL, NULL, 1, 1, 0, N'2026-05-18 20:08:32.748', N'2026-05-18 20:08:32.748', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200900)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200900, N'1001', N'库存现金', N'流动资产', N'借', 1, 0, 1, N'outlet', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-19 00:44:34.550', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200901)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200901, N'1002', N'银行存款', N'流动资产', N'借', 1, 0, 0, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200902)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200902, N'100201', N'中国银行太仓支行', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200903)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200903, N'100209', N'支付宝-对公', N'流动资产', N'借', 2, 200901, 0, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200904)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200904, N'10020901', N'支付宝@163', N'流动资产', N'借', 3, 200903, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200905)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200905, N'10020902', N'支付宝8960', N'流动资产', N'借', 3, 200903, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200906)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200906, N'10020903', N'支付宝-建鑫', N'流动资产', N'借', 3, 200903, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200907)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200907, N'10020904', N'支付宝-美鑫', N'流动资产', N'借', 3, 200903, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200908)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200908, N'100203', N'工行-敦新建', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200909)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200909, N'100204', N'浏河建鑫', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200910)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200910, N'100205', N'沙溪美鑫', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200911)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200911, N'100206', N'韵科-中行', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200912)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200912, N'100207', N'韵科-建行', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200913)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200913, N'100208', N'韵达-瑞予宏', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200914)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200914, N'100210', N'极兔傲速', N'流动资产', N'借', 2, 200901, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200915)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200915, N'1012', N'其他货币资金', N'流动资产', N'借', 1, 0, 0, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200916)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200916, N'101201', N'申通城区', N'流动资产', N'借', 2, 200915, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200917)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200917, N'101202', N'申通浏河', N'流动资产', N'借', 2, 200915, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200918)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200918, N'101203', N'申通南郊（科教新城）', N'流动资产', N'借', 2, 200915, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200919)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200919, N'101204', N'申通沙溪', N'流动资产', N'借', 2, 200915, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200920)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200920, N'101205', N'申通市场部', N'流动资产', N'借', 2, 200915, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200921)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200921, N'1101', N'短期投资', N'流动资产', N'借', 1, 0, 0, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200922)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200922, N'110101', N'股票', N'流动资产', N'借', 2, 200921, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200923)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200923, N'110102', N'债券', N'流动资产', N'借', 2, 200921, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200924)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200924, N'110103', N'基金', N'流动资产', N'借', 2, 200921, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200925)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200925, N'1121', N'应收票据', N'流动资产', N'借', 1, 0, 1, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN科目] WHERE [FID] = 200926)
        
        INSERT INTO [FIN科目] ([FID], [F编码], [F名称], [F类别], [F余额方向], [F级次], [F父ID], [F是否末级], [F辅助核算], [F外币], [F计算单位], [F启用状态], [F账套ID], [F组织ID], [F创建时间], [F更新时间], [F启用年度], [F启用期间]) VALUES (200926, N'1122', N'应收账款', N'流动资产', N'借', 1, 0, 0, N'', N'', N'', 1, 2, 0, N'2026-05-18 20:18:39.973', N'2026-05-18 20:18:39.973', 0, 0);
        
        SET IDENTITY_INSERT [FIN科目] OFF;
        
        ");

        // FIN阿米巴损益模板
        ExecSql(ctx, @"
        SET IDENTITY_INSERT [FIN阿米巴损益模板] ON;
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益模板] WHERE [FID] = 3)
        
        INSERT INTO [FIN阿米巴损益模板] ([FID], [F名称], [F描述], [F是否默认], [F创建时间], [F更新时间], [F账套ID]) VALUES (3, N'太仓经营损益模板', NULL, 1, N'2026-05-12 00:44:25.870', N'2026-05-24 15:53:24.332', 2);
        
        SET IDENTITY_INSERT [FIN阿米巴损益模板] OFF;
        
        ");

        // FIN阿米巴损益项
        ExecSql(ctx, @"
        SET IDENTITY_INSERT [FIN阿米巴损益项] ON;
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 414)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (414, 3, N'出港', NULL, 1, 0, NULL, N'2026-05-24 18:49:54.920', N'2026-05-24 18:49:54.920', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 415)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (415, 3, N'出港指标', NULL, 20, 414, NULL, N'2026-05-24 18:49:54.923', N'2026-05-25 00:00:34.459', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 416)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (416, 3, N'发件票量', NULL, 20, 415, NULL, N'2026-05-24 18:49:54.926', N'2026-05-25 00:14:31.796', NULL, NULL, N'票', N'none', NULL, 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 417)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (417, 3, N'发件重量', NULL, 30, 415, NULL, N'2026-05-24 18:49:54.926', N'2026-05-25 00:14:31.826', NULL, NULL, N'kg', N'none', NULL, 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 418)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (418, 3, N'均重', NULL, 40, 415, NULL, N'2026-05-24 18:49:54.926', N'2026-05-25 00:14:31.860', N'formula', NULL, N'kg/票', N'none', NULL, 0, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 419)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (419, 3, N'揽件员人数', NULL, 50, 415, NULL, N'2026-05-24 18:49:54.926', N'2026-05-25 00:14:31.901', NULL, NULL, N'人', N'none', NULL, 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 420)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (420, 3, N'揽件效能', NULL, 60, 415, NULL, N'2026-05-24 18:49:54.926', N'2026-05-25 00:14:31.932', N'formula', NULL, N'件/人/日', N'none', NULL, 0, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 421)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (421, 3, N'出港客服人数', NULL, 70, 415, NULL, N'2026-05-24 18:49:54.926', N'2026-05-25 00:14:31.962', NULL, NULL, N'人', N'none', NULL, 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 422)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (422, 3, N'出港收入', NULL, 30, 414, NULL, N'2026-05-24 18:49:54.930', N'2026-05-25 00:00:34.519', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 423)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (423, 3, N'出港发件收入', NULL, 10, 422, NULL, N'2026-05-24 18:49:54.930', N'2026-05-24 23:49:26.348', N'voucher', NULL, N'元', N'auto', NULL, 0, N'寄件业务收入', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 425)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (425, 3, N'其他出港收入', NULL, 20, 422, NULL, N'2026-05-24 18:49:54.930', N'2026-05-24 23:49:26.403', N'voucher', NULL, N'元', N'auto', NULL, 0, N'抖音、个人寄件、散件、裹裹件收入等', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 426)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (426, 3, N'出港成本', NULL, 40, 414, NULL, N'2026-05-24 18:49:54.933', N'2026-05-25 00:00:34.558', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 427)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (427, 3, N'申通系统', NULL, 1, 426, NULL, N'2026-05-24 18:49:54.933', N'2026-05-24 18:49:54.933', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 428)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (428, 3, N'面单&派费成本', NULL, 1, 427, N'[{""code"": ""54010101""}, {""code"": ""54010106""}, {""code"": ""54010107""}, {""code"": ""54010108""}, {""code"": ""54010109""}]', N'2026-05-24 18:49:54.936', N'2026-05-24 18:49:54.936', N'voucher', NULL, N'元', N'auto', NULL, 0, N'1.61每单预付+派费调整+大货派费+乡镇补贴', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 429)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (429, 3, N'中转费', NULL, 2, 427, N'[{""code"": ""54010301""}, {""code"": ""54010302""}]', N'2026-05-24 18:49:54.936', N'2026-05-24 18:49:54.936', N'voucher', NULL, N'元', N'auto', NULL, 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 430)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (430, 3, N'操作费', NULL, 3, 427, N'[{""code"": ""54010305""}]', N'2026-05-24 18:49:54.936', N'2026-05-24 18:49:54.936', N'voucher', NULL, N'元', N'auto', NULL, 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 431)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (431, 3, N'全网出港费', NULL, 4, 427, N'[{""code"": ""54010304""}]', N'2026-05-24 18:49:54.936', N'2026-05-24 18:49:54.936', N'voucher', NULL, N'元', N'auto', NULL, 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 432)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (432, 3, N'政策返还', NULL, 5, 427, NULL, N'2026-05-24 18:49:54.940', N'2026-05-24 18:49:54.940', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 433)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (433, 3, N'返利', NULL, 1, 432, N'[{""code"": ""54010102""}, {""code"": ""54010104""}]', N'2026-05-24 18:49:54.943', N'2026-05-24 18:49:54.943', N'voucher', NULL, N'元', N'auto', NULL, 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 434)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (434, 3, N'业务量罚款', NULL, 2, 432, N'[{""code"": ""54010103""}, {""code"": ""54010401""}]', N'2026-05-24 18:49:54.943', N'2026-05-24 18:49:54.943', N'voucher', NULL, N'元', N'auto', NULL, 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 435)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (435, 3, N'一口价结算', NULL, 6, 427, N'[{""code"": ""54010105""}]', N'2026-05-24 18:49:54.946', N'2026-05-24 18:49:54.946', N'voucher', NULL, N'元', N'auto', NULL, 0, N'一口价运费+一口价考核', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 437)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (437, 3, N'出港质量罚款', NULL, 2, 426, NULL, N'2026-05-24 18:49:54.946', N'2026-05-24 18:49:54.946', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 438)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (438, 3, N'出港质量类罚款', NULL, 1, 437, N'[{""code"": ""54010713""}, {""code"": ""54010711""}, {""code"": ""54010703""}, {""code"": ""54010702""}, {""code"": ""54010704""}, {""code"": ""54010705""}, {""code"": ""54010706""}, {""code"": ""54010707""}, {""code"": ""54010708""}, {""code"": ""54010709""}, {""code"": ""54010710""}, {""code"": ""54010701""}]', N'2026-05-24 18:49:54.950', N'2026-05-24 18:49:54.950', N'voucher', NULL, N'元', N'auto', NULL, 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 439)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (439, 3, N'三件私了', NULL, 2, 437, N'[{""code"": ""540106""}]', N'2026-05-24 18:49:54.950', N'2026-05-24 18:49:54.950', N'voucher', NULL, N'元', N'auto', NULL, 0, N'向客户理赔产生的支出', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 440)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (440, 3, N'理赔追回', NULL, 3, 437, N'[{""code"": ""540106""}]', N'2026-05-24 18:49:54.950', N'2026-05-24 18:49:54.950', N'voucher', NULL, N'元', N'auto', NULL, 0, N'客服人员向责任中心或网点追回的理赔收入', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 441)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (441, 3, N'增值业务成本', NULL, 3, 426, N'[{""code"": ""54010501""}, {""code"": ""54010507""}, {""code"": ""54010508""}]', N'2026-05-24 18:49:54.953', N'2026-05-24 18:49:54.953', N'voucher', NULL, N'元', N'auto', NULL, 0, N'申鲜尊享项目收费', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 442)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (442, 3, N'揽件员工资', NULL, 4, 426, NULL, N'2026-05-24 18:49:54.953', N'2026-05-24 18:49:54.953', NULL, NULL, N'元', N'manual', NULL, 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 443)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (443, 3, N'集包费', NULL, 5, 426, N'[{""code"": ""54010502""}]', N'2026-05-24 18:49:54.953', N'2026-05-24 18:49:54.953', N'voucher', NULL, N'元', N'auto', NULL, 0, N'网点付集包工厂费用', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 444)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (444, 3, N'出港操作装卸', NULL, 6, 426, NULL, N'2026-05-24 18:49:54.953', N'2026-05-24 18:49:54.953', NULL, NULL, N'元', N'manual', NULL, 1, N'网点操作工及中心交货卸车工成本', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 445)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (445, 3, N'出港运输成本', NULL, 7, 426, NULL, N'2026-05-24 18:49:54.953', N'2026-05-24 18:49:54.953', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 446)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (446, 3, N'出港司机工资', NULL, 1, 445, NULL, N'2026-05-24 18:49:54.956', N'2026-05-24 18:49:54.956', NULL, NULL, N'元', N'manual', NULL, 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 447)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (447, 3, N'出港油费过路费', NULL, 2, 445, NULL, N'2026-05-24 18:49:54.956', N'2026-05-24 18:49:54.956', NULL, NULL, N'元', N'manual', NULL, 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 448)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (448, 3, N'出港维保其他', NULL, 3, 445, NULL, N'2026-05-24 18:49:54.956', N'2026-05-24 18:49:54.956', NULL, NULL, N'元', N'manual', NULL, 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 449)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (449, 3, N'出港车辆折旧', NULL, 4, 445, NULL, N'2026-05-24 18:49:54.956', N'2026-05-24 18:49:54.956', NULL, NULL, N'元', N'manual', NULL, 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 450)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (450, 3, N'出港物料成本', NULL, 8, 426, N'[{""code"": ""540102""}]', N'2026-05-24 18:49:54.960', N'2026-05-24 18:49:54.960', N'voucher', NULL, N'元', N'auto', NULL, 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 451)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (451, 3, N'其他出港成本', NULL, 9, 426, NULL, N'2026-05-24 18:49:54.960', N'2026-05-24 18:49:54.960', NULL, NULL, N'元', N'manual', NULL, 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 452)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (452, 3, N'出港边际利润', N'${出港收入} - ${出港成本}', 50, 414, NULL, N'2026-05-24 18:49:54.960', N'2026-05-25 00:00:34.592', NULL, NULL, N'元', NULL, NULL, 0, NULL, NULL, N'formula', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 453)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (453, 3, N'进港', NULL, 2, 0, NULL, N'2026-05-24 18:49:54.963', N'2026-05-24 18:49:54.963', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 454)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (454, 3, N'进港指标', NULL, 1, 453, NULL, N'2026-05-24 18:49:54.963', N'2026-05-24 18:49:54.963', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 455)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (455, 3, N'派件票量', NULL, 1, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'票', N'none', N'网点管家-有偿流量流向报表', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 456)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (456, 3, N'派件重量', NULL, 2, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'KG', N'none', N'网点管家-有偿流量流向报表', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 457)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (457, 3, N'入库率', NULL, 3, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'%', N'none', N'网点自行统计', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 458)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (458, 3, N'驿站单票成本', NULL, 4, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', N'formula', NULL, N'票/元', N'none', NULL, 0, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 459)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (459, 3, N'派件员人数', NULL, 5, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'人', N'none', N'网点自行统计', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 460)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (460, 3, N'派件效能', NULL, 6, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', N'formula', NULL, N'件/人/日', N'none', NULL, 0, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 461)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (461, 3, N'进港客服人数', NULL, 7, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'人', N'none', N'网点自行统计', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 462)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (462, 3, N'操作&装卸人数', NULL, 8, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'人', N'none', N'网点自行统计', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 463)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (463, 3, N'与中心距离', NULL, 9, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'KM', N'none', N'网点自行统计', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 464)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (464, 3, N'车型', NULL, 10, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'米/方', N'none', N'网点自行统计', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 465)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (465, 3, N'场地面积', NULL, 11, 454, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, N'平米', N'none', N'网点自行统计', 1, NULL, NULL, N'indicator', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 466)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (466, 3, N'进港收入', NULL, 2, 453, NULL, N'2026-05-24 18:49:54.970', N'2026-05-24 18:49:54.970', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 467)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (467, 3, N'派费收入', NULL, 1, 466, N'[{""code"": ""50010301""}, {""code"": ""50010302""}, {""code"": ""50010303""}, {""code"": ""50010304""}, {""code"": ""50010305""}, {""code"": ""50010306""}, {""code"": ""50010307""}]', N'2026-05-24 18:49:54.973', N'2026-05-24 18:49:54.973', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, N'基础派费+补贴派费+特价业务派费+周期性派费+大货计重派费+乡镇补贴+错签+补签', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 468)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (468, 3, N'政策互惠', NULL, 2, 466, N'[{""code"": ""50010401""}, {""code"": ""50010402""}]', N'2026-05-24 18:49:54.973', N'2026-05-24 18:49:54.973', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 469)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (469, 3, N'共配派费', NULL, 3, 466, NULL, N'2026-05-24 18:49:54.973', N'2026-05-24 18:49:54.973', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 470)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (470, 3, N'考核派费', NULL, 4, 466, N'[{""code"": ""50010701""}, {""code"": ""50010702""}]', N'2026-05-24 18:49:54.973', N'2026-05-24 18:49:54.973', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, N'考核派费+综合KPI，填列所属期考核派费，次月账单中查询', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 471)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (471, 3, N'星计划收费', NULL, 5, 466, NULL, N'2026-05-24 18:49:54.973', N'2026-05-24 18:49:54.973', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 472)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (472, 3, N'操作收入', NULL, 6, 466, N'[{""code"": ""50010501""}]', N'2026-05-24 18:49:54.973', N'2026-05-24 18:49:54.973', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, N'退件操作费+时效件投诉+同行封装投诉+偷逃大货举报奖励+异形件投诉+子母件等', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 473)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (473, 3, N'增值业务收入', NULL, 7, 466, N'[{""code"": ""50010801""}, {""code"": ""50010807""}]', N'2026-05-24 18:49:54.973', N'2026-05-24 18:49:54.973', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, N'到收代付手续费+保价业务+申鲜尊享+按需派送补贴+按需派送奖励+申咚咚激励金+派件手续费', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 474)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (474, 3, N'进港成本', NULL, 3, 453, NULL, N'2026-05-24 18:49:54.976', N'2026-05-24 18:49:54.976', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 475)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (475, 3, N'进港操作费', NULL, 1, 474, N'[{""code"": ""50010802""}]', N'2026-05-24 18:49:54.980', N'2026-05-24 18:49:54.980', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, N'进港操作费+0.18元/包的操作费', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 476)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (476, 3, N'环保袋费用', NULL, 2, 474, N'[{""code"": ""50010805""}]', N'2026-05-24 18:49:54.980', N'2026-05-24 18:49:54.980', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, N'环保袋使用、超时、遗失费用', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 477)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (477, 3, N'承包区派费', NULL, 3, 474, NULL, N'2026-05-24 18:49:54.980', N'2026-05-24 18:49:54.980', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 478)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (478, 3, N'派件员工资', NULL, 4, 474, NULL, N'2026-05-24 18:49:54.980', N'2026-05-24 18:49:54.980', NULL, NULL, N'元', N'manual', N'网点自行统计+网点管家-调账', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 479)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (479, 3, N'驿站成本', NULL, 5, 474, NULL, N'2026-05-24 18:49:54.980', N'2026-05-24 18:49:54.980', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 480)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (480, 3, N'支付驿站费用', NULL, 1, 479, NULL, N'2026-05-24 18:49:54.983', N'2026-05-24 18:49:54.983', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, N'直接支付给各种驿站、丰巢、代收点的费用', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 481)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (481, 3, N'运输成本（网点-驿站）', NULL, 2, 479, NULL, N'2026-05-24 18:49:54.983', N'2026-05-24 18:49:54.983', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, N'从网点送货到各种驿站、丰巢、代售点的运输费用', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 482)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (482, 3, N'进港操作装卸', NULL, 6, 474, NULL, N'2026-05-24 18:49:54.986', N'2026-05-24 18:49:54.986', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 483)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (483, 3, N'进港质量罚款', NULL, 7, 474, NULL, N'2026-05-24 18:49:54.986', N'2026-05-24 18:49:54.986', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 484)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (484, 3, N'进港质量类罚款', NULL, 1, 483, N'[{""code"": ""50010717""}, {""code"": ""50010714""}, {""code"": ""50010705""}, {""code"": ""50010704""}, {""code"": ""50010706""}, {""code"": ""50010708""}, {""code"": ""50010703""}, {""code"": ""50010713""}, {""code"": ""50010715""}, {""code"": ""50010710""}, {""code"": ""50010709""}, {""code"": ""50010707""}, {""code"": ""50010711""}, {""code"": ""50010712""}, {""code"": ""50010716""}]', N'2026-05-24 18:49:54.990', N'2026-05-24 18:49:54.990', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 485)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (485, 3, N'客服三件理赔', NULL, 2, 483, N'[{""code"": ""500106""}]', N'2026-05-24 18:49:54.990', N'2026-05-24 18:49:54.990', N'voucher', NULL, N'元', N'auto', N'网点管家-网点账单', 0, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 486)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (486, 3, N'进港运输成本', NULL, 8, 474, NULL, N'2026-05-24 18:49:54.990', N'2026-05-24 18:49:54.990', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, N'group', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 487)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (487, 3, N'进港司机工资', NULL, 1, 486, NULL, N'2026-05-24 18:49:54.993', N'2026-05-24 18:49:54.993', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 488)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (488, 3, N'进港油费过路费', NULL, 2, 486, NULL, N'2026-05-24 18:49:54.993', N'2026-05-24 18:49:54.993', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 489)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (489, 3, N'进港维保其他', NULL, 3, 486, NULL, N'2026-05-24 18:49:54.993', N'2026-05-24 18:49:54.993', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 490)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (490, 3, N'进港车辆折旧', NULL, 4, 486, NULL, N'2026-05-24 18:49:54.993', N'2026-05-24 18:49:54.993', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 491)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (491, 3, N'进港物料成本', NULL, 9, 474, NULL, N'2026-05-24 18:49:54.996', N'2026-05-24 18:49:54.996', NULL, NULL, N'元', N'manual', N'网点自行统计', 1, NULL, NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 492)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (492, 3, N'其他派件成本', NULL, 10, 474, NULL, N'2026-05-24 18:49:54.996', N'2026-05-24 18:49:54.996', NULL, NULL, N'元', N'manual', N'网点自行统计+网点管家-调账', 1, N'代派、三件私了等', NULL, N'data', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 493)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (493, 3, N'进港边际毛利', N'${进港收入} - ${进港成本}', 4, 453, NULL, N'2026-05-24 18:49:55.000', N'2026-05-24 18:49:55.000', NULL, NULL, N'元', NULL, NULL, 0, NULL, NULL, N'formula', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 494)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (494, 3, N'经营净利润', N'${出港边际利润} + ${进港边际毛利}', 99, 0, NULL, N'2026-05-24 18:49:55.000', N'2026-05-24 18:49:55.000', NULL, NULL, N'元', NULL, NULL, 0, NULL, NULL, N'formula', NULL);
        
        IF NOT EXISTS (SELECT 1 FROM [FIN阿米巴损益项] WHERE [FID] = 495)
        
        INSERT INTO [FIN阿米巴损益项] ([FID], [F模板ID], [F项目名称], [F计算公式], [F排序], [F父ID], [F关联科目JSON], [F创建时间], [F更新时间], [F数据源], [F摘要关键词JSON], [F单位], [F单票均模式], [F数据来源说明], [F是否手工填报], [F计算逻辑], [F辅助核算过滤Json], [F节点角色], [F计费过滤Json]) VALUES (495, 3, N'出港计价件量', NULL, 10, 415, NULL, N'2026-05-24 19:51:30.659', N'2026-05-25 00:14:31.767', N'billing', NULL, N'票', NULL, NULL, 0, NULL, NULL, N'data', N'{""outlets"":[],""businessObjects"":[],""aggregation"":""waybill_count"",""scope"":""priced""}');
        
        SET IDENTITY_INSERT [FIN阿米巴损益项] OFF;
        
        ");

        // 唯一索引: UQ_FIN凭证_VoucherNo
        ExecSql(ctx, @"
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_FIN凭证_VoucherNo' AND object_id = OBJECT_ID('FIN凭证'))
        BEGIN
        CREATE UNIQUE INDEX [UQ_FIN凭证_VoucherNo] ON [FIN凭证] ([F凭证字], [F期间ID], [F账套ID], [F凭证号]);
        END
        ");

    }

    private static void MigrateV2(STOTOPDbContext ctx)
    {
        if (!SeederHelper.IsSqlServer(ctx)) return;

        // [FIN阿米巴损益模板]: 清除 FTabsJson（先删默认约束再删列）
        SeederHelper.DropColumnSafe(ctx, "FIN阿米巴损益模板", "FTabsJson");

        // [FIN阿米巴损益项]: 清除 F项目类型, F显示级别, F业务方向（先删默认约束再删列）
        SeederHelper.DropColumnSafe(ctx, "FIN阿米巴损益项", "F项目类型");
        SeederHelper.DropColumnSafe(ctx, "FIN阿米巴损益项", "F显示级别");
        SeederHelper.DropColumnSafe(ctx, "FIN阿米巴损益项", "F业务方向");
    }

    private static void MigrateV3(STOTOPDbContext ctx)
    {
        if (!SeederHelper.IsSqlServer(ctx)) return;

        // 数据迁移：基于 FNodeRole + FDataSource 推导新字段值

        // F值来源映射
        ExecSql(ctx, @"
        UPDATE [FIN阿米巴损益项] SET [F值来源] = CASE 
          WHEN [F数据源] IN ('voucher','billing','estimate','depreciation') THEN 'system'
          WHEN [F数据源] = 'formula' THEN 'formula'
          WHEN [F数据源] = 'manual' THEN 'manual'
          ELSE NULL
        END
        WHERE [F值来源] IS NULL;
        ");

        // F系统数据源映射
        ExecSql(ctx, @"
        UPDATE [FIN阿米巴损益项] SET [F系统数据源] = [F数据源]
        WHERE [F数据源] IN ('voucher','billing','estimate','depreciation')
          AND [F系统数据源] IS NULL;
        ");

        // F项目类别映射 Step 1: 直接映射的类型
        ExecSql(ctx, @"
        UPDATE [FIN阿米巴损益项] SET [F项目类别] = 'section'   WHERE [F节点角色] = 'group'     AND [F项目类别] IS NULL;
        UPDATE [FIN阿米巴损益项] SET [F项目类别] = 'indicator'  WHERE [F节点角色] = 'indicator' AND [F项目类别] IS NULL;
        UPDATE [FIN阿米巴损益项] SET [F项目类别] = 'profit'     WHERE [F节点角色] = 'formula'   AND [F项目类别] IS NULL;
        ");

        // F项目类别映射 Step 1.5: 标记 indicator section
        // 指标分区按设计为"全局唯一、根级"——仅根级（F父ID=0）group 可标记；
        // Tab 内嵌套的指标组（如出港指标/进港指标）是普通分组，不得标记
        ExecSql(ctx, @"
        UPDATE p SET p.[F是否指标分区] = 1
        FROM [FIN阿米巴损益项] p
        WHERE p.[F节点角色] = 'group'
          AND p.[F父ID] = 0
          AND EXISTS (SELECT 1 FROM [FIN阿米巴损益项] c WHERE c.[F父ID] = p.[FID] AND c.[F节点角色] = 'indicator')
          AND p.[F是否指标分区] = 0;
        ");

        // F项目类别映射 Step 2: data 类型需根据父节点名称判断 revenue/cost
        ExecSql(ctx, @"
        UPDATE t SET t.[F项目类别] = CASE
          WHEN p.[F项目名称] LIKE '%收入%' THEN 'revenue'
          ELSE 'cost'
        END
        FROM [FIN阿米巴损益项] t
        INNER JOIN [FIN阿米巴损益项] p ON t.[F父ID] = p.[FID]
        WHERE t.[F节点角色] = 'data' AND t.[F项目类别] IS NULL;
        ");
    }

    private static void MigrateV4(STOTOPDbContext ctx)
    {
        if (!SeederHelper.IsSqlServer(ctx)) return;

        // 修复 V3 Step 1.5 的过度标记：指标分区按设计为"全局唯一、根级"，
        // V3 把所有含 indicator 子项的 group（如 Tab 内嵌套的出港指标/进港指标）都标记成了
        // 指标分区，导致多期报表只分离其中一个、另一个残留主 Tab，且前端因"已存在指标分区"
        // 无法再创建真正的根级运营指标分区。非根级标记一律回归普通分组。
        ExecSql(ctx, @"
        UPDATE [FIN阿米巴损益项]
        SET [F是否指标分区] = 0
        WHERE [F是否指标分区] = 1 AND [F父ID] <> 0;
        ");
    }
}
