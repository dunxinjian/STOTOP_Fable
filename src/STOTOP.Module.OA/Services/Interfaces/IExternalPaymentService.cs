using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IExternalPaymentService
{
    Task<PagedResult<ExternalPaymentDto>> GetPagedListAsync(long userId, int page, int pageSize, int? status, long? orgId);
    Task<ExternalPaymentDto?> GetByIdAsync(long id);
    Task<ExternalPaymentDto> CreateAsync(CreateExternalPaymentRequest request, long userId);
    Task<ExternalPaymentDto?> UpdateAsync(long id, UpdateExternalPaymentRequest request, long userId);
    Task<bool> DeleteAsync(long id, long userId);
    Task SubmitAsync(long id, long userId);
}
