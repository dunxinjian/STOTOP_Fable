using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IExpenseAccountMappingService
{
    Task<List<ExpenseAccountMappingDto>> GetListAsync(long? expenseTypeId, long? orgId);
    Task<ExpenseAccountMappingDto> CreateAsync(CreateExpenseAccountMappingRequest request);
    Task<ExpenseAccountMappingDto?> UpdateAsync(long id, UpdateExpenseAccountMappingRequest request);
    Task<bool> DeleteAsync(long id);
}
