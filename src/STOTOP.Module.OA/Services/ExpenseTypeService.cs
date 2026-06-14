using Microsoft.EntityFrameworkCore;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Services;

public class ExpenseTypeService : IExpenseTypeService
{
    private readonly STOTOPDbContext _db;

    public ExpenseTypeService(STOTOPDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExpenseTypeDto>> GetListAsync(long? orgId, string? scene)
    {
        var query = _db.Set<OaExpenseType>().AsNoTracking().AsQueryable();
        if (orgId.HasValue)
            query = query.Where(e => e.FOrgId == orgId.Value);
        if (!string.IsNullOrEmpty(scene) && scene != "all")
            query = query.Where(e => e.FApplicableScene == scene || e.FApplicableScene == "both");
        var list = await query.OrderBy(e => e.FSortOrder).ThenBy(e => e.FID).ToListAsync();
        return list.Select(e => new ExpenseTypeDto
        {
            Id = e.FID,
            TypeCode = e.FTypeCode,
            TypeName = e.FTypeName,
            ApplicableScene = e.FApplicableScene,
            OrgId = e.FOrgId,
            SortOrder = e.FSortOrder,
            IsEnabled = e.FIsEnabled == 1,
            CreatedTime = e.FCreatedTime,
        }).ToList();
    }

    public async Task<ExpenseTypeDto> CreateAsync(CreateExpenseTypeRequest request)
    {
        // 检查编码是否重复
        var exists = await _db.Set<OaExpenseType>()
            .AnyAsync(e => e.FTypeCode == request.TypeCode && e.FOrgId == request.OrgId);
        if (exists)
            throw new InvalidOperationException("费用类型编码已存在");

        // 检查名称是否重复
        var nameExists = await _db.Set<OaExpenseType>()
            .AnyAsync(e => e.FTypeName == request.TypeName && e.FOrgId == request.OrgId);
        if (nameExists)
            throw new InvalidOperationException("费用类型名称已存在");

        var entity = new OaExpenseType
        {
            FTypeCode = request.TypeCode,
            FTypeName = request.TypeName,
            FApplicableScene = request.ApplicableScene,
            FOrgId = request.OrgId,
            FSortOrder = request.SortOrder,
            FIsEnabled = 1,
            FCreatedTime = DateTime.Now
        };

        _db.Set<OaExpenseType>().Add(entity);
        await _db.SaveChangesAsync();

        return new ExpenseTypeDto
        {
            Id = entity.FID,
            TypeCode = entity.FTypeCode,
            TypeName = entity.FTypeName,
            ApplicableScene = entity.FApplicableScene,
            OrgId = entity.FOrgId,
            SortOrder = entity.FSortOrder,
            IsEnabled = entity.FIsEnabled == 1,
            CreatedTime = entity.FCreatedTime
        };
    }

    public async Task<ExpenseTypeDto?> UpdateAsync(long id, UpdateExpenseTypeRequest request)
    {
        var entity = await _db.Set<OaExpenseType>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);
        if (entity == null)
            return null;

        // 检查编码是否与其他记录重复
        var codeExists = await _db.Set<OaExpenseType>()
            .AnyAsync(e => e.FTypeCode == request.TypeCode && e.FOrgId == entity.FOrgId && e.FID != id);
        if (codeExists)
            throw new InvalidOperationException("费用类型编码已存在");

        // 检查名称是否与其他记录重复
        var nameExists = await _db.Set<OaExpenseType>()
            .AnyAsync(e => e.FTypeName == request.TypeName && e.FOrgId == entity.FOrgId && e.FID != id);
        if (nameExists)
            throw new InvalidOperationException("费用类型名称已存在");

        entity.FTypeCode = request.TypeCode;
        entity.FTypeName = request.TypeName;
        entity.FApplicableScene = request.ApplicableScene;
        entity.FSortOrder = request.SortOrder;

        await _db.SaveChangesAsync();

        return new ExpenseTypeDto
        {
            Id = entity.FID,
            TypeCode = entity.FTypeCode,
            TypeName = entity.FTypeName,
            ApplicableScene = entity.FApplicableScene,
            OrgId = entity.FOrgId,
            SortOrder = entity.FSortOrder,
            IsEnabled = entity.FIsEnabled == 1,
            CreatedTime = entity.FCreatedTime
        };
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var entity = await _db.Set<OaExpenseType>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);
        if (entity == null)
            return false;

        // 检查是否被报销明细引用
        var reimbursementUsed = await _db.Set<OaExpenseReimbursementDetail>()
            .AnyAsync(d => d.FExpenseType == entity.FTypeCode);
        if (reimbursementUsed)
            throw new InvalidOperationException("该费用类型已被报销单引用，无法删除");

        // 检查是否被对外付款明细引用
        var paymentUsed = await _db.Set<OaExternalPaymentDetail>()
            .AnyAsync(d => d.FExpenseType == entity.FTypeCode);
        if (paymentUsed)
            throw new InvalidOperationException("该费用类型已被对外付款单引用，无法删除");

        // 检查是否被备用金报销明细引用
        var pettyCashUsed = await _db.Set<OaPettyCashReimbursementDetail>()
            .AnyAsync(d => d.FExpenseType == entity.FTypeCode);
        if (pettyCashUsed)
            throw new InvalidOperationException("该费用类型已被备用金报销单引用，无法删除");

        // 删除关联的科目映射
        var mappings = await _db.Set<OaExpenseAccountMapping>()
            .Where(m => m.FExpenseTypeId == id)
            .ToListAsync();
        _db.Set<OaExpenseAccountMapping>().RemoveRange(mappings);

        _db.Set<OaExpenseType>().Remove(entity);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleAsync(long id)
    {
        var entity = await _db.Set<OaExpenseType>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);
        if (entity == null)
            return false;

        entity.FIsEnabled = entity.FIsEnabled == 1 ? 0 : 1;
        await _db.SaveChangesAsync();

        return true;
    }
}
