using Microsoft.EntityFrameworkCore;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Services;

public class ExpenseAccountMappingService : IExpenseAccountMappingService
{
    private readonly STOTOPDbContext _db;

    public ExpenseAccountMappingService(STOTOPDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExpenseAccountMappingDto>> GetListAsync(long? expenseTypeId, long? orgId)
    {
        var query = from mapping in _db.Set<OaExpenseAccountMapping>().AsNoTracking()
                    join expenseType in _db.Set<OaExpenseType>().AsNoTracking()
                        on mapping.FExpenseTypeId equals expenseType.FID
                    select new { mapping, expenseType };

        if (expenseTypeId.HasValue)
            query = query.Where(x => x.mapping.FExpenseTypeId == expenseTypeId.Value);

        if (orgId.HasValue)
            query = query.Where(x => x.mapping.FOrgId == orgId.Value);

        var list = await query
            .OrderBy(x => x.mapping.FExpenseTypeId)
            .ThenBy(x => x.mapping.FID)
            .ToListAsync();

        return list.Select(x => new ExpenseAccountMappingDto
        {
            Id = x.mapping.FID,
            ExpenseTypeId = x.mapping.FExpenseTypeId,
            ExpenseTypeName = x.expenseType.FTypeName,
            AccountId = x.mapping.FAccountId,
            AccountCode = x.mapping.FAccountCode,
            AccountName = x.mapping.FAccountName,
            OrgId = x.mapping.FOrgId,
            IsDefault = x.mapping.FIsDefault == 1
        }).ToList();
    }

    public async Task<ExpenseAccountMappingDto> CreateAsync(CreateExpenseAccountMappingRequest request)
    {
        // 检查费用类型是否存在
        var expenseType = await _db.Set<OaExpenseType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FID == request.ExpenseTypeId);
        if (expenseType == null)
            throw new InvalidOperationException("费用类型不存在");

        // 检查是否已存在相同科目映射
        var exists = await _db.Set<OaExpenseAccountMapping>()
            .AnyAsync(m => m.FExpenseTypeId == request.ExpenseTypeId
                        && m.FAccountId == request.AccountId
                        && m.FOrgId == request.OrgId);
        if (exists)
            throw new InvalidOperationException("该费用类型已映射到此科目");

        // 如果设为默认，清除其他默认标记
        if (request.IsDefault)
        {
            var existingDefaults = await _db.Set<OaExpenseAccountMapping>()
                .AsTracking()
                .Where(m => m.FExpenseTypeId == request.ExpenseTypeId && m.FIsDefault == 1)
                .ToListAsync();
            foreach (var item in existingDefaults)
            {
                item.FIsDefault = 0;
            }
        }

        var entity = new OaExpenseAccountMapping
        {
            FExpenseTypeId = request.ExpenseTypeId,
            FAccountId = request.AccountId,
            FAccountCode = request.AccountCode,
            FAccountName = request.AccountName,
            FOrgId = request.OrgId,
            FIsDefault = request.IsDefault ? 1 : 0
        };

        _db.Set<OaExpenseAccountMapping>().Add(entity);
        await _db.SaveChangesAsync();

        return new ExpenseAccountMappingDto
        {
            Id = entity.FID,
            ExpenseTypeId = entity.FExpenseTypeId,
            ExpenseTypeName = expenseType.FTypeName,
            AccountId = entity.FAccountId,
            AccountCode = entity.FAccountCode,
            AccountName = entity.FAccountName,
            OrgId = entity.FOrgId,
            IsDefault = entity.FIsDefault == 1
        };
    }

    public async Task<ExpenseAccountMappingDto?> UpdateAsync(long id, UpdateExpenseAccountMappingRequest request)
    {
        var entity = await _db.Set<OaExpenseAccountMapping>()
            .AsTracking()
            .FirstOrDefaultAsync(m => m.FID == id);
        if (entity == null)
            return null;

        // 检查是否已存在相同科目映射（排除自身）
        var exists = await _db.Set<OaExpenseAccountMapping>()
            .AnyAsync(m => m.FExpenseTypeId == entity.FExpenseTypeId
                        && m.FAccountId == request.AccountId
                        && m.FOrgId == entity.FOrgId
                        && m.FID != id);
        if (exists)
            throw new InvalidOperationException("该费用类型已映射到此科目");

        // 如果设为默认，清除其他默认标记
        if (request.IsDefault)
        {
            var existingDefaults = await _db.Set<OaExpenseAccountMapping>()
                .AsTracking()
                .Where(m => m.FExpenseTypeId == entity.FExpenseTypeId && m.FIsDefault == 1 && m.FID != id)
                .ToListAsync();
            foreach (var item in existingDefaults)
            {
                item.FIsDefault = 0;
            }
        }

        entity.FAccountId = request.AccountId;
        entity.FAccountCode = request.AccountCode;
        entity.FAccountName = request.AccountName;
        entity.FIsDefault = request.IsDefault ? 1 : 0;

        await _db.SaveChangesAsync();

        var expenseType = await _db.Set<OaExpenseType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FID == entity.FExpenseTypeId);

        return new ExpenseAccountMappingDto
        {
            Id = entity.FID,
            ExpenseTypeId = entity.FExpenseTypeId,
            ExpenseTypeName = expenseType?.FTypeName ?? "",
            AccountId = entity.FAccountId,
            AccountCode = entity.FAccountCode,
            AccountName = entity.FAccountName,
            OrgId = entity.FOrgId,
            IsDefault = entity.FIsDefault == 1
        };
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await _db.Set<OaExpenseAccountMapping>()
            .AsTracking()
            .FirstOrDefaultAsync(m => m.FID == id);
        if (entity == null)
            return false;

        _db.Set<OaExpenseAccountMapping>().Remove(entity);
        await _db.SaveChangesAsync();

        return true;
    }
}
