using Microsoft.AspNetCore.Http;
using STOTOP.Infrastructure.Data;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Services;

public class AttachmentService : IAttachmentService
{
    private readonly STOTOPDbContext _db;

    public AttachmentService(STOTOPDbContext db)
    {
        _db = db;
    }

    public Task<AttachmentDto> UploadAsync(string docType, long docId, IFormFile file, long uploaderId)
        => throw new NotImplementedException();

    public Task<List<AttachmentDto>> GetListAsync(string docType, long docId)
        => throw new NotImplementedException();

    public Task<(byte[] fileBytes, string fileName, string contentType)?> DownloadAsync(long id)
        => throw new NotImplementedException();

    public Task<bool> DeleteAsync(long id, long userId)
        => throw new NotImplementedException();
}
