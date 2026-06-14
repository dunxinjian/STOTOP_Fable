using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Interfaces;
using STOTOP.Core.Models;
using STOTOP.Module.Dormitory.Dtos;
using STOTOP.Module.Dormitory.Entities;
using STOTOP.Module.Dormitory.Services.Interfaces;

namespace STOTOP.Module.Dormitory.Services;

public class ExpenseService : IExpenseService
{
    private readonly IRepository<DorExpense> _expenseRepository;
    private readonly IRepository<DorRoom> _roomRepository;
    private readonly IRepository<DorBuilding> _buildingRepository;

    public ExpenseService(
        IRepository<DorExpense> expenseRepository,
        IRepository<DorRoom> roomRepository,
        IRepository<DorBuilding> buildingRepository)
    {
        _expenseRepository = expenseRepository;
        _roomRepository = roomRepository;
        _buildingRepository = buildingRepository;
    }

    public async Task<PagedResult<ExpenseListItemDto>> GetExpensesAsync(ExpenseQueryRequest request)
    {
        var query = from e in _expenseRepository.Query()
                    join r in _roomRepository.Query() on e.FRoomId equals r.FID
                    join b in _buildingRepository.Query() on r.FBuildingId equals b.FID
                    select new { Expense = e, Room = r, Building = b };

        if (request.BuildingId.HasValue)
        {
            query = query.Where(x => x.Room.FBuildingId == request.BuildingId.Value);
        }

        if (request.RoomId.HasValue)
        {
            query = query.Where(x => x.Expense.FRoomId == request.RoomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Month))
        {
            query = query.Where(x => x.Expense.FMonth == request.Month);
        }

        if (!string.IsNullOrWhiteSpace(request.ExpenseType))
        {
            query = query.Where(x => x.Expense.FExpenseType == request.ExpenseType);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Expense.FCreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<ExpenseListItemDto>
        {
            Items = items.Select(x => MapToListItemDto(x.Expense, x.Room, x.Building)).ToList(),
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }

    public async Task<ExpenseDto?> GetExpenseByIdAsync(long id)
    {
        var result = await (from e in _expenseRepository.Query()
                           join r in _roomRepository.Query() on e.FRoomId equals r.FID
                           join b in _buildingRepository.Query() on r.FBuildingId equals b.FID
                           where e.FID == id
                           select new { Expense = e, Room = r, Building = b })
                           .FirstOrDefaultAsync();

        return result == null ? null : MapToDto(result.Expense, result.Room, result.Building);
    }

    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request)
    {
        // 检查房间是否存在
        var room = await _roomRepository.Query()
            .Include(r => r.Building)
            .FirstOrDefaultAsync(r => r.FID == request.RoomId);

        if (room == null)
        {
            throw new InvalidOperationException("房间不存在");
        }

        var expense = new DorExpense
        {
            FRoomId = request.RoomId,
            FExpenseType = request.ExpenseType,
            FAmount = request.Amount,
            FMonth = request.Month,
            FShareMethod = request.ShareMethod,
            FRemark = request.Remark,
            FStatus = 1,
            FCreatedTime = DateTime.Now,
            FUpdatedTime = DateTime.Now
        };

        await _expenseRepository.AddAsync(expense);
        return (await GetExpenseByIdAsync(expense.FID))!;
    }

    public async Task<ExpenseDto?> UpdateExpenseAsync(long id, UpdateExpenseRequest request)
    {
        var expense = await _expenseRepository.Query()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (expense == null) return null;

        expense.FExpenseType = request.ExpenseType;
        expense.FAmount = request.Amount;
        expense.FMonth = request.Month;
        expense.FShareMethod = request.ShareMethod;
        expense.FRemark = request.Remark;
        expense.FUpdatedTime = DateTime.Now;

        await _expenseRepository.UpdateAsync(expense);
        return await GetExpenseByIdAsync(id);
    }

    public async Task<bool> DeleteExpenseAsync(long id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return false;

        await _expenseRepository.DeleteAsync(id);
        return true;
    }

    public async Task<List<MonthlyExpenseSummaryDto>> GetMonthlySummaryAsync(string? month)
    {
        var query = _expenseRepository.Query();

        if (!string.IsNullOrWhiteSpace(month))
        {
            query = query.Where(e => e.FMonth == month);
        }

        var summary = await query
            .GroupBy(e => new { e.FMonth, e.FExpenseType })
            .Select(g => new MonthlyExpenseSummaryDto
            {
                Month = g.Key.FMonth,
                ExpenseType = g.Key.FExpenseType,
                TotalAmount = g.Sum(e => e.FAmount),
                Count = g.Count()
            })
            .OrderByDescending(s => s.Month)
            .ThenBy(s => s.ExpenseType)
            .ToListAsync();

        return summary;
    }

    #region Mapping

    private static ExpenseDto MapToDto(DorExpense entity, DorRoom room, DorBuilding building)
    {
        return new ExpenseDto
        {
            Id = entity.FID,
            RoomId = entity.FRoomId,
            RoomNumber = room.FRoomNumber,
            BuildingName = building.FName,
            ExpenseType = entity.FExpenseType,
            Amount = entity.FAmount,
            Month = entity.FMonth,
            ShareMethod = entity.FShareMethod,
            Remark = entity.FRemark,
            Status = entity.FStatus,
            CreatorId = entity.FCreatorId,
            CreatedTime = entity.FCreatedTime,
            UpdatedTime = entity.FUpdatedTime
        };
    }

    private static ExpenseListItemDto MapToListItemDto(DorExpense entity, DorRoom room, DorBuilding building)
    {
        return new ExpenseListItemDto
        {
            Id = entity.FID,
            RoomId = entity.FRoomId,
            RoomNumber = room.FRoomNumber,
            BuildingName = building.FName,
            ExpenseType = entity.FExpenseType,
            Amount = entity.FAmount,
            Month = entity.FMonth,
            Status = entity.FStatus,
            CreatedTime = entity.FCreatedTime
        };
    }

    #endregion
}
