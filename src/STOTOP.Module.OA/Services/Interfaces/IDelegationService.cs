using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IDelegationService
{
    Task<DelegationDto> CreateAsync(CreateDelegationRequest request);
    Task<List<DelegationDto>> GetMyDelegationsAsync(long userId);
    Task<bool> RevokeDelegationAsync(long delegationId, long userId);
    Task<long> ResolveActualApproverAsync(long originalApproverId, long orgId, string processType);
}
