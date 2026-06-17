using System.Collections.Generic;
using STOTOP.Module.Finance.Entities;
using STOTOP.Module.Finance.Services;
using Xunit;

namespace STOTOP.Module.Finance.Tests.Amoeba;

/// <summary>
/// 批次5-S2 公共费分摊「接入」的纯函数辅助方法单测（设计 spec §4.1/§4.2/§4.4）。
/// 引擎本身的比例/除零/配对在 <see cref="CommonCostAllocationEngineTests"/>，此处只测接线侧取数口径。
/// </summary>
public class AmoebaCommonCostWiringTests
{
    private static FinAmoebaPLItem Item(long fid, string name, string role, string? alloc = null) =>
        new() { FID = fid, FItemName = name, FNodeRole = role, F分摊方式 = alloc };

    [Theory]
    [InlineData("volume", true)]
    [InlineData("VOLUME", true)]   // 大小写不敏感
    [InlineData("direct", false)]
    [InlineData(null, false)]
    public void IsCommonCostLeaf_true_only_for_volume(string? alloc, bool expected)
    {
        Assert.Equal(expected, AmoebaPLService.IsCommonCostLeaf(Item(1, "房租", "data", alloc)));
    }

    [Fact]
    public void ResolveVolumeIndicatorIds_matches_send_and_deliver_by_name()
    {
        var items = new List<FinAmoebaPLItem>
        {
            Item(10, "发件票量", "indicator"),
            Item(11, "派件票量", "indicator"),
            Item(12, "发件票量(数据)", "data"),   // 非 indicator 角色不计入
        };

        var (send, deliver) = AmoebaPLService.ResolveVolumeIndicatorIds(items);

        Assert.Equal(10, send);
        Assert.Equal(11, deliver);
    }

    [Fact]
    public void ResolveVolumeIndicatorIds_returns_null_when_absent()
    {
        var (send, deliver) = AmoebaPLService.ResolveVolumeIndicatorIds(
            new List<FinAmoebaPLItem> { Item(1, "出港收入", "data") });

        Assert.Null(send);
        Assert.Null(deliver);
    }

    [Fact]
    public void ComputeVolumeBasis_send_from_billing_waybill_count_only()
    {
        var points = new List<DataPoint>
        {
            new() { Source = "billing", WaybillCount = 300 },
            new() { Source = "billing", WaybillCount = 200 },
            new() { Source = "voucher", WaybillCount = 0, Amount = 9999m },   // 凭证不带件量
        };

        var vb = AmoebaPLService.ComputeVolumeBasis(points, new Dictionary<long, decimal>(), sendId: 10, deliverId: 11);

        Assert.Equal(500, vb.SendCount);
        Assert.Equal(0, vb.DeliverCount);
    }

    [Fact]
    public void ComputeVolumeBasis_send_falls_back_to_send_indicator_when_no_billing()
    {
        var points = new List<DataPoint> { new() { Source = "voucher", Amount = 100m } };
        var matched = new Dictionary<long, decimal> { [10] = 420m };   // 发件票量 indicator 匹配值

        var vb = AmoebaPLService.ComputeVolumeBasis(points, matched, sendId: 10, deliverId: 11);

        Assert.Equal(420, vb.SendCount);
    }

    [Fact]
    public void ComputeVolumeBasis_deliver_from_deliver_indicator_matched_value()
    {
        var points = new List<DataPoint> { new() { Source = "billing", WaybillCount = 500 } };
        var matched = new Dictionary<long, decimal> { [11] = 180m };   // 派件票量 indicator(进港无 billing 源)

        var vb = AmoebaPLService.ComputeVolumeBasis(points, matched, sendId: 10, deliverId: 11);

        Assert.Equal(500, vb.SendCount);    // billing 优先于 send indicator
        Assert.Equal(180, vb.DeliverCount);
    }

    [Fact]
    public void ComputeVolumeBasis_null_indicator_ids_yield_zero_without_throwing()
    {
        var points = new List<DataPoint> { new() { Source = "voucher", Amount = 100m } };

        var vb = AmoebaPLService.ComputeVolumeBasis(points, new Dictionary<long, decimal>(), sendId: null, deliverId: null);

        Assert.Equal(0, vb.SendCount);
        Assert.Equal(0, vb.DeliverCount);
    }
}
