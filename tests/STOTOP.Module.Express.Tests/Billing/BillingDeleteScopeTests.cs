using System.Collections.Generic;
using STOTOP.Module.Express.Services.Billing;
using Xunit;

namespace STOTOP.Module.Express.Tests.Billing;

/// <summary>
/// 计费结果删除范围回归测试。
/// 锁定：重跑去重的 DELETE 必须限定本批次本运单（Phase B 还需限定失败状态），
/// 否则会发生「同批两阶段互删」「跨批次误删」导致成功行丢失。
/// </summary>
public class BillingDeleteScopeTests
{
    private static readonly IReadOnlySet<string> WaybillSto123 =
        new HashSet<string> { "STO123" };

    // 同批互删：Phase A 写入的成功行(批次82, 状态1)，在 Phase B 删失败行时必须存活。
    [Fact]
    public void PhaseB_delete_must_not_remove_phaseA_success_row()
    {
        // 旧语义：Phase B 不限定状态 → 成功行被命中删除（数据丢失）
        Assert.True(BillingDeleteScope.WouldDeleteRow(
            rowBatchId: 82, rowWaybillNo: "STO123", rowCalcStatus: 1,
            deleteBatchId: 82, deleteWaybillNos: WaybillSto123, deleteCalcStatus: null));

        // 新语义：Phase B 限定状态=2 → 成功行(状态1)不被命中，存活
        Assert.False(BillingDeleteScope.WouldDeleteRow(
            rowBatchId: 82, rowWaybillNo: "STO123", rowCalcStatus: 1,
            deleteBatchId: 82, deleteWaybillNos: WaybillSto123, deleteCalcStatus: 2));
    }

    // 跨批次误删：批次81的历史成功行，不应被批次82的删除命中（修复后行为）。
    [Fact]
    public void Delete_must_not_cross_batch()
    {
        Assert.False(BillingDeleteScope.WouldDeleteRow(
            rowBatchId: 81, rowWaybillNo: "STO123", rowCalcStatus: 1,
            deleteBatchId: 82, deleteWaybillNos: WaybillSto123, deleteCalcStatus: null));
    }

    // 本批次本运单的失败行：Phase B 正常清理（重跑去重的预期行为）。
    [Fact]
    public void PhaseB_delete_removes_same_batch_failure_row()
    {
        Assert.True(BillingDeleteScope.WouldDeleteRow(
            rowBatchId: 82, rowWaybillNo: "STO123", rowCalcStatus: 2,
            deleteBatchId: 82, deleteWaybillNos: WaybillSto123, deleteCalcStatus: 2));
    }

    // 不在待删名单的运单：永不命中。
    [Fact]
    public void Delete_skips_waybills_not_in_set()
    {
        Assert.False(BillingDeleteScope.WouldDeleteRow(
            rowBatchId: 82, rowWaybillNo: "STO999", rowCalcStatus: 2,
            deleteBatchId: 82, deleteWaybillNos: WaybillSto123, deleteCalcStatus: 2));
    }
}
