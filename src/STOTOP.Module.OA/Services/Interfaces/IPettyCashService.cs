using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IPettyCashService
{
    // 备用金申请
    Task<PagedResult<PettyCashApplyDto>> GetApplyListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<PettyCashApplyDto?> GetApplyByIdAsync(long id);
    Task<PettyCashApplyDto> CreateApplyAsync(CreatePettyCashApplyRequest request, long userId);
    Task<PettyCashApplyDto?> UpdateApplyAsync(long id, UpdatePettyCashApplyRequest request, long userId);
    Task SubmitApplyAsync(long id, long userId);

    // 备用金报销
    Task<PagedResult<PettyCashReimburseDto>> GetReimburseListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<PettyCashReimburseDto> CreateReimburseAsync(CreatePettyCashReimburseRequest request, long userId);
    Task SubmitReimburseAsync(long id, long userId);

    // 备用金还款
    Task<PagedResult<PettyCashReturnDto>> GetReturnListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<PettyCashReturnDto> CreateReturnAsync(CreatePettyCashReturnRequest request, long userId);
    Task SubmitReturnAsync(long id, long userId);

    // 备用金冲销
    Task<PagedResult<PettyCashWriteOffDto>> GetWriteOffListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<PettyCashWriteOffDto> CreateWriteOffAsync(CreatePettyCashWriteOffRequest request, long userId);
    Task SubmitWriteOffAsync(long id, long userId);

    // 备用金台账
    Task<List<PettyCashLedgerDto>> GetLedgerAsync(long? orgId, long? applicantId);
}
