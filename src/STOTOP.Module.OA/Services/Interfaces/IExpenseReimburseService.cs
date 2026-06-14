using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IExpenseReimburseService
{
    Task<PagedResult<ExpenseReimburseDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<ExpenseReimburseDto?> GetByIdAsync(long id);
    Task<ExpenseReimburseDto> CreateAsync(CreateExpenseReimburseRequest request, long userId);
    Task<ExpenseReimburseDto?> UpdateAsync(long id, UpdateExpenseReimburseRequest request, long userId);
    Task<bool> DeleteAsync(long id, long userId);
    Task SubmitAsync(long id, long userId);
    Task<List<AvailableRequestDto>> GetAvailableRequestsAsync(long orgId, long userId);
    Task<List<AvailableLoanDto>> GetAvailableLoansAsync(long orgId, long userId);
}
