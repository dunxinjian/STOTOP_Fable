using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IExpenseTypeService
{
    Task<List<ExpenseTypeDto>> GetListAsync(long? orgId, string? scene);
    Task<ExpenseTypeDto> CreateAsync(CreateExpenseTypeRequest request);
    Task<ExpenseTypeDto?> UpdateAsync(long id, UpdateExpenseTypeRequest request);
    Task<bool> DeleteAsync(long id);
    Task<bool> ToggleAsync(long id);
}
