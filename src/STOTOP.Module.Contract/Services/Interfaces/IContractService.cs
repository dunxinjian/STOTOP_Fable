using STOTOP.Core.Models;
using STOTOP.Module.Contract.Dtos;

namespace STOTOP.Module.Contract.Services.Interfaces;

public interface IContractService
{
    Task<PagedResult<ContractListItemDto>> GetContractsAsync(ContractQueryRequest request);
    Task<ContractDto?> GetContractByIdAsync(long id);
    Task<ContractDto> CreateContractAsync(CreateContractRequest request);
    Task<ContractDto?> UpdateContractAsync(long id, UpdateContractRequest request);
    Task<bool> DeleteContractAsync(long id);
    Task<bool> UpdateStatusAsync(long id, int status);
    Task<ContractDto> RenewContractAsync(long originalContractId, CreateContractRequest request);
}
