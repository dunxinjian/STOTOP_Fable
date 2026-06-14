using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IExpenseRequestService
{
    Task<PagedResult<ExpenseRequestDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<ExpenseRequestDto?> GetByIdAsync(long id);
    Task<ExpenseRequestDto> CreateAsync(CreateExpenseRequestRequest request, long userId);
    Task<ExpenseRequestDto?> UpdateAsync(long id, UpdateExpenseRequestRequest request, long userId);
    Task<bool> DeleteAsync(long id, long userId);
    Task SubmitAsync(long id, long userId);
}
