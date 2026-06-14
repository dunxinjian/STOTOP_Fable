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
using STOTOP.Module.System.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace STOTOP.Module.OA.Services;

public class ExpenseReimburseService : IExpenseReimburseService
{
    private readonly STOTOPDbContext _db;
    private readonly IOrgContextService _orgContextService;
    private readonly IConfiguration _configuration;

    public ExpenseReimburseService(
        STOTOPDbContext db,
        IOrgContextService orgContextService,
        IConfiguration configuration)
    {
        _db = db;
        _orgContextService = orgContextService;
        _configuration = configuration;
    }

    /// <summary>
    /// 分页查询费用报销单，支持状态/组织筛选，JOIN用户表获取申请人姓名
    /// </summary>
    public async Task<PagedResult<ExpenseReimburseDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var query = _db.Set<OaExpenseReimbursement>()
            .Include(r => r.Details)
            .AsQueryable();

        // 筛选条件
        if (status.HasValue)
        {
            query = query.Where(r => r.FDocStatus == status.Value);
        }

        if (orgId.HasValue)
        {
            query = query.Where(r => r.FOrgId == orgId.Value);
        }

        // 按创建时间倒序
        query = query.OrderByDescending(r => r.FCreatedTime);

        // 总数
        var total = await query.CountAsync();

        // 分页
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 获取关联的用户和组织信息
        var applicantIds = items.Select(r => r.FApplicantId).Distinct().ToList();
        var orgIds = items.Select(r => r.FOrgId).Distinct().ToList();
        var deptIds = items.Select(r => r.FDeptId).Distinct().ToList();

        var applicants = await _db.Set<SysUser>()
            .Where(u => applicantIds.Contains(u.FID))
            .ToDictionaryAsync(u => u.FID, u => u.FName);

        var orgs = await _db.Set<SysOrganization>()
            .Where(o => orgIds.Contains(o.FID))
            .ToDictionaryAsync(o => o.FID, o => o.FName);

        var depts = await _db.Set<SysOrganization>()
            .Where(d => deptIds.Contains(d.FID))
            .ToDictionaryAsync(d => d.FID, d => d.FName);

        // 映射为DTO
        var dtoList = items.Select(r => new ExpenseReimburseDto
        {
            Id = r.FID,
            DocNumber = r.FDocNumber,
            ApplicantId = r.FApplicantId,
            ApplicantName = applicants.GetValueOrDefault(r.FApplicantId, string.Empty),
            DeptId = r.FDeptId,
            DeptName = depts.GetValueOrDefault(r.FDeptId, string.Empty),
            OrgId = r.FOrgId,
            OrgName = orgs.GetValueOrDefault(r.FOrgId, string.Empty),
            Reason = r.FReason,
            TotalAmount = r.FTotalAmount,
            RequestRefId = r.FRequestRefId,
            LoanRefId = r.FLoanRefId,
            PaymentMethod = r.FPaymentMethod,
            PayeeName = r.FPayeeName,
            PayeeAccount = r.FPayeeAccount,
            PayeeBank = r.FPayeeBank,
            AttachmentCount = r.FAttachmentCount,
            Remark = r.FRemark,
            DocStatus = r.FDocStatus,
            StatusText = GetStatusText(r.FDocStatus),
            CreatedTime = r.FCreatedTime,
            ModifiedTime = r.FModifiedTime,
            Details = r.Details.OrderBy(d => d.FLineNo).Select(d => new ExpenseReimburseDetailDto
            {
                Id = d.FID,
                LineNo = d.FLineNo,
                ExpenseType = d.FExpenseType,
                ExpenseAccountCode = d.FExpenseAccountCode,
                Summary = d.FSummary,
                Amount = d.FAmount,
                OccurDate = d.FOccurDate,
                Remark = d.FRemark
            }).ToList()
        }).ToList();

        return new PagedResult<ExpenseReimburseDto>
        {
            Items = dtoList,
            Total = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 获取费用报销单详情（含明细子表）
    /// </summary>
    public async Task<ExpenseReimburseDto?> GetByIdAsync(long id)
    {
        var entity = await _db.Set<OaExpenseReimbursement>()
            .Include(r => r.Details.OrderBy(d => d.FLineNo))
            .FirstOrDefaultAsync(r => r.FID == id);

        if (entity == null)
            return null;

        // 获取关联信息
        var applicant = await _db.Set<SysUser>().FindAsync(entity.FApplicantId);
        var org = await _db.Set<SysOrganization>().FindAsync(entity.FOrgId);
        var dept = await _db.Set<SysOrganization>().FindAsync(entity.FDeptId);

        return new ExpenseReimburseDto
        {
            Id = entity.FID,
            DocNumber = entity.FDocNumber,
            ApplicantId = entity.FApplicantId,
            ApplicantName = applicant?.FName ?? string.Empty,
            DeptId = entity.FDeptId,
            DeptName = dept?.FName ?? string.Empty,
            OrgId = entity.FOrgId,
            OrgName = org?.FName ?? string.Empty,
            Reason = entity.FReason,
            TotalAmount = entity.FTotalAmount,
            RequestRefId = entity.FRequestRefId,
            LoanRefId = entity.FLoanRefId,
            PaymentMethod = entity.FPaymentMethod,
            PayeeName = entity.FPayeeName,
            PayeeAccount = entity.FPayeeAccount,
            PayeeBank = entity.FPayeeBank,
            AttachmentCount = entity.FAttachmentCount,
            Remark = entity.FRemark,
            DocStatus = entity.FDocStatus,
            StatusText = GetStatusText(entity.FDocStatus),
            CreatedTime = entity.FCreatedTime,
            ModifiedTime = entity.FModifiedTime,
            Details = entity.Details.Select(d => new ExpenseReimburseDetailDto
            {
                Id = d.FID,
                LineNo = d.FLineNo,
                ExpenseType = d.FExpenseType,
                ExpenseAccountCode = d.FExpenseAccountCode,
                Summary = d.FSummary,
                Amount = d.FAmount,
                OccurDate = d.FOccurDate,
                Remark = d.FRemark
            }).ToList()
        };
    }

    /// <summary>
    /// 创建费用报销单草稿（编号规则：FYBX + yyyyMMdd + 3位序号）
    /// </summary>
    public async Task<ExpenseReimburseDto> CreateAsync(CreateExpenseReimburseRequest request, long userId)
    {
        // 验证明细
        if (request.Details == null || request.Details.Count == 0)
            throw new InvalidOperationException("报销明细不能为空");

        // 计算总金额
        var totalAmount = request.Details.Sum(d => d.Amount);

        // 生成单据编号
        var docNumber = await GenerateDocNumberAsync();

        // 获取用户所属部门（从任职组织推导可切换祖先）
        var userOrgs = await _orgContextService.GetUserOrganizationsAsync(userId);
        var userOrg = userOrgs.FirstOrDefault(uo => uo.OrgId == request.OrgId);
        var deptId = userOrg?.SwitchableOrgId ?? 0;

        long createdId = 0;
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 创建主表
                var entity = new OaExpenseReimbursement
                {
                    FDocNumber = docNumber,
                    FApplicantId = userId,
                    FDeptId = deptId,
                    FOrgId = request.OrgId,
                    FReason = request.Reason,
                    FTotalAmount = totalAmount,
                    FRequestRefId = request.RequestRefId,
                    FLoanRefId = request.LoanRefId,
                    FPaymentMethod = request.PaymentMethod,
                    FPayeeName = request.PayeeName,
                    FPayeeAccount = request.PayeeAccount,
                    FPayeeBank = request.PayeeBank,
                    FAttachmentCount = 0,
                    FRemark = request.Remark,
                    FDocStatus = 0, // 草稿
                    FCreatedTime = DateTime.Now
                };

                _db.Set<OaExpenseReimbursement>().Add(entity);
                await _db.SaveChangesAsync();

                // 创建明细
                var lineNo = 1;
                foreach (var detailReq in request.Details)
                {
                    var detail = new OaExpenseReimbursementDetail
                    {
                        FReimbursementId = entity.FID,
                        FLineNo = lineNo++,
                        FExpenseType = detailReq.ExpenseType,
                        FExpenseAccountCode = detailReq.ExpenseAccountCode,
                        FSummary = detailReq.Summary,
                        FAmount = detailReq.Amount,
                        FOccurDate = detailReq.OccurDate,
                        FRemark = detailReq.Remark
                    };
                    _db.Set<OaExpenseReimbursementDetail>().Add(detail);
                }
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
                createdId = entity.FID;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return (await GetByIdAsync(createdId))!;
    }

    /// <summary>
    /// 更新费用报销单（仅草稿可编辑）
    /// </summary>
    public async Task<ExpenseReimburseDto?> UpdateAsync(long id, UpdateExpenseReimburseRequest request, long userId)
    {
        // 显式启用跟踪（全局 QueryTrackingBehavior=NoTracking），否则 entity 属性修改不会生成 UPDATE
        var entity = await _db.Set<OaExpenseReimbursement>()
            .AsTracking()
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.FID == id);

        if (entity == null)
            return null;

        // 仅草稿可编辑
        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的报销单才能编辑");

        // 验证明细
        if (request.Details == null || request.Details.Count == 0)
            throw new InvalidOperationException("报销明细不能为空");

        // 计算总金额
        var totalAmount = request.Details.Sum(d => d.Amount);

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 更新主表
                entity.FReason = request.Reason;
                entity.FTotalAmount = totalAmount;
                entity.FRequestRefId = request.RequestRefId;
                entity.FLoanRefId = request.LoanRefId;
                entity.FPaymentMethod = request.PaymentMethod;
                entity.FPayeeName = request.PayeeName;
                entity.FPayeeAccount = request.PayeeAccount;
                entity.FPayeeBank = request.PayeeBank;
                entity.FRemark = request.Remark;
                entity.FModifiedTime = DateTime.Now;

                // 删除旧明细
                _db.Set<OaExpenseReimbursementDetail>().RemoveRange(entity.Details);

                // 插入新明细
                var lineNo = 1;
                foreach (var detailReq in request.Details)
                {
                    var detail = new OaExpenseReimbursementDetail
                    {
                        FReimbursementId = entity.FID,
                        FLineNo = lineNo++,
                        FExpenseType = detailReq.ExpenseType,
                        FExpenseAccountCode = detailReq.ExpenseAccountCode,
                        FSummary = detailReq.Summary,
                        FAmount = detailReq.Amount,
                        FOccurDate = detailReq.OccurDate,
                        FRemark = detailReq.Remark
                    };
                    _db.Set<OaExpenseReimbursementDetail>().Add(detail);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 删除费用报销单（仅草稿可删除）
    /// </summary>
    public async Task<bool> DeleteAsync(long id, long userId)
    {
        // 显式启用跟踪，RemoveRange 需要实体处于跟踪状态
        var entity = await _db.Set<OaExpenseReimbursement>()
            .AsTracking()
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.FID == id);

        if (entity == null)
            return false;

        // 仅草稿可删除
        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的报销单才能删除");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 明细会被级联删除，但为了明确，先删除明细
                _db.Set<OaExpenseReimbursementDetail>().RemoveRange(entity.Details);
                _db.Set<OaExpenseReimbursement>().Remove(entity);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return true;
    }

    /// <summary>
    /// 提交审批。
    /// </summary>
    public async Task SubmitAsync(long id, long userId)
    {
        // 显式启用跟踪，否则 entity.FDocStatus = 1 不会生成 UPDATE
        var entity = await _db.Set<OaExpenseReimbursement>()
            .AsTracking()
            .FirstOrDefaultAsync(r => r.FID == id);

        if (entity == null)
            throw new InvalidOperationException("费用报销单不存在");

        // 仅草稿可提交
        if (entity.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的报销单才能提交审批");

        // 验证申请人是否为当前用户
        if (entity.FApplicantId != userId)
            throw new InvalidOperationException("只有申请人才能提交审批");

        throw new NotSupportedException("BPM流程已废除，请通过 CardFlow 发起费用报销审批。");
    }

    /// <summary>
    /// 查询当前组织下状态为"已通过"且未被引用的费用请款单
    /// </summary>
    public async Task<List<AvailableRequestDto>> GetAvailableRequestsAsync(long orgId, long userId)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                r.FID AS Id,
                r.F单据编号 AS DocNumber,
                r.F请款事由 AS Reason,
                r.F请款金额 AS Amount,
                ISNULL(r.F已引用金额, 0) AS ReferencedAmount,
                r.F请款金额 - ISNULL(r.F已引用金额, 0) AS AvailableAmount,
                u.F姓名 AS ApplicantName,
                r.F创建时间 AS CreatedTime
            FROM OA费用请款单 r
            INNER JOIN SYS用户 u ON r.F申请人ID = u.FID
            WHERE r.F组织ID = @OrgId 
              AND r.F单据状态 = 2
              AND (r.F已引用金额 < r.F请款金额 OR r.F已引用金额 IS NULL)
            ORDER BY r.F创建时间 DESC";

        var result = await conn.QueryAsync<AvailableRequestDto>(sql, new { OrgId = orgId });
        return result.ToList();
    }

    /// <summary>
    /// 查询当前组织下状态为"已通过"且未冲销的借款单
    /// </summary>
    public async Task<List<AvailableLoanDto>> GetAvailableLoansAsync(long orgId, long userId)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT 
                l.FID AS Id,
                l.F单据编号 AS DocNumber,
                l.F借款事由 AS LoanReason,
                l.F借款金额 AS LoanAmount,
                l.F未核销余额 AS OutstandingBalance,
                u.F姓名 AS ApplicantName,
                l.F创建时间 AS CreatedTime
            FROM OA借款申请单 l
            INNER JOIN SYS用户 u ON l.F申请人PID = u.FID
            WHERE l.F组织ID = @OrgId 
              AND l.F单据状态 = 2
              AND l.F未核销余额 > 0
            ORDER BY l.F创建时间 DESC";

        var result = await conn.QueryAsync<AvailableLoanDto>(sql, new { OrgId = orgId });
        return result.ToList();
    }

    #region 私有辅助方法

    /// <summary>
    /// 生成单据编号：FYBX + yyyyMMdd + 3位序号
    /// </summary>
    private async Task<string> GenerateDocNumberAsync()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"FYBX{today}";

        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT MAX(F单据编号) 
            FROM OA费用报销单 
            WHERE F单据编号 LIKE @Prefix + '%'";

        var maxNumber = await conn.ExecuteScalarAsync<string?>(sql, new { Prefix = prefix });

        int sequence = 1;
        if (!string.IsNullOrEmpty(maxNumber) && maxNumber.Length >= prefix.Length + 3)
        {
            var seqStr = maxNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out var currentSeq))
            {
                sequence = currentSeq + 1;
            }
        }

        return $"{prefix}{sequence:D3}";
    }

    /// <summary>
    /// 获取状态文本
    /// </summary>
    private static string GetStatusText(int status)
    {
        return status switch
        {
            0 => "草稿",
            1 => "审批中",
            2 => "已通过",
            3 => "已驳回",
            4 => "已撤回",
            5 => "已作废",
            6 => "已冲销",
            _ => "未知"
        };
    }

    #endregion
}
