using Dapper;
using Microsoft.Data.SqlClient;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Services;
using Microsoft.Extensions.Configuration;

namespace STOTOP.Module.OA.Services;

/// <summary>
/// 费用请款服务实现
/// </summary>
public class ExpenseRequestService : IExpenseRequestService
{
    private readonly IConfiguration _configuration;

    public ExpenseRequestService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 分页查询费用请款单列表
    /// </summary>
    public async Task<PagedResult<ExpenseRequestDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // 构建查询条件
        var whereClauses = new List<string> { "1=1" };
        var parameters = new DynamicParameters();

        if (status.HasValue)
        {
            whereClauses.Add("er.F单据状态 = @Status");
            parameters.Add("Status", status.Value);
        }

        if (orgId.HasValue)
        {
            whereClauses.Add("er.F组织ID = @OrgId");
            parameters.Add("OrgId", orgId.Value);
        }

        // 只查询当前用户创建的单据
        whereClauses.Add("er.F申请人ID = @UserId");
        parameters.Add("UserId", userId);

        var whereClause = string.Join(" AND ", whereClauses);

        // 查询总数
        var countSql = $@"
            SELECT COUNT(1)
            FROM [OA费用请款单] er
            WHERE {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // 分页查询列表
        var offset = (page - 1) * pageSize;
        var listSql = $@"
            SELECT 
                er.FID AS Id,
                er.F单据编号 AS DocNumber,
                er.F申请人ID AS ApplicantId,
                u.F姓名 AS ApplicantName,
                er.F部门ID AS DeptId,
                d.F名称 AS DeptName,
                er.F组织ID AS OrgId,
                o.F名称 AS OrgName,
                er.F请款事由 AS Reason,
                er.F请款金额 AS Amount,
                er.F期望付款日期 AS ExpectedPayDate,
                er.F费用类型 AS ExpenseType,
                er.F收款方名称 AS PayeeName,
                er.F收款方账号 AS PayeeAccount,
                er.F收款方开户行 AS PayeeBank,
                er.F备注 AS Remark,
                er.F单据状态 AS DocStatus,
                er.F已引用金额 AS ReferencedAmount,
                er.F创建时间 AS CreatedTime,
                er.F修改时间 AS ModifiedTime
            FROM [OA费用请款单] er
            LEFT JOIN [SYS用户] u ON er.F申请人ID = u.FID
            LEFT JOIN [SYS部门] d ON er.F部门ID = d.FID
            LEFT JOIN [SYS组织架构] o ON er.F组织ID = o.FID
            WHERE {whereClause}
            ORDER BY er.F创建时间 DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var items = await connection.QueryAsync<ExpenseRequestDto>(listSql, parameters);
        var itemList = items.ToList();

        // 设置状态文本
        foreach (var item in itemList)
        {
            item.StatusText = GetStatusText(item.DocStatus);
        }

        return new PagedResult<ExpenseRequestDto>
        {
            Items = itemList,
            Total = totalCount,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 获取单据详情
    /// </summary>
    public async Task<ExpenseRequestDto?> GetByIdAsync(long id)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT 
                er.FID AS Id,
                er.F单据编号 AS DocNumber,
                er.F申请人ID AS ApplicantId,
                u.F姓名 AS ApplicantName,
                er.F部门ID AS DeptId,
                d.F名称 AS DeptName,
                er.F组织ID AS OrgId,
                o.F名称 AS OrgName,
                er.F请款事由 AS Reason,
                er.F请款金额 AS Amount,
                er.F期望付款日期 AS ExpectedPayDate,
                er.F费用类型 AS ExpenseType,
                er.F收款方名称 AS PayeeName,
                er.F收款方账号 AS PayeeAccount,
                er.F收款方开户行 AS PayeeBank,
                er.F备注 AS Remark,
                er.F单据状态 AS DocStatus,
                er.F已引用金额 AS ReferencedAmount,
                er.F创建时间 AS CreatedTime,
                er.F修改时间 AS ModifiedTime
            FROM [OA费用请款单] er
            LEFT JOIN [SYS用户] u ON er.F申请人ID = u.FID
            LEFT JOIN [SYS部门] d ON er.F部门ID = d.FID
            LEFT JOIN [SYS组织架构] o ON er.F组织ID = o.FID
            WHERE er.FID = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<ExpenseRequestDto>(sql, new { Id = id });

        if (result != null)
        {
            result.StatusText = GetStatusText(result.DocStatus);
        }

        return result;
    }

    /// <summary>
    /// 创建草稿
    /// </summary>
    public async Task<ExpenseRequestDto> CreateAsync(CreateExpenseRequestRequest request, long userId)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // 生成单据编号
        var docNumber = await GenerateDocNumberAsync(connection);

        var now = DateTime.Now;
        var sql = @"
            INSERT INTO [OA费用请款单] (
                F单据编号, F申请人ID, F部门ID, F组织ID, F请款事由, F请款金额,
                F期望付款日期, F费用类型, F收款方名称, F收款方账号, F收款方开户行,
                F备注, F单据状态, F已引用金额, F创建时间
            ) VALUES (
                @DocNumber, @ApplicantId, @DeptId, @OrgId, @Reason, @Amount,
                @ExpectedPayDate, @ExpenseType, @PayeeName, @PayeeAccount, @PayeeBank,
                @Remark, 0, 0, @CreatedTime
            );
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var id = await connection.ExecuteScalarAsync<long>(sql, new
        {
            DocNumber = docNumber,
            ApplicantId = userId,
            DeptId = request.DeptId,
            OrgId = request.OrgId,
            Reason = request.Reason,
            Amount = request.Amount,
            ExpectedPayDate = request.ExpectedPayDate,
            ExpenseType = request.ExpenseType,
            PayeeName = request.PayeeName,
            PayeeAccount = request.PayeeAccount,
            PayeeBank = request.PayeeBank,
            Remark = request.Remark,
            CreatedTime = now
        });

        // 返回创建的单据
        var result = await GetByIdAsync(id);
        return result!;
    }

    /// <summary>
    /// 更新草稿
    /// </summary>
    public async Task<ExpenseRequestDto?> UpdateAsync(long id, UpdateExpenseRequestRequest request, long userId)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // 检查单据状态
        var currentStatus = await connection.ExecuteScalarAsync<int>(
            "SELECT F单据状态 FROM [OA费用请款单] WHERE FID = @Id",
            new { Id = id });

        if (currentStatus != 0)
        {
            throw new InvalidOperationException("只有草稿状态的单据才能编辑");
        }

        // 检查是否是申请人本人
        var applicantId = await connection.ExecuteScalarAsync<long>(
            "SELECT F申请人ID FROM [OA费用请款单] WHERE FID = @Id",
            new { Id = id });

        if (applicantId != userId)
        {
            throw new InvalidOperationException("只能编辑自己创建的单据");
        }

        var now = DateTime.Now;
        var sql = @"
            UPDATE [OA费用请款单] SET
                F请款事由 = @Reason,
                F请款金额 = @Amount,
                F期望付款日期 = @ExpectedPayDate,
                F费用类型 = @ExpenseType,
                F收款方名称 = @PayeeName,
                F收款方账号 = @PayeeAccount,
                F收款方开户行 = @PayeeBank,
                F备注 = @Remark,
                F修改时间 = @ModifiedTime
            WHERE FID = @Id";

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Reason = request.Reason,
            Amount = request.Amount,
            ExpectedPayDate = request.ExpectedPayDate,
            ExpenseType = request.ExpenseType,
            PayeeName = request.PayeeName,
            PayeeAccount = request.PayeeAccount,
            PayeeBank = request.PayeeBank,
            Remark = request.Remark,
            ModifiedTime = now
        });

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// 删除草稿
    /// </summary>
    public async Task<bool> DeleteAsync(long id, long userId)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // 检查单据状态
        var currentStatus = await connection.ExecuteScalarAsync<int?>(
            "SELECT F单据状态 FROM [OA费用请款单] WHERE FID = @Id",
            new { Id = id });

        if (!currentStatus.HasValue)
        {
            return false;
        }

        if (currentStatus != 0)
        {
            throw new InvalidOperationException("只有草稿状态的单据才能删除");
        }

        // 检查是否是申请人本人
        var applicantId = await connection.ExecuteScalarAsync<long>(
            "SELECT F申请人ID FROM [OA费用请款单] WHERE FID = @Id",
            new { Id = id });

        if (applicantId != userId)
        {
            throw new InvalidOperationException("只能删除自己创建的单据");
        }

        var deleted = await connection.ExecuteAsync(
            "DELETE FROM [OA费用请款单] WHERE FID = @Id",
            new { Id = id });

        return deleted > 0;
    }

    /// <summary>
    /// 提交审批
    /// </summary>
    public async Task SubmitAsync(long id, long userId)
    {
        var connectionString = DbConnectionsHelper.GetSystemConnectionString(_configuration.GetValue<string>("Security:EncryptionKey"));
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var doc = await connection.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT F单据状态 AS DocStatus, F申请人ID AS ApplicantId 
              FROM [OA费用请款单] WHERE FID = @Id",
            new { Id = id });

        if (doc == null)
            throw new InvalidOperationException("费用请款单不存在");

        if ((int)doc.DocStatus != 0)
            throw new InvalidOperationException("只有草稿状态的单据才能提交");

        if ((long)doc.ApplicantId != userId)
            throw new InvalidOperationException("只能提交自己创建的单据");

        throw new NotSupportedException("BPM流程已废除，请通过 CardFlow 发起费用请款审批。");
    }

    #region 私有辅助方法

    /// <summary>
    /// 生成单据编号（规则：FYQK + yyyyMMdd + 3位序号）
    /// </summary>
    private async Task<string> GenerateDocNumberAsync(SqlConnection connection)
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"FYQK{today}";

        // 查询当天最大编号
        var maxNumber = await connection.ExecuteScalarAsync<string>(
            "SELECT MAX(F单据编号) FROM [OA费用请款单] WHERE F单据编号 LIKE @Prefix + '%'",
            new { Prefix = prefix });

        int sequence = 1;
        if (!string.IsNullOrEmpty(maxNumber) && maxNumber.Length >= prefix.Length + 3)
        {
            var sequenceStr = maxNumber.Substring(prefix.Length);
            if (int.TryParse(sequenceStr, out var maxSeq))
            {
                sequence = maxSeq + 1;
            }
        }

        return $"{prefix}{sequence:D3}";
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
        5 => "已引用",
        _ => "未知"
    };

    #endregion
}
