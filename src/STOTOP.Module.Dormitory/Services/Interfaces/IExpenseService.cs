using STOTOP.Core.Models;
using STOTOP.Module.Dormitory.Dtos;

namespace STOTOP.Module.Dormitory.Services.Interfaces;

public interface IExpenseService
{
    Task<PagedResult<ExpenseListItemDto>> GetExpensesAsync(ExpenseQueryRequest request);
    Task<ExpenseDto?> GetExpenseByIdAsync(long id);
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request);
    Task<ExpenseDto?> UpdateExpenseAsync(long id, UpdateExpenseRequest request);
    Task<bool> DeleteExpenseAsync(long id);
    Task<List<MonthlyExpenseSummaryDto>> GetMonthlySummaryAsync(string? month);
}
