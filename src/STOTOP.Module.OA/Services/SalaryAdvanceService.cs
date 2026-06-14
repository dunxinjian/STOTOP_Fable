using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Models;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Entities;

namespace STOTOP.Module.OA.Services;

public class SalaryAdvanceService : ISalaryAdvanceService
{
    private readonly STOTOPDbContext _db;

    public SalaryAdvanceService(STOTOPDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SalaryAdvanceDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var query = _db.Set<OaSalaryAdvance>().AsNoTracking().AsQueryable();

        // 只查询当前用户的申请单
        query = query.Where(e => e.FApplicantId == userId);

        if (status.HasValue)
            query = query.Where(e => e.FDocStatus == status.Value);

        if (orgId.HasValue)
            query = query.Where(e => e.FOrgId == orgId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.FCreatedTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 批量获取关联数据
        var orgIds = items.Select(e => e.FOrgId).Distinct().ToList();
        var deptIds = items.Select(e => e.FDeptId).Distinct().ToList();

        var orgs = await _db.Set<SysOrganization>()
            .AsNoTracking()
            .Where(o => orgIds.Contains(o.FID))
            .ToDictionaryAsync(o => o.FID, o => o.FName);

        var depts = await _db.Set<SysOrganization>()
            .AsNoTracking()
            .Where(d => deptIds.Contains(d.FID))
            .ToDictionaryAsync(d => d.FID, d => d.FName);

        var user = await _db.Set<SysUser>().AsNoTracking().FirstOrDefaultAsync(u => u.FID == userId);

        var result = items.Select(e => new SalaryAdvanceDto
        {
            Id = e.FID,
            DocNumber = e.FDocNumber,
            ApplicantId = e.FApplicantId,
            ApplicantName = user?.FName ?? "",
            DeptId = e.FDeptId,
            DeptName = depts.TryGetValue(e.FDeptId, out var deptName) ? deptName : "",
            OrgId = e.FOrgId,
            OrgName = orgs.TryGetValue(e.FOrgId, out var orgName) ? orgName : "",
            AdvanceAmount = e.FAdvanceAmount,
            AdvanceMonth = e.FAdvanceMonth,
            ApplyReason = e.FApplyReason,
            PaymentMethod = e.FPaymentMethod,
            PayeeName = e.FPayeeName,
            PayeeAccount = e.FPayeeAccount,
            PayeeBank = e.FPayeeBank,
            Remark = e.FRemark,
            DocStatus = e.FDocStatus,
            StatusText = GetStatusText(e.FDocStatus),
            CreatedTime = e.FCreatedTime,
            ModifiedTime = e.FModifiedTime
        }).ToList();

        return new PagedResult<SalaryAdvanceDto>
        {
            Items = result,
            Total = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<SalaryAdvanceDto?> GetByIdAsync(long id)
    {
        var entity = await _db.Set<OaSalaryAdvance>().AsNoTracking().FirstOrDefaultAsync(e => e.FID == id);
        if (entity == null)
            return null;

        var org = await _db.Set<SysOrganization>().AsNoTracking().FirstOrDefaultAsync(o => o.FID == entity.FOrgId);
        var dept = await _db.Set<SysOrganization>().AsNoTracking().FirstOrDefaultAsync(d => d.FID == entity.FDeptId);
        var user = await _db.Set<SysUser>().AsNoTracking().FirstOrDefaultAsync(u => u.FID == entity.FApplicantId);

        return new SalaryAdvanceDto
        {
            Id = entity.FID,
            DocNumber = entity.FDocNumber,
            ApplicantId = entity.FApplicantId,
            ApplicantName = user?.FName ?? "",
            DeptId = entity.FDeptId,
            DeptName = dept?.FName ?? "",
            OrgId = entity.FOrgId,
            OrgName = org?.FName ?? "",
            AdvanceAmount = entity.FAdvanceAmount,
            AdvanceMonth = entity.FAdvanceMonth,
            ApplyReason = entity.FApplyReason,
            PaymentMethod = entity.FPaymentMethod,
            PayeeName = entity.FPayeeName,
            PayeeAccount = entity.FPayeeAccount,
            PayeeBank = entity.FPayeeBank,
            Remark = entity.FRemark,
            DocStatus = entity.FDocStatus,
            StatusText = GetStatusText(entity.FDocStatus),
            CreatedTime = entity.FCreatedTime,
            ModifiedTime = entity.FModifiedTime
        };
    }

    public async Task<SalaryAdvanceDto> CreateAsync(CreateSalaryAdvanceRequest request, long userId)
    {
        // 生成单据编号：YZGZ + yyyyMMdd + 3位序号
        var docNumber = await GenerateDocNumberAsync("YZGZ");

        var entity = new OaSalaryAdvance
        {
            FDocNumber = docNumber,
            FApplicantId = userId,
            FDeptId = request.DeptId,
            FOrgId = request.OrgId,
            FAdvanceAmount = request.AdvanceAmount,
            FAdvanceMonth = request.AdvanceMonth,
            FApplyReason = request.ApplyReason,
            FPaymentMethod = request.PaymentMethod,
            FPayeeName = request.PayeeName,
            FPayeeAccount = request.PayeeAccount,
            FPayeeBank = request.PayeeBank,
            FRemark = request.Remark,
            FDocStatus = 0, // 草稿
            FCreatedTime = DateTime.Now
        };

        _db.Set<OaSalaryAdvance>().Add(entity);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(entity.FID))!;
    }

    public async Task<SalaryAdvanceDto?> UpdateAsync(long id, UpdateSalaryAdvanceRequest request, long userId)
    {
        var entity = await _db.Set<OaSalaryAdvance>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null)
            return null;

        // 只有草稿状态才能编辑
        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的单据才能编辑");

        // 验证是否是申请人本人
        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能编辑自己创建的单据");

        entity.FAdvanceAmount = request.AdvanceAmount;
        entity.FAdvanceMonth = request.AdvanceMonth;
        entity.FApplyReason = request.ApplyReason;
        entity.FPaymentMethod = request.PaymentMethod;
        entity.FPayeeName = request.PayeeName;
        entity.FPayeeAccount = request.PayeeAccount;
        entity.FPayeeBank = request.PayeeBank;
        entity.FRemark = request.Remark;
        entity.FModifiedTime = DateTime.Now;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id, long userId)
    {
        var entity = await _db.Set<OaSalaryAdvance>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null)
            return false;

        // 只有草稿状态才能删除
        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的单据才能删除");

        // 验证是否是申请人本人
        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能删除自己创建的单据");

        _db.Set<OaSalaryAdvance>().Remove(entity);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task SubmitAsync(long id, long userId)
    {
        var entity = await _db.Set<OaSalaryAdvance>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null)
            throw new InvalidOperationException("预支工资单不存在");

        // 只有草稿状态才能提交
        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的单据才能提交审批");

        // 验证是否是申请人本人
        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能提交自己创建的单据");

        throw new NotSupportedException("BPM流程已废除，请通过 CardFlow 发起薪资预支审批。");
    }

    #region 私有辅助方法

    private static string GetStatusText(int status) => status switch
    {
        0 => "草稿",
        1 => "审批中",
        2 => "已通过",
        3 => "已拒绝",
        4 => "已撤回",
        5 => "已作废",
        _ => "未知"
    };

    /// <summary>
    /// 生成单据编号：前缀 + yyyyMMdd + 3位序号
    /// </summary>
    private async Task<string> GenerateDocNumberAsync(string prefix)
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var pattern = $"{prefix}{today}%";

        // 查询当天最大编号
        var maxNumber = await _db.Set<OaSalaryAdvance>()
            .AsNoTracking()
            .Where(e => e.FDocNumber.StartsWith(prefix + today))
            .Select(e => e.FDocNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (!string.IsNullOrEmpty(maxNumber) && maxNumber.Length >= prefix.Length + 11)
        {
            // 解析序号部分
            var seqStr = maxNumber.Substring(prefix.Length + 8);
            if (int.TryParse(seqStr, out var seq))
            {
                sequence = seq + 1;
            }
        }

        return $"{prefix}{today}{sequence:D3}";
    }

    #endregion
}
