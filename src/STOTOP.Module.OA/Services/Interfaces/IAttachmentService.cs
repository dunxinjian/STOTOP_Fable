using Microsoft.AspNetCore.Http;
using STOTOP.Module.OA.Dtos;

namespace STOTOP.Module.OA.Services.Interfaces;

public interface IAttachmentService
{
    Task<AttachmentDto> UploadAsync(string docType, long docId, IFormFile file, long uploaderId);
    Task<List<AttachmentDto>> GetListAsync(string docType, long docId);
    Task<(byte[] fileBytes, string fileName, string contentType)?> DownloadAsync(long id);
    Task<bool> DeleteAsync(long id, long userId);
}
