using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Models;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Entities;

namespace STOTOP.Module.OA.Services;

public class ExternalPaymentService : IExternalPaymentService
{
    private readonly STOTOPDbContext _db;

    public ExternalPaymentService(STOTOPDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 分页查询对外付款单列表
    /// </summary>
    public async Task<PagedResult<ExternalPaymentDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var query = from p in _db.Set<OaExternalPayment>().AsNoTracking()
                    join user in _db.Set<SysUser>().AsNoTracking() on p.FApplicantId equals user.FID
                    join dept in _db.Set<SysOrganization>().AsNoTracking() on p.FDeptId equals dept.FID
                    join org in _db.Set<SysOrganization>().AsNoTracking() on p.FOrgId equals org.FID
                    select new { p, user, dept, org };

        // 只查询当前用户的单据
        query = query.Where(x => x.p.FApplicantId == userId);

        if (status.HasValue)
            query = query.Where(x => x.p.FDocStatus == status.Value);

        if (orgId.HasValue)
            query = query.Where(x => x.p.FOrgId == orgId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.p.FCreatedTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ExternalPaymentDto
            {
                Id = x.p.FID,
                DocNumber = x.p.FDocNumber,
                ApplicantId = x.p.FApplicantId,
                ApplicantName = x.user.FName,
                DeptId = x.p.FDeptId,
                DeptName = x.dept.FName,
                OrgId = x.p.FOrgId,
                OrgName = x.org.FName,
                PaymentReason = x.p.FPaymentReason,
                TotalAmount = x.p.FTotalAmount,
                RequestRefId = x.p.FRequestRefId,
                PayeeName = x.p.FPayeeName,
                PayeeAccount = x.p.FPayeeAccount,
                PayeeBank = x.p.FPayeeBank,
                PaymentMethod = x.p.FPaymentMethod,
                ExpectedPayDate = x.p.FExpectedPayDate,
                ContractNo = x.p.FContractNo,
                InvoiceNo = x.p.FInvoiceNo,
                AttachmentCount = x.p.FAttachmentCount,
                Remark = x.p.FRemark,
                DocStatus = x.p.FDocStatus,
                StatusText = GetStatusText(x.p.FDocStatus),
                CreatedTime = x.p.FCreatedTime,
                ModifiedTime = x.p.FModifiedTime
            })
            .ToListAsync();

        return new PagedResult<ExternalPaymentDto>
        {
            Items = items,
            Total = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 根据ID获取对外付款单详情（含明细）
    /// </summary>
    public async Task<ExternalPaymentDto?> GetByIdAsync(long id)
    {
        var payment = await (from p in _db.Set<OaExternalPayment>().AsNoTracking()
                             join user in _db.Set<SysUser>().AsNoTracking() on p.FApplicantId equals user.FID
                             join dept in _db.Set<SysOrganization>().AsNoTracking() on p.FDeptId equals dept.FID
                             join org in _db.Set<SysOrganization>().AsNoTracking() on p.FOrgId equals org.FID
                             where p.FID == id
                             select new { p, user, dept, org })
                            .FirstOrDefaultAsync();

        if (payment == null)
            return null;

        // 查询明细
        var details = await _db.Set<OaExternalPaymentDetail>()
            .AsNoTracking()
            .Where(d => d.FPaymentId == id)
            .OrderBy(d => d.FLineNo)
            .Select(d => new ExternalPaymentDetailDto
                       {
                Id = d.FID,
                LineNo = d.FLineNo,
                ExpenseType = d.FExpenseType,
                ExpenseAccountCode = d.FExpenseAccountCode,
                Summary = d.FSummary,
                Amount = d.FAmount,
                Remark = d.FRemark
            })
            .ToListAsync();

        return new ExternalPaymentDto
        {
            Id = payment.p.FID,
            DocNumber = payment.p.FDocNumber,
            ApplicantId = payment.p.FApplicantId,
            ApplicantName = payment.user.FName,
            DeptId = payment.p.FDeptId,
            DeptName = payment.dept.FName,
            OrgId = payment.p.FOrgId,
            OrgName = payment.org.FName,
            PaymentReason = payment.p.FPaymentReason,
            TotalAmount = payment.p.FTotalAmount,
            RequestRefId = payment.p.FRequestRefId,
            PayeeName = payment.p.FPayeeName,
            PayeeAccount = payment.p.FPayeeAccount,
            PayeeBank = payment.p.FPayeeBank,
            PaymentMethod = payment.p.FPaymentMethod,
            ExpectedPayDate = payment.p.FExpectedPayDate,
            ContractNo = payment.p.FContractNo,
            InvoiceNo = payment.p.FInvoiceNo,
            AttachmentCount = payment.p.FAttachmentCount,
            Remark = payment.p.FRemark,
            DocStatus = payment.p.FDocStatus,
            StatusText = GetStatusText(payment.p.FDocStatus),
            CreatedTime = payment.p.FCreatedTime,
            ModifiedTime = payment.p.FModifiedTime,
            Details = details
        };
    }

    /// <summary>
    /// 创建对外付款单（草稿）
    /// </summary>
    public async Task<ExternalPaymentDto> CreateAsync(CreateExternalPaymentRequest request, long userId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 生成单据编号：DWFK + yyyyMMdd + 3位序号
            var docNumber = await GenerateDocNumberAsync();

            // 计算总金额
            var totalAmount = request.Details.Sum(d => d.Amount);

            var payment = new OaExternalPayment
            {
                FDocNumber = docNumber,
                FApplicantId = userId,
                FDeptId = request.DeptId,
                FOrgId = request.OrgId,
                FPaymentReason = request.PaymentReason,
                FTotalAmount = totalAmount,
                FRequestRefId = request.RequestRefId,
                FPayeeName = request.PayeeName,
                FPayeeAccount = request.PayeeAccount,
                FPayeeBank = request.PayeeBank,
                FPaymentMethod = request.PaymentMethod,
                FExpectedPayDate = request.ExpectedPayDate,
                FContractNo = request.ContractNo,
                FInvoiceNo = request.InvoiceNo,
                FAttachmentCount = 0,
                FRemark = request.Remark,
                FDocStatus = 0, // 草稿
                FCreatedTime = DateTime.Now
            };

            _db.Set<OaExternalPayment>().Add(payment);
            await _db.SaveChangesAsync();

            // 插入明细
            var lineNo = 1;
            foreach (var detailReq in request.Details)
            {
                var detail = new OaExternalPaymentDetail
                {
                    FPaymentId = payment.FID,
                    FLineNo = lineNo++,
                    FExpenseType = detailReq.ExpenseType,
                    FExpenseAccountCode = detailReq.ExpenseAccountCode,
                    FSummary = detailReq.Summary,
                    FAmount = detailReq.Amount,
                    FRemark = detailReq.Remark
                };
                _db.Set<OaExternalPaymentDetail>().Add(detail);
            }
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return (await GetByIdAsync(payment.FID))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 更新对外付款单（仅草稿可编辑）
    /// </summary>
    public async Task<ExternalPaymentDto?> UpdateAsync(long id, UpdateExternalPaymentRequest request, long userId)
    {
        var payment = await _db.Set<OaExternalPayment>()
            .AsTracking()
            .Include(p => p.Details)
            .FirstOrDefaultAsync(p => p.FID == id);

        if (payment == null)
            return null;

        // 验证权限
        if (payment.FApplicantId != userId)
            throw new InvalidOperationException("您无权修改此单据");

        // 仅草稿可编辑
        if (payment.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的单据才能编辑");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 计算总金额
            var totalAmount = request.Details.Sum(d => d.Amount);

            // 更新主表
            payment.FPaymentReason = request.PaymentReason;
            payment.FTotalAmount = totalAmount;
            payment.FRequestRefId = request.RequestRefId;
            payment.FPayeeName = request.PayeeName;
            payment.FPayeeAccount = request.PayeeAccount;
            payment.FPayeeBank = request.PayeeBank;
            payment.FPaymentMethod = request.PaymentMethod;
            payment.FExpectedPayDate = request.ExpectedPayDate;
            payment.FContractNo = request.ContractNo;
            payment.FInvoiceNo = request.InvoiceNo;
            payment.FRemark = request.Remark;
            payment.FModifiedTime = DateTime.Now;

            // 删除原明细
            _db.Set<OaExternalPaymentDetail>().RemoveRange(payment.Details);

            // 插入新明细
            var lineNo = 1;
            foreach (var detailReq in request.Details)
            {
                var detail = new OaExternalPaymentDetail
                {
                    FPaymentId = payment.FID,
                    FLineNo = lineNo++,
                    FExpenseType = detailReq.ExpenseType,
                    FExpenseAccountCode = detailReq.ExpenseAccountCode,
                    FSummary = detailReq.Summary,
                    FAmount = detailReq.Amount,
                    FRemark = detailReq.Remark
                };
                _db.Set<OaExternalPaymentDetail>().Add(detail);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 删除对外付款单（仅草稿可删除）
    /// </summary>
    public async Task<bool> DeleteAsync(long id, long userId)
    {
        var payment = await _db.Set<OaExternalPayment>()
            .AsTracking()
            .FirstOrDefaultAsync(p => p.FID == id);

        if (payment == null)
            return false;

        // 验证权限
        if (payment.FApplicantId != userId)
            throw new InvalidOperationException("您无权删除此单据");

        // 仅草稿可删除
        if (payment.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的单据才能删除");

        _db.Set<OaExternalPayment>().Remove(payment);
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 提交审批
    /// </summary>
    public async Task SubmitAsync(long id, long userId)
    {
        var payment = await _db.Set<OaExternalPayment>()
            .AsTracking()
            .FirstOrDefaultAsync(p => p.FID == id);

        if (payment == null)
            throw new InvalidOperationException("对外付款单不存在");

        // 验证权限
        if (payment.FApplicantId != userId)
            throw new InvalidOperationException("您无权提交此单据");

        // 仅草稿可提交
        if (payment.FDocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的单据才能提交审批");

        throw new NotSupportedException("BPM流程已废除，请通过 CardFlow 发起对外付款审批。");
    }

    #region 私有辅助方法

    /// <summary>
    /// 生成单据编号：DWFK + yyyyMMdd + 3位序号
    /// </summary>
    private async Task<string> GenerateDocNumberAsync()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"DWFK{today}";

        // 查询今天已有的最大序号
        var maxNumber = await _db.Set<OaExternalPayment>()
            .AsNoTracking()
            .Where(p => p.FDocNumber.StartsWith(prefix))
            .Select(p => p.FDocNumber)
            .MaxAsync();

        int nextSeq = 1;
        if (!string.IsNullOrEmpty(maxNumber) && maxNumber.Length == prefix.Length + 3)
        {
            if (int.TryParse(maxNumber.Substring(prefix.Length), out var seq))
            {
                nextSeq = seq + 1;
            }
        }

        return $"{prefix}{nextSeq:D3}";
    }

    /// <summary>
    /// 获取状态文本
    /// </summary>
    private static string GetStatusText(int status) => status switch
    {
        0 => "草稿",
        1 => "审批中",
        2 => "已通过",
        3 => "已拒绝",
        4 => "已撤回",
        _ => "未知"
    };

    #endregion
}
