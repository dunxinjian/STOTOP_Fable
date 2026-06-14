using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Models;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Entities;
using STOTOP.Module.System.Services;
using Microsoft.Extensions.Configuration;

namespace STOTOP.Module.OA.Services;

public class PettyCashService : IPettyCashService
{
    private readonly STOTOPDbContext _db;
    private readonly IConfiguration _configuration;

    public PettyCashService(STOTOPDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    #region 备用金申请

    public async Task<PagedResult<PettyCashApplyDto>> GetApplyListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var query = _db.Set<OaPettyCashApplication>().AsNoTracking().AsQueryable();

        // 根据用户查询其所属组织的申请单
        var userOrgIds = await _db.Set<SysUserOrganization>()
            .Where(uo => uo.FUserId == userId)
            .Select(uo => uo.FOrgId)
            .ToListAsync();

        query = query.Where(e => userOrgIds.Contains(e.FOrgId));

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

        // 获取关联数据
        var applicantIds = items.Select(e => e.FApplicantId).Distinct().ToList();
        var deptIds = items.Select(e => e.FDeptId).Distinct().ToList();
        var orgIds = items.Select(e => e.FOrgId).Distinct().ToList();

        var users = await _db.Set<SysUser>()
            .AsNoTracking()
            .Where(u => applicantIds.Contains(u.FID))
            .ToDictionaryAsync(u => u.FID, u => u.FName);

        var depts = await _db.Set<SysOrganization>()
            .AsNoTracking()
            .Where(d => deptIds.Contains(d.FID))
            .ToDictionaryAsync(d => d.FID, d => d.FName);

        var orgs = await _db.Set<SysOrganization>()
            .AsNoTracking()
            .Where(o => orgIds.Contains(o.FID))
            .ToDictionaryAsync(o => o.FID, o => o.FName);

        var dtos = items.Select(e => new PettyCashApplyDto
        {
            Id = e.FID,
            DocNumber = e.FDocNumber,
            ApplicantId = e.FApplicantId,
            ApplicantName = users.TryGetValue(e.FApplicantId, out var userName) ? userName : "",
            DeptId = e.FDeptId,
            DeptName = depts.TryGetValue(e.FDeptId, out var deptName) ? deptName : "",
            OrgId = e.FOrgId,
            OrgName = orgs.TryGetValue(e.FOrgId, out var orgName) ? orgName : "",
            ApplyAmount = e.FApplyAmount,
            ApplyReason = e.FApplyReason,
            ExpectedReturnDate = e.FExpectedReturnDate,
            PaymentMethod = e.FPaymentMethod,
            PayeeName = e.FPayeeName,
            PayeeAccount = e.FPayeeAccount,
            PayeeBank = e.FPayeeBank,
            Remark = e.FRemark,
            DocStatus = e.FDocStatus,
            StatusText = GetStatusText(e.FDocStatus),
            ReimbursedAmount = e.FReimbursedAmount,
            RepaidAmount = e.FRepaidAmount,
            OutstandingBalance = e.FOutstandingBalance,
            CreatedTime = e.FCreatedTime
        }).ToList();

        return new PagedResult<PettyCashApplyDto>
        {
            Items = dtos,
            Total = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<PettyCashApplyDto?> GetApplyByIdAsync(long id)
    {
        var entity = await _db.Set<OaPettyCashApplication>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null) return null;

        var user = await _db.Set<SysUser>().AsNoTracking().FirstOrDefaultAsync(u => u.FID == entity.FApplicantId);
        var dept = await _db.Set<SysOrganization>().AsNoTracking().FirstOrDefaultAsync(d => d.FID == entity.FDeptId);
        var org = await _db.Set<SysOrganization>().AsNoTracking().FirstOrDefaultAsync(o => o.FID == entity.FOrgId);

        return new PettyCashApplyDto
        {
            Id = entity.FID,
            DocNumber = entity.FDocNumber,
            ApplicantId = entity.FApplicantId,
            ApplicantName = user?.FName ?? "",
            DeptId = entity.FDeptId,
            DeptName = dept?.FName ?? "",
            OrgId = entity.FOrgId,
            OrgName = org?.FName ?? "",
            ApplyAmount = entity.FApplyAmount,
            ApplyReason = entity.FApplyReason,
            ExpectedReturnDate = entity.FExpectedReturnDate,
            PaymentMethod = entity.FPaymentMethod,
            PayeeName = entity.FPayeeName,
            PayeeAccount = entity.FPayeeAccount,
            PayeeBank = entity.FPayeeBank,
            Remark = entity.FRemark,
            DocStatus = entity.FDocStatus,
            StatusText = GetStatusText(entity.FDocStatus),
            ReimbursedAmount = entity.FReimbursedAmount,
            RepaidAmount = entity.FRepaidAmount,
            OutstandingBalance = entity.FOutstandingBalance,
            CreatedTime = entity.FCreatedTime
        };
    }

    public async Task<PettyCashApplyDto> CreateApplyAsync(CreatePettyCashApplyRequest request, long userId)
    {
        // 生成单据编号：BYJSQ + yyyyMMdd + 3位序号
        var docNumber = await GenerateDocNumberAsync("BYJSQ");

        var entity = new OaPettyCashApplication
        {
            FDocNumber = docNumber,
            FApplicantId = userId,
            FDeptId = request.DeptId,
            FOrgId = request.OrgId,
            FApplyAmount = request.ApplyAmount,
            FApplyReason = request.ApplyReason,
            FExpectedReturnDate = request.ExpectedReturnDate,
            FPaymentMethod = request.PaymentMethod,
            FPayeeName = request.PayeeName,
            FPayeeAccount = request.PayeeAccount,
            FPayeeBank = request.PayeeBank,
            FRemark = request.Remark,
            FDocStatus = 0, // 草稿
            FReimbursedAmount = 0,
            FRepaidAmount = 0,
            FOutstandingBalance = request.ApplyAmount, // 初始余额等于申请金额
            FCreatedTime = DateTime.Now
        };

        _db.Set<OaPettyCashApplication>().Add(entity);
        await _db.SaveChangesAsync();

        return (await GetApplyByIdAsync(entity.FID))!;
    }

    public async Task<PettyCashApplyDto?> UpdateApplyAsync(long id, UpdatePettyCashApplyRequest request, long userId)
    {
        var entity = await _db.Set<OaPettyCashApplication>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null) return null;

        // 只有草稿状态才能修改
        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的申请单才能修改");

        // 检查是否是申请人本人
        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能修改自己提交的申请单");

        entity.FApplyAmount = request.ApplyAmount;
        entity.FApplyReason = request.ApplyReason;
        entity.FExpectedReturnDate = request.ExpectedReturnDate;
        entity.FPaymentMethod = request.PaymentMethod;
        entity.FPayeeName = request.PayeeName;
        entity.FPayeeAccount = request.PayeeAccount;
        entity.FPayeeBank = request.PayeeBank;
        entity.FRemark = request.Remark;
        entity.FOutstandingBalance = request.ApplyAmount - entity.FReimbursedAmount - entity.FRepaidAmount;

        await _db.SaveChangesAsync();

        return await GetApplyByIdAsync(id);
    }

    public async Task SubmitApplyAsync(long id, long userId)
    {
        var entity = await _db.Set<OaPettyCashApplication>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null)
            throw new InvalidOperationException("备用金申请单不存在");

        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的申请单才能提交审批");

        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能提交自己的申请单");

        // 更新状态为审批中
        entity.FDocStatus = 1;
        await _db.SaveChangesAsync();

        // 创建流程实例并启动工作流
        await CreateProcessInstanceAndStartWorkflowAsync(
            "PettyCashApply",
            "备用金申请流程",
            entity.FID,
            userId,
            entity.FOrgId);
    }

    #endregion

    #region 备用金报销

    public async Task<PagedResult<PettyCashReimburseDto>> GetReimburseListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var query = _db.Set<OaPettyCashReimbursement>()
            .Include(r => r.Details)
            .AsNoTracking()
            .AsQueryable();

        // 根据用户查询其所属组织的报销单
        var userOrgIds = await _db.Set<SysUserOrganization>()
            .Where(uo => uo.FUserId == userId)
            .Select(uo => uo.FOrgId)
            .ToListAsync();

        query = query.Where(e => userOrgIds.Contains(e.FOrgId));

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

        // 获取关联数据
        var applicantIds = items.Select(e => e.FApplicantId).Distinct().ToList();
        var users = await _db.Set<SysUser>()
            .AsNoTracking()
            .Where(u => applicantIds.Contains(u.FID))
            .ToDictionaryAsync(u => u.FID, u => u.FName);

        var dtos = items.Select(e => new PettyCashReimburseDto
        {
            Id = e.FID,
            DocNumber = e.FDocNumber,
            ApplicantId = e.FApplicantId,
            ApplicantName = users.TryGetValue(e.FApplicantId, out var userName) ? userName : "",
            OrgId = e.FOrgId,
            ApplicationRefId = e.FApplicationRefId,
            TotalAmount = e.FTotalAmount,
            Reason = e.FReason,
            AttachmentCount = e.FAttachmentCount,
            Remark = e.FRemark,
            DocStatus = e.FDocStatus,
            StatusText = GetStatusText(e.FDocStatus),
            CreatedTime = e.FCreatedTime,
            Details = e.Details.Select(d => new PettyCashReimburseDetailDto
            {
                Id = d.FID,
                LineNo = d.FLineNo,
                ExpenseType = d.FExpenseType,
                ExpenseAccountCode = d.FExpenseAccountCode,
                Summary = d.FSummary,
                Amount = d.FAmount,
                OccurDate = d.FOccurDate
            }).ToList()
        }).ToList();

        return new PagedResult<PettyCashReimburseDto>
        {
            Items = dtos,
            Total = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<PettyCashReimburseDto> CreateReimburseAsync(CreatePettyCashReimburseRequest request, long userId)
    {
        // 验证关联的备用金申请单
        var application = await _db.Set<OaPettyCashApplication>()
            .FirstOrDefaultAsync(a => a.FID == request.ApplicationRefId);

        if (application == null)
            throw new InvalidOperationException("关联的备用金申请单不存在");

        if (application.FDocStatus != 2)
            throw new InvalidOperationException("只有已通过的备用金申请单才能进行报销");

        // 计算报销总金额
        var totalAmount = request.Details.Sum(d => d.Amount);

        // 检查报销金额不超过可用余额
        if (totalAmount > application.FOutstandingBalance)
            throw new InvalidOperationException($"报销金额({totalAmount:F2})超过可用余额({application.FOutstandingBalance:F2})");

        // 生成单据编号：BYJBX + yyyyMMdd + 3位序号
        var docNumber = await GenerateDocNumberAsync("BYJBX");

        var entity = new OaPettyCashReimbursement
        {
            FDocNumber = docNumber,
            FApplicantId = userId,
            FDeptId = request.DeptId,
            FOrgId = request.OrgId,
            FApplicationRefId = request.ApplicationRefId,
            FTotalAmount = totalAmount,
            FReason = request.Reason,
            FAttachmentCount = 0,
            FRemark = request.Remark,
            FDocStatus = 0, // 草稿
            FCreatedTime = DateTime.Now
        };

        // 添加明细
        int lineNo = 1;
        foreach (var detail in request.Details)
        {
            entity.Details.Add(new OaPettyCashReimbursementDetail
            {
                FLineNo = lineNo++,
                FExpenseType = detail.ExpenseType,
                FExpenseAccountCode = detail.ExpenseAccountCode,
                FSummary = detail.Summary,
                FAmount = detail.Amount,
                FOccurDate = detail.OccurDate
            });
        }

        _db.Set<OaPettyCashReimbursement>().Add(entity);
        await _db.SaveChangesAsync();

        return await GetReimburseByIdInternalAsync(entity.FID);
    }

    public async Task SubmitReimburseAsync(long id, long userId)
    {
        var entity = await _db.Set<OaPettyCashReimbursement>()
            .AsTracking()
            .Include(r => r.Details)
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null)
            throw new InvalidOperationException("备用金报销单不存在");

        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的报销单才能提交审批");

        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能提交自己的报销单");

        // 再次验证可用余额
        var application = await _db.Set<OaPettyCashApplication>()
            .FirstOrDefaultAsync(a => a.FID == entity.FApplicationRefId);

        if (application == null)
            throw new InvalidOperationException("关联的备用金申请单不存在");

        if (entity.FTotalAmount > application.FOutstandingBalance)
            throw new InvalidOperationException("报销金额超过可用余额，请重新核算");

        // 更新状态为审批中
        entity.FDocStatus = 1;
        await _db.SaveChangesAsync();

        // 创建流程实例并启动工作流
        await CreateProcessInstanceAndStartWorkflowAsync(
            "PettyCashReimburse",
            "备用金报销流程",
            entity.FID,
            userId,
            entity.FOrgId);
    }

    private async Task<PettyCashReimburseDto> GetReimburseByIdInternalAsync(long id)
    {
        var entity = await _db.Set<OaPettyCashReimbursement>()
            .Include(r => r.Details)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null) return null!;

        var user = await _db.Set<SysUser>().AsNoTracking().FirstOrDefaultAsync(u => u.FID == entity.FApplicantId);

        return new PettyCashReimburseDto
        {
            Id = entity.FID,
            DocNumber = entity.FDocNumber,
            ApplicantId = entity.FApplicantId,
            ApplicantName = user?.FName ?? "",
            OrgId = entity.FOrgId,
            ApplicationRefId = entity.FApplicationRefId,
            TotalAmount = entity.FTotalAmount,
            Reason = entity.FReason,
            AttachmentCount = entity.FAttachmentCount,
            Remark = entity.FRemark,
            DocStatus = entity.FDocStatus,
            StatusText = GetStatusText(entity.FDocStatus),
            CreatedTime = entity.FCreatedTime,
            Details = entity.Details.Select(d => new PettyCashReimburseDetailDto
            {
                Id = d.FID,
                LineNo = d.FLineNo,
                ExpenseType = d.FExpenseType,
                ExpenseAccountCode = d.FExpenseAccountCode,
                Summary = d.FSummary,
                Amount = d.FAmount,
                OccurDate = d.FOccurDate
            }).ToList()
        };
    }

    #endregion

    #region 备用金还款

    public async Task<PagedResult<PettyCashReturnDto>> GetReturnListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var query = _db.Set<OaPettyCashReturn>()
            .AsNoTracking()
            .AsQueryable();

        // 根据用户查询其所属组织的还款单
        var userOrgIds = await _db.Set<SysUserOrganization>()
            .Where(uo => uo.FUserId == userId)
            .Select(uo => uo.FOrgId)
            .ToListAsync();

        query = query.Where(e => userOrgIds.Contains(e.FOrgId));

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

        // 获取关联数据
        var applicantIds = items.Select(e => e.FApplicantId).Distinct().ToList();
        var users = await _db.Set<SysUser>()
            .AsNoTracking()
            .Where(u => applicantIds.Contains(u.FID))
            .ToDictionaryAsync(u => u.FID, u => u.FName);

        var dtos = items.Select(e => new PettyCashReturnDto
        {
            Id = e.FID,
            DocNumber = e.FDocNumber,
            ApplicantId = e.FApplicantId,
            ApplicantName = users.TryGetValue(e.FApplicantId, out var userName) ? userName : "",
            OrgId = e.FOrgId,
            ApplicationRefId = e.FApplicationRefId,
            ReturnAmount = e.FReturnAmount,
            ReturnMethod = e.FReturnMethod,
            ReturnNote = e.FReturnNote,
            DocStatus = e.FDocStatus,
            StatusText = GetStatusText(e.FDocStatus),
            CreatedTime = e.FCreatedTime
        }).ToList();

        return new PagedResult<PettyCashReturnDto>
        {
            Items = dtos,
            Total = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<PettyCashReturnDto> CreateReturnAsync(CreatePettyCashReturnRequest request, long userId)
    {
        // 验证关联的备用金申请单
        var application = await _db.Set<OaPettyCashApplication>()
            .FirstOrDefaultAsync(a => a.FID == request.ApplicationRefId);

        if (application == null)
            throw new InvalidOperationException("关联的备用金申请单不存在");

        if (application.FDocStatus != 2)
            throw new InvalidOperationException("只有已通过的备用金申请单才能进行还款");

        // 检查还款金额不超过可用余额
        if (request.ReturnAmount > application.FOutstandingBalance)
            throw new InvalidOperationException($"还款金额({request.ReturnAmount:F2})超过可用余额({application.FOutstandingBalance:F2})");

        // 生成单据编号：BYJHK + yyyyMMdd + 3位序号
        var docNumber = await GenerateDocNumberAsync("BYJHK");

        var entity = new OaPettyCashReturn
        {
            FDocNumber = docNumber,
            FApplicantId = userId,
            FDeptId = request.DeptId,
            FOrgId = request.OrgId,
            FApplicationRefId = request.ApplicationRefId,
            FReturnAmount = request.ReturnAmount,
            FReturnMethod = request.ReturnMethod,
            FReturnNote = request.ReturnNote,
            FDocStatus = 0, // 草稿
            FCreatedTime = DateTime.Now
        };

        _db.Set<OaPettyCashReturn>().Add(entity);
        await _db.SaveChangesAsync();

        return await GetReturnByIdInternalAsync(entity.FID);
    }

    public async Task SubmitReturnAsync(long id, long userId)
    {
        var entity = await _db.Set<OaPettyCashReturn>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null)
            throw new InvalidOperationException("备用金还款单不存在");

        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的还款单才能提交审批");

        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能提交自己的还款单");

        // 再次验证可用余额
        var application = await _db.Set<OaPettyCashApplication>()
            .FirstOrDefaultAsync(a => a.FID == entity.FApplicationRefId);

        if (application == null)
            throw new InvalidOperationException("关联的备用金申请单不存在");

        if (entity.FReturnAmount > application.FOutstandingBalance)
            throw new InvalidOperationException("还款金额超过可用余额，请重新核算");

        // 更新状态为审批中
        entity.FDocStatus = 1;
        await _db.SaveChangesAsync();

        // 创建流程实例并启动工作流
        await CreateProcessInstanceAndStartWorkflowAsync(
            "PettyCashReturn",
            "备用金还款流程",
            entity.FID,
            userId,
            entity.FOrgId);
    }

    private async Task<PettyCashReturnDto> GetReturnByIdInternalAsync(long id)
    {
        var entity = await _db.Set<OaPettyCashReturn>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null) return null!;

        var user = await _db.Set<SysUser>().AsNoTracking().FirstOrDefaultAsync(u => u.FID == entity.FApplicantId);

        return new PettyCashReturnDto
        {
            Id = entity.FID,
            DocNumber = entity.FDocNumber,
            ApplicantId = entity.FApplicantId,
            ApplicantName = user?.FName ?? "",
            OrgId = entity.FOrgId,
            ApplicationRefId = entity.FApplicationRefId,
            ReturnAmount = entity.FReturnAmount,
            ReturnMethod = entity.FReturnMethod,
            ReturnNote = entity.FReturnNote,
            DocStatus = entity.FDocStatus,
            StatusText = GetStatusText(entity.FDocStatus),
            CreatedTime = entity.FCreatedTime
        };
    }

    #endregion

    #region 备用金冲销

    public async Task<PagedResult<PettyCashWriteOffDto>> GetWriteOffListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var query = _db.Set<OaPettyCashWriteOff>()
            .AsNoTracking()
            .AsQueryable();

        // 根据用户查询其所属组织的冲销单
        var userOrgIds = await _db.Set<SysUserOrganization>()
            .Where(uo => uo.FUserId == userId)
            .Select(uo => uo.FOrgId)
            .ToListAsync();

        query = query.Where(e => userOrgIds.Contains(e.FOrgId));

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

        // 获取关联数据
        var applicantIds = items.Select(e => e.FApplicantId).Distinct().ToList();
        var users = await _db.Set<SysUser>()
            .AsNoTracking()
            .Where(u => applicantIds.Contains(u.FID))
            .ToDictionaryAsync(u => u.FID, u => u.FName);

        var dtos = items.Select(e => new PettyCashWriteOffDto
        {
            Id = e.FID,
            DocNumber = e.FDocNumber,
            ApplicantId = e.FApplicantId,
            ApplicantName = users.TryGetValue(e.FApplicantId, out var userName) ? userName : "",
            OrgId = e.FOrgId,
            ApplicationRefId = e.FApplicationRefId,
            OriginalAmount = e.FOriginalAmount,
            ReimbursedTotal = e.FReimbursedTotal,
            ReturnedTotal = e.FReturnedTotal,
            Difference = e.FDifference,
            DifferenceDirection = e.FDifferenceDirection,
            Remark = e.FRemark,
            DocStatus = e.FDocStatus,
            StatusText = GetStatusText(e.FDocStatus),
            CreatedTime = e.FCreatedTime
        }).ToList();

        return new PagedResult<PettyCashWriteOffDto>
        {
            Items = dtos,
            Total = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    public async Task<PettyCashWriteOffDto> CreateWriteOffAsync(CreatePettyCashWriteOffRequest request, long userId)
    {
        // 验证关联的备用金申请单
        var application = await _db.Set<OaPettyCashApplication>()
            .FirstOrDefaultAsync(a => a.FID == request.ApplicationRefId);

        if (application == null)
            throw new InvalidOperationException("关联的备用金申请单不存在");

        if (application.FDocStatus != 2 && application.FDocStatus != 5)
            throw new InvalidOperationException("只有已通过或已冲销的备用金申请单才能进行冲销操作");

        // 生成单据编号：BYJCX + yyyyMMdd + 3位序号
        var docNumber = await GenerateDocNumberAsync("BYJCX");

        // 计算差额
        var difference = application.FOutstandingBalance;
        string differenceDirection;

        if (difference > 0)
        {
            // 有余额未冲销，员工需退还
            differenceDirection = "员工退回";
        }
        else if (difference < 0)
        {
            // 已报销/还款超过申请金额，需补付员工
            differenceDirection = "补付员工";
            difference = Math.Abs(difference);
        }
        else
        {
            // 刚好冲平
            differenceDirection = "刚好冲平";
        }

        var entity = new OaPettyCashWriteOff
        {
            FDocNumber = docNumber,
            FApplicantId = userId,
            FDeptId = request.DeptId,
            FOrgId = request.OrgId,
            FApplicationRefId = request.ApplicationRefId,
            FOriginalAmount = application.FApplyAmount,
            FReimbursedTotal = application.FReimbursedAmount,
            FReturnedTotal = application.FRepaidAmount,
            FDifference = difference,
            FDifferenceDirection = differenceDirection,
            FRemark = request.Remark,
            FDocStatus = 0, // 草稿
            FCreatedTime = DateTime.Now
        };

        _db.Set<OaPettyCashWriteOff>().Add(entity);
        await _db.SaveChangesAsync();

        return await GetWriteOffByIdInternalAsync(entity.FID);
    }

    public async Task SubmitWriteOffAsync(long id, long userId)
    {
        var entity = await _db.Set<OaPettyCashWriteOff>()
            .AsTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null)
            throw new InvalidOperationException("备用金冲销单不存在");

        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的冲销单才能提交审批");

        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只能提交自己的冲销单");

        // 更新状态为审批中
        entity.FDocStatus = 1;
        await _db.SaveChangesAsync();

        // 创建流程实例并启动工作流
        await CreateProcessInstanceAndStartWorkflowAsync(
            "PettyCashWriteOff",
            "备用金冲销流程",
            entity.FID,
            userId,
            entity.FOrgId);
    }

    private async Task<PettyCashWriteOffDto> GetWriteOffByIdInternalAsync(long id)
    {
        var entity = await _db.Set<OaPettyCashWriteOff>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.FID == id);

        if (entity == null) return null!;

        var user = await _db.Set<SysUser>().AsNoTracking().FirstOrDefaultAsync(u => u.FID == entity.FApplicantId);

        return new PettyCashWriteOffDto
        {
            Id = entity.FID,
            DocNumber = entity.FDocNumber,
            ApplicantId = entity.FApplicantId,
            ApplicantName = user?.FName ?? "",
            OrgId = entity.FOrgId,
            ApplicationRefId = entity.FApplicationRefId,
            OriginalAmount = entity.FOriginalAmount,
            ReimbursedTotal = entity.FReimbursedTotal,
            ReturnedTotal = entity.FReturnedTotal,
            Difference = entity.FDifference,
            DifferenceDirection = entity.FDifferenceDirection,
            Remark = entity.FRemark,
            DocStatus = entity.FDocStatus,
            StatusText = GetStatusText(entity.FDocStatus),
            CreatedTime = entity.FCreatedTime
        };
    }

    #endregion

    #region 备用金台账

    public async Task<List<PettyCashLedgerDto>> GetLedgerAsync(long? orgId, long? applicantId)
    {
        var query = _db.Set<OaPettyCashApplication>().AsNoTracking().AsQueryable();
        if (orgId.HasValue)
            query = query.Where(e => e.FOrgId == orgId.Value);
        if (applicantId.HasValue)
            query = query.Where(e => e.FApplicantId == applicantId.Value);
        // 只查未全额冲销的
        query = query.Where(e => e.FDocStatus == 2 && e.FOutstandingBalance > 0);

        var applies = await query.OrderByDescending(e => e.FCreatedTime).ToListAsync();
        var applicantIds = applies.Select(e => e.FApplicantId).Distinct().ToList();
        var users = await _db.Set<SysUser>()
            .AsNoTracking()
            .Where(u => applicantIds.Contains(u.FID))
            .ToDictionaryAsync(u => u.FID, u => u.FName);

        return applies.Select(e => new PettyCashLedgerDto
        {
            ApplicationId = e.FID,
            DocNumber = e.FDocNumber,
            ApplicantName = users.TryGetValue(e.FApplicantId, out var name) ? name : "",
            ApplyAmount = e.FApplyAmount,
            ReimbursedAmount = e.FReimbursedAmount,
            RepaidAmount = e.FRepaidAmount,
            OutstandingBalance = e.FOutstandingBalance,
            DocStatus = e.FDocStatus,
            StatusText = GetStatusText(e.FDocStatus),
            CreatedTime = e.FCreatedTime,
        }).ToList();
    }

    #endregion

    #region 私有辅助方法

    private static string GetStatusText(int status) => status switch
    {
        0 => "草稿",
        1 => "审批中",
        2 => "已通过",
        3 => "已拒绝",
        4 => "已撤回",
        5 => "已冲销",
        _ => "未知"
    };

    /// <summary>
    /// 生成单据编号
    /// </summary>
    private async Task<string> GenerateDocNumberAsync(string prefix)
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var likePattern = $"{prefix}{today}%";

        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // 查询当日最大编号（跨所有备用金相关表）
        var maxFromApply = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT MAX(F单据编号) FROM OA备用金申请单 WHERE F单据编号 LIKE @Pattern", new { Pattern = likePattern });
        var maxFromReimburse = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT MAX(F单据编号) FROM OA备用金报销单 WHERE F单据编号 LIKE @Pattern", new { Pattern = likePattern });
        var maxFromReturn = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT MAX(F单据编号) FROM OA备用金还款单 WHERE F单据编号 LIKE @Pattern", new { Pattern = likePattern });
        var maxFromWriteOff = await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT MAX(F单据编号) FROM OA备用金冲销单 WHERE F单据编号 LIKE @Pattern", new { Pattern = likePattern });

        var allMax = new[] { maxFromApply, maxFromReimburse, maxFromReturn, maxFromWriteOff }
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        int nextSeq = 1;
        if (allMax.Count > 0)
        {
            var maxNumber = allMax.Max()!;
            // 提取序号部分
            var seqStr = maxNumber.Substring(prefix.Length + 8); // prefix(5) + date(8) = 13 chars
            if (int.TryParse(seqStr, out int currentSeq))
            {
                nextSeq = currentSeq + 1;
            }
        }

        return $"{prefix}{today}{nextSeq:D3}";
    }

    /// <summary>
    /// 发起审批
    /// </summary>
    private async Task CreateProcessInstanceAndStartWorkflowAsync(
        string bizDocType,
        string processName,
        long bizDocId,
        long initiatorId,
        long orgId)
    {
        await Task.CompletedTask;
        throw new NotSupportedException($"BPM流程已废除，请通过 CardFlow 发起{processName}审批。");
    }

    #endregion
}
