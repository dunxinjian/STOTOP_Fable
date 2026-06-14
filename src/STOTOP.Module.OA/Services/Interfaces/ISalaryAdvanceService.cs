using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface ISalaryAdvanceService
{
    Task<PagedResult<SalaryAdvanceDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<SalaryAdvanceDto?> GetByIdAsync(long id);
    Task<SalaryAdvanceDto> CreateAsync(CreateSalaryAdvanceRequest request, long userId);
    Task<SalaryAdvanceDto?> UpdateAsync(long id, UpdateSalaryAdvanceRequest request, long userId);
    Task<bool> DeleteAsync(long id, long userId);
    Task SubmitAsync(long id, long userId);
}
