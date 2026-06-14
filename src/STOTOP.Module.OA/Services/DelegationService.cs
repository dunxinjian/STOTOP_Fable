using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using STOTOP.Core.Interfaces;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Entities;
using STOTOP.Module.OA.Services.Interfaces;
using STOTOP.Module.System.Entities;

namespace STOTOP.Module.OA.Services;

public class DelegationService : IDelegationService
{
    private readonly IRepository<OaDelegation> _delegationRepository;
    private readonly IRepository<SysUser> _userRepository;
    private readonly IRepository<SysOrganization> _orgRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DelegationService(
        IRepository<OaDelegation> delegationRepository,
        IRepository<SysUser> userRepository,
        IRepository<SysOrganization> orgRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _delegationRepository = delegationRepository;
        _userRepository = userRepository;
        _orgRepository = orgRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    private long GetCurrentUserId()
    {
        var userIdObj = _httpContextAccessor.HttpContext?.Items["CurrentUserId"];
        if (userIdObj is long userId) return userId;
        return 0;
    }

    public async Task<DelegationDto> CreateAsync(CreateDelegationRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == 0)
            throw new InvalidOperationException("无法获取当前用户信息");

        if (request.DelegateeId == currentUserId)
            throw new InvalidOperationException("不能委托给自己");

        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("结束日期不能早于开始日期");

        // 检查是否已有同类型的有效委托
        var existing = await _delegationRepository.Query()
            .Where(d => d.FDelegatorId == currentUserId
                && d.FOrgId == request.OrgId
                && d.FStatus == 1
                && d.FEndDate >= DateOnly.FromDateTime(DateTime.Today))
            .Where(d => d.FProcessType == request.ProcessType || d.FProcessType == null)
            .FirstOrDefaultAsync();

        if (existing != null)
            throw new InvalidOperationException("已存在相同类型的有效委托");

        var delegation = new OaDelegation
        {
            FDelegatorId = currentUserId,
            FDelegateeId = request.DelegateeId,
            FOrgId = request.OrgId,
            FProcessType = request.ProcessType,
            FStartDate = request.StartDate,
            FEndDate = request.EndDate,
            FReason = request.Reason,
            FStatus = 1, // 生效
            FCreatedTime = DateTime.Now
        };

        await _delegationRepository.AddAsync(delegation);
        return await MapToDto(delegation);
    }

    public async Task<List<DelegationDto>> GetMyDelegationsAsync(long userId)
    {
        var delegations = await _delegationRepository.Query()
            .Where(d => d.FDelegatorId == userId)
            .OrderByDescending(d => d.FCreatedTime)
            .ToListAsync();

        var result = new List<DelegationDto>();
        foreach (var d in delegations)
        {
            result.Add(await MapToDto(d));
        }
        return result;
    }

    public async Task<bool> RevokeDelegationAsync(long delegationId, long userId)
    {
        var delegation = await _delegationRepository.GetByIdAsync(delegationId);
        if (delegation == null)
            return false;

        if (delegation.FDelegatorId != userId)
            throw new InvalidOperationException("您无权撤销此委托");

        if (delegation.FStatus != 1)
            throw new InvalidOperationException("该委托已失效，无需撤销");

        delegation.FStatus = 0; // 已撤销
        await _delegationRepository.UpdateAsync(delegation);
        return true;
    }

    public async Task<long> ResolveActualApproverAsync(long originalApproverId, long orgId, string processType)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 查找精确匹配流程类型的委托
        var delegation = await _delegationRepository.Query()
            .Where(d => d.FDelegatorId == originalApproverId
                && d.FOrgId == orgId
                && d.FStatus == 1
                && d.FStartDate <= today
                && d.FEndDate >= today
                && d.FProcessType == processType)
            .FirstOrDefaultAsync();

        // 如果没有精确匹配，查找全局委托（ProcessType 为 null 表示所有类型）
        if (delegation == null)
        {
            delegation = await _delegationRepository.Query()
                .Where(d => d.FDelegatorId == originalApproverId
                    && d.FOrgId == orgId
                    && d.FStatus == 1
                    && d.FStartDate <= today
                    && d.FEndDate >= today
                    && d.FProcessType == null)
                .FirstOrDefaultAsync();
        }

        return delegation?.FDelegateeId ?? originalApproverId;
    }

    private async Task<DelegationDto> MapToDto(OaDelegation delegation)
    {
        var delegator = await _userRepository.GetByIdAsync(delegation.FDelegatorId);
        var delegatee = await _userRepository.GetByIdAsync(delegation.FDelegateeId);
        var org = await _orgRepository.GetByIdAsync(delegation.FOrgId);

        return new DelegationDto
        {
            Id = delegation.FID,
            DelegatorId = delegation.FDelegatorId,
            DelegatorName = delegator?.FName ?? string.Empty,
            DelegateeId = delegation.FDelegateeId,
            DelegateeName = delegatee?.FName ?? string.Empty,
            OrgId = delegation.FOrgId,
            OrgName = org?.FName ?? string.Empty,
            ProcessType = delegation.FProcessType,
            ProcessTypeName = delegation.FProcessType != null
                ? BizDocTypeHelper.GetDisplayName(delegation.FProcessType)
                : "全部类型",
            StartDate = delegation.FStartDate,
            EndDate = delegation.FEndDate,
            Reason = delegation.FReason,
            Status = delegation.FStatus,
            CreatedTime = delegation.FCreatedTime
        };
    }
}
