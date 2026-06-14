using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Interfaces;
using STOTOP.Module.Dormitory.Dtos;
using STOTOP.Module.Dormitory.Entities;
using STOTOP.Module.Dormitory.Services.Interfaces;

namespace STOTOP.Module.Dormitory.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IRepository<DorBed> _bedRepository;
    private readonly IRepository<DorRoom> _roomRepository;
    private readonly IRepository<DorBuilding> _buildingRepository;
    private readonly IRepository<DorResidence> _residenceRepository;
    private readonly IRepository<DorExpense> _expenseRepository;
    private readonly IRepository<DorRepairOrder> _repairOrderRepository;
    private readonly IRepository<DorVisitor> _visitorRepository;

    public StatisticsService(
        IRepository<DorBed> bedRepository,
        IRepository<DorRoom> roomRepository,
        IRepository<DorBuilding> buildingRepository,
        IRepository<DorResidence> residenceRepository,
        IRepository<DorExpense> expenseRepository,
        IRepository<DorRepairOrder> repairOrderRepository,
        IRepository<DorVisitor> visitorRepository)
    {
        _bedRepository = bedRepository;
        _roomRepository = roomRepository;
        _buildingRepository = buildingRepository;
        _residenceRepository = residenceRepository;
        _expenseRepository = expenseRepository;
        _repairOrderRepository = repairOrderRepository;
        _visitorRepository = visitorRepository;
    }

    public async Task<DormitoryStatisticsDto> GetStatisticsAsync()
    {
        // 获取总床位数
        var totalBeds = await _bedRepository.Query().CountAsync();

        // 获取已入住床位数（状态为入住中且未退宿的记录）
        var occupiedBeds = await _residenceRepository.Query()
            .CountAsync(r => r.FStatus == 1 && r.FCheckOutDate == null);

        var availableBeds = totalBeds - occupiedBeds;

        // 计算入住率和空置率
        var occupancyRate = totalBeds > 0 ? Math.Round((decimal)occupiedBeds / totalBeds * 100, 2) : 0;
        var vacancyRate = totalBeds > 0 ? Math.Round((decimal)availableBeds / totalBeds * 100, 2) : 0;

        // 各楼栋入住统计
        var buildingOccupancies = await GetBuildingOccupanciesAsync();

        // 本月费用汇总
        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        var monthlyExpenseSummaries = await GetMonthlyExpenseSummariesAsync(currentMonth);

        // 待处理报修工单数
        var pendingRepairOrders = await _repairOrderRepository.Query()
            .CountAsync(ro => ro.FStatus == 1);

        // 今日访客数
        var today = DateTime.Today;
        var todayVisitors = await _visitorRepository.Query()
            .CountAsync(v => v.FArrivalTime.Date == today);

        // 总楼栋数
        var totalBuildings = await _buildingRepository.Query()
            .CountAsync(b => b.FStatus == 1);

        // 总房间数
        var totalRooms = await _roomRepository.Query().CountAsync();

        // 待缴费用总额（状态为1表示未支付）
        var pendingExpenses = await _expenseRepository.Query()
            .Where(e => e.FStatus == 1)
            .SumAsync(e => e.FAmount);

        return new DormitoryStatisticsDto
        {
            TotalBeds = totalBeds,
            OccupiedBeds = occupiedBeds,
            AvailableBeds = availableBeds,
            OccupancyRate = occupancyRate,
            VacancyRate = vacancyRate,
            BuildingOccupancies = buildingOccupancies,
            MonthlyExpenseSummaries = monthlyExpenseSummaries,
            PendingRepairOrders = pendingRepairOrders,
            TodayVisitors = todayVisitors,
            TotalBuildings = totalBuildings,
            TotalRooms = totalRooms,
            PendingExpenses = pendingExpenses
        };
    }

    private async Task<List<BuildingOccupancyDto>> GetBuildingOccupanciesAsync()
    {
        var buildings = await _buildingRepository.Query()
            .Where(b => b.FStatus == 1)
            .ToListAsync();

        var result = new List<BuildingOccupancyDto>();

        foreach (var building in buildings)
        {
            // 获取该楼栋所有房间ID
            var roomIds = await _roomRepository.Query()
                .Where(r => r.FBuildingId == building.FID)
                .Select(r => r.FID)
                .ToListAsync();

            // 获取该楼栋所有床位ID
            var bedIds = await _bedRepository.Query()
                .Where(b => roomIds.Contains(b.FRoomId))
                .Select(b => b.FID)
                .ToListAsync();

            var totalBeds = bedIds.Count;

            // 获取已入住床位数
            var occupiedBeds = await _residenceRepository.Query()
                .CountAsync(r => bedIds.Contains(r.FBedId) && r.FStatus == 1 && r.FCheckOutDate == null);

            var availableBeds = totalBeds - occupiedBeds;
            var occupancyRate = totalBeds > 0 ? Math.Round((decimal)occupiedBeds / totalBeds * 100, 2) : 0;

            result.Add(new BuildingOccupancyDto
            {
                BuildingId = building.FID,
                BuildingName = building.FName,
                TotalBeds = totalBeds,
                OccupiedBeds = occupiedBeds,
                AvailableBeds = availableBeds,
                OccupancyRate = occupancyRate
            });
        }

        return result;
    }

    private async Task<List<MonthlyExpenseSummaryDto>> GetMonthlyExpenseSummariesAsync(string month)
    {
        var summary = await _expenseRepository.Query()
            .Where(e => e.FMonth == month)
            .GroupBy(e => e.FExpenseType)
            .Select(g => new MonthlyExpenseSummaryDto
            {
                Month = month,
                ExpenseType = g.Key,
                TotalAmount = g.Sum(e => e.FAmount),
                Count = g.Count()
            })
            .ToListAsync();

        return summary;
    }
}
