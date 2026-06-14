using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface ILoanApplyService
{
    Task<PagedResult<LoanApplyDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<LoanApplyDto?> GetByIdAsync(long id);
    Task<LoanApplyDto> CreateAsync(CreateLoanApplyRequest request, long userId);
    Task<LoanApplyDto?> UpdateAsync(long id, UpdateLoanApplyRequest request, long userId);
    Task<bool> DeleteAsync(long id, long userId);
    Task SubmitAsync(long id, long userId);
    Task<List<LoanLedgerDto>> GetLedgerAsync(long? orgId, long? applicantId);
}
