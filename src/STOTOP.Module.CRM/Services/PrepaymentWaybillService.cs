using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Interfaces;
using STOTOP.Core.Models;
using STOTOP.Module.CRM.Dtos;
using STOTOP.Module.CRM.Entities;
using STOTOP.Module.CRM.Services.Interfaces;

namespace STOTOP.Module.CRM.Services;

public class PrepaymentWaybillService : IPrepaymentWaybillService
{
    private readonly IRepository<CrmWaybillPool> _poolRepo;
    private readonly IRepository<CrmCustomerAccount> _accountRepo;
    private readonly IRepository<CrmPrepayment> _prepaymentRepo;
    private readonly IRepository<CrmWaybillAllocation> _allocationRepo;

    private const int MaxRetryCount = 3;

    public PrepaymentWaybillService(
        IRepository<CrmWaybillPool> poolRepo,
        IRepository<CrmCustomerAccount> accountRepo,
        IRepository<CrmPrepayment> prepaymentRepo,
        IRepository<CrmWaybillAllocation> allocationRepo)
    {
        _poolRepo = poolRepo;
        _accountRepo = accountRepo;
        _prepaymentRepo = prepaymentRepo;
        _allocationRepo = allocationRepo;
    }

    #region Waybill Pool

    public async Task<PagedResult<WaybillPoolDto>> GetWaybillPoolsAsync(WaybillPoolQueryRequest request)
    {
        var query = _poolRepo.Query();

        if (!string.IsNullOrWhiteSpace(request.BrandCode))
            query = query.Where(p => p.FBrandCode == request.BrandCode);
        if (request.Status.HasValue)
            query = query.Where(p => p.FStatus == request.Status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.FCreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<WaybillPoolDto>
        {
            Items = items.Select(MapPoolToDto).ToList(),
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }

    public async Task<WaybillPoolDto?> GetWaybillPoolByIdAsync(long id)
    {
        var entity = await _poolRepo.Query().FirstOrDefaultAsync(p => p.FID == id);
        return entity == null ? null : MapPoolToDto(entity);
    }

    public async Task<WaybillPoolDto> CreateWaybillPoolAsync(CreateWaybillPoolRequest request)
    {
        var entity = new CrmWaybillPool
        {
            FBrandCode = request.BrandCode,
            FPrefix = request.Prefix,
            FStartNo = request.StartNo,
            FEndNo = request.EndNo,
            FTotalCount = request.TotalCount,
            FAllocatedCount = 0,
            FRemainingCount = request.TotalCount,
            FPurchaseDate = request.PurchaseDate,
            FUnitPrice = request.UnitPrice,
            FVersion = 0,
            FStatus = 0,
            FCreatedTime = DateTime.Now
        };

        await _poolRepo.AddAsync(entity);
        return MapPoolToDto(entity);
    }

    public async Task<bool> DeleteWaybillPoolAsync(long id)
    {
        var entity = await _poolRepo.GetByIdAsync(id);
        if (entity == null) return false;
        await _poolRepo.DeleteAsync(id);
        return true;
    }

    #endregion

    #region Customer Account

    public async Task<CustomerAccountDto?> GetCustomerAccountAsync(string customerId, string brandCode)
    {
        var entity = await _accountRepo.Query()
            .FirstOrDefaultAsync(a => a.FCustomerId == customerId && a.FBrandCode == brandCode);
        return entity == null ? null : MapAccountToDto(entity);
    }

    public async Task<CustomerAccountDto> RechargeAccountAsync(long accountId, decimal amount)
    {
        var entity = await _accountRepo.Query().AsTracking()
            .FirstOrDefaultAsync(a => a.FID == accountId)
            ?? throw new InvalidOperationException("客户账户不存在");

        entity.FBalance += amount;
        entity.FTotalRecharge += amount;
        entity.FUpdatedTime = DateTime.Now;
        await _accountRepo.UpdateAsync(entity);
        return MapAccountToDto(entity);
    }

    public async Task<CustomerAccountDto> DeductAccountAsync(long accountId, decimal amount)
    {
        var entity = await _accountRepo.Query().AsTracking()
            .FirstOrDefaultAsync(a => a.FID == accountId)
            ?? throw new InvalidOperationException("客户账户不存在");

        if (entity.FBalance < amount)
            throw new InvalidOperationException("账户余额不足");

        entity.FBalance -= amount;
        entity.FTotalConsumption += amount;
        entity.FUpdatedTime = DateTime.Now;
        await _accountRepo.UpdateAsync(entity);
        return MapAccountToDto(entity);
    }

    #endregion

    #region Prepayment

    public async Task<PagedResult<PrepaymentDto>> GetPrepaymentsAsync(PrepaymentQueryRequest request)
    {
        var query = _prepaymentRepo.Query();

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            query = query.Where(p => p.FCustomerId == request.CustomerId);
        if (!string.IsNullOrWhiteSpace(request.BrandCode))
            query = query.Where(p => p.FBrandCode == request.BrandCode);
        if (request.Status.HasValue)
            query = query.Where(p => p.FStatus == request.Status.Value);
        if (request.StartDate.HasValue)
            query = query.Where(p => p.FCreatedTime >= request.StartDate.Value);
        if (request.EndDate.HasValue)
            query = query.Where(p => p.FCreatedTime <= request.EndDate.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.FCreatedTime)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<PrepaymentDto>
        {
            Items = items.Select(MapPrepaymentToDto).ToList(),
            Total = total,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }

    public async Task<PrepaymentDto?> GetPrepaymentByIdAsync(long id)
    {
        var entity = await _prepaymentRepo.Query().FirstOrDefaultAsync(p => p.FID == id);
        return entity == null ? null : MapPrepaymentToDto(entity);
    }

    public async Task<PrepaymentDto> CreatePrepaymentAsync(CreatePrepaymentRequest request)
    {
        var entity = new CrmPrepayment
        {
            FCustomerId = request.CustomerId,
            FCustomerAccountId = request.CustomerAccountId,
            FOrgId = request.OrgId ?? 0,
            FBrandCode = request.BrandCode,
            FPrepayAmount = request.PrepayAmount,
            FReceivedAmount = 0,
            FExpectedWaybillCount = request.ExpectedWaybillCount,
            FAllocatedWaybillCount = 0,
            FStatus = 0, // 待到账
            FRemark = request.Remark,
            FCreatedTime = DateTime.Now
        };

        await _prepaymentRepo.AddAsync(entity);
        return MapPrepaymentToDto(entity);
    }

    public async Task<bool> ConfirmPrepaymentReceivedAsync(long id, decimal receivedAmount, long? bankTransactionId)
    {
        var entity = await _prepaymentRepo.Query().AsTracking()
            .FirstOrDefaultAsync(p => p.FID == id);
        if (entity == null) return false;

        entity.FReceivedAmount = receivedAmount;
        entity.FBankTransactionId = bankTransactionId;
        entity.FStatus = 1; // 已到账
        entity.FUpdatedTime = DateTime.Now;
        await _prepaymentRepo.UpdateAsync(entity);

        // 到账时充值账户余额
        var account = await _accountRepo.Query().AsTracking()
            .FirstOrDefaultAsync(a => a.FID == entity.FCustomerAccountId);
        if (account != null)
        {
            account.FBalance += receivedAmount;
            account.FTotalRecharge += receivedAmount;
            account.FUpdatedTime = DateTime.Now;
            await _accountRepo.UpdateAsync(account);
        }

        return true;
    }

    #endregion

    #region Waybill Allocation

    public async Task<WaybillAllocationDto> AllocateWaybillAsync(AllocateWaybillRequest request)
    {
        for (int retry = 0; retry < MaxRetryCount; retry++)
        {
            try
            {
                var pool = await _poolRepo.Query().AsTracking()
                    .FirstOrDefaultAsync(p => p.FID == request.PoolId)
                    ?? throw new InvalidOperationException("号段池不存在");

                if (pool.FRemainingCount < request.Count)
                    throw new InvalidOperationException($"号段池剩余数量不足，剩余 {pool.FRemainingCount}，请求 {request.Count}");

                // 计算本次分配的起止号
                long poolStart = long.Parse(pool.FStartNo);
                long allocStart = poolStart + pool.FAllocatedCount;
                long allocEnd = allocStart + request.Count - 1;

                var allocation = new CrmWaybillAllocation
                {
                    FPrepaymentId = request.PrepaymentId,
                    FCustomerId = request.CustomerId,
                    FPoolId = request.PoolId,
                    FStartNo = (pool.FPrefix ?? "") + allocStart.ToString(),
                    FEndNo = (pool.FPrefix ?? "") + allocEnd.ToString(),
                    FAllocatedCount = request.Count,
                    FAllocationDate = DateOnly.FromDateTime(DateTime.Now),
                    FOperatorId = request.OperatorId,
                    FStatus = 1, // 已分配
                    FCreatedTime = DateTime.Now
                };

                // 更新号段池（乐观锁由 EF Core ConcurrencyToken on FVersion 保护）
                pool.FAllocatedCount += request.Count;
                pool.FRemainingCount -= request.Count;
                pool.FUpdatedTime = DateTime.Now;

                await _allocationRepo.AddAsync(allocation);
                await _poolRepo.UpdateAsync(pool);

                // 更新预付款的已分配运单数
                var prepayment = await _prepaymentRepo.Query().AsTracking()
                    .FirstOrDefaultAsync(p => p.FID == request.PrepaymentId);
                if (prepayment != null)
                {
                    prepayment.FAllocatedWaybillCount += request.Count;
                    prepayment.FUpdatedTime = DateTime.Now;
                    await _prepaymentRepo.UpdateAsync(prepayment);
                }

                return MapAllocationToDto(allocation);
            }
            catch (DbUpdateConcurrencyException) when (retry < MaxRetryCount - 1)
            {
                // 乐观锁冲突，重试
                continue;
            }
        }

        throw new InvalidOperationException("号段分配失败，请重试");
    }

    public async Task<bool> RecycleWaybillAsync(long allocationId)
    {
        for (int retry = 0; retry < MaxRetryCount; retry++)
        {
            try
            {
                var allocation = await _allocationRepo.Query().AsTracking()
                    .FirstOrDefaultAsync(a => a.FID == allocationId);
                if (allocation == null) return false;

                if (allocation.FStatus != 1)
                    throw new InvalidOperationException("只能回收已分配状态的运单号");

                allocation.FStatus = 2; // 已回收
                allocation.FUpdatedTime = DateTime.Now;
                await _allocationRepo.UpdateAsync(allocation);

                // 恢复号段池
                var pool = await _poolRepo.Query().AsTracking()
                    .FirstOrDefaultAsync(p => p.FID == allocation.FPoolId);
                if (pool != null)
                {
                    pool.FAllocatedCount -= allocation.FAllocatedCount;
                    pool.FRemainingCount += allocation.FAllocatedCount;
                    pool.FUpdatedTime = DateTime.Now;
                    await _poolRepo.UpdateAsync(pool);
                }

                return true;
            }
            catch (DbUpdateConcurrencyException) when (retry < MaxRetryCount - 1)
            {
                continue;
            }
        }

        throw new InvalidOperationException("运单回收失败，请重试");
    }

    public async Task<List<WaybillAllocationDto>> GetAllocationsByCustomerAsync(string customerId)
    {
        var items = await _allocationRepo.Query()
            .Where(a => a.FCustomerId == customerId)
            .OrderByDescending(a => a.FCreatedTime)
            .ToListAsync();
        return items.Select(MapAllocationToDto).ToList();
    }

    public async Task<List<WaybillAllocationDto>> GetAllocationsByPoolAsync(long poolId)
    {
        var items = await _allocationRepo.Query()
            .Where(a => a.FPoolId == poolId)
            .OrderByDescending(a => a.FCreatedTime)
            .ToListAsync();
        return items.Select(MapAllocationToDto).ToList();
    }

    #endregion

    #region Mapping

    private static WaybillPoolDto MapPoolToDto(CrmWaybillPool e) => new()
    {
        Id = e.FID,
        BrandCode = e.FBrandCode,
        Prefix = e.FPrefix,
        StartNo = e.FStartNo,
        EndNo = e.FEndNo,
        TotalCount = e.FTotalCount,
        AllocatedCount = e.FAllocatedCount,
        RemainingCount = e.FRemainingCount,
        PurchaseDate = e.FPurchaseDate,
        UnitPrice = e.FUnitPrice,
        Status = e.FStatus,
        CreatorName = e.FCreatorName,
        CreatedTime = e.FCreatedTime
    };

    private static CustomerAccountDto MapAccountToDto(CrmCustomerAccount e) => new()
    {
        Id = e.FID,
        CustomerId = e.FCustomerId,
        BrandCode = e.FBrandCode,
        Balance = e.FBalance,
        TotalRecharge = e.FTotalRecharge,
        TotalConsumption = e.FTotalConsumption,
        FrozenAmount = e.FFrozenAmount,
        CreatorName = e.FCreatorName,
        CreatedTime = e.FCreatedTime
    };

    private static PrepaymentDto MapPrepaymentToDto(CrmPrepayment e) => new()
    {
        Id = e.FID,
        CustomerId = e.FCustomerId,
        CustomerAccountId = e.FCustomerAccountId,
        OrgId = e.FOrgId,
        BrandCode = e.FBrandCode,
        PrepayAmount = e.FPrepayAmount,
        ReceivedAmount = e.FReceivedAmount,
        ExpectedWaybillCount = e.FExpectedWaybillCount,
        AllocatedWaybillCount = e.FAllocatedWaybillCount,
        Status = e.FStatus,
        BankTransactionId = e.FBankTransactionId,
        Remark = e.FRemark,
        CreatorName = e.FCreatorName,
        CreatedTime = e.FCreatedTime
    };

    private static WaybillAllocationDto MapAllocationToDto(CrmWaybillAllocation e) => new()
    {
        Id = e.FID,
        PrepaymentId = e.FPrepaymentId,
        CustomerId = e.FCustomerId,
        PoolId = e.FPoolId,
        StartNo = e.FStartNo,
        EndNo = e.FEndNo,
        AllocatedCount = e.FAllocatedCount,
        AllocationDate = e.FAllocationDate,
        OperatorId = e.FOperatorId,
        Status = e.FStatus,
        CreatorName = e.FCreatorName,
        CreatedTime = e.FCreatedTime
    };

    #endregion
}
