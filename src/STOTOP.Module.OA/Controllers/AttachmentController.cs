using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STOTOP.Core.Models;
using STOTOP.Module.OA.Dtos;
using STOTOP.Module.OA.Services.Interfaces;

namespace STOTOP.Module.OA.Controllers;

[Authorize]
[ApiController]
[Route("api/oa/attachment")]
public class AttachmentController : ControllerBase
{
    private readonly IAttachmentService _service;

    public AttachmentController(IAttachmentService service)
    {
        _service = service;
    }

    private long GetUserId() => long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    [HttpPost("upload")]
    public async Task<ApiResult<AttachmentDto>> Upload(
        [FromQuery] string docType,
        [FromQuery] long docId,
        IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return ApiResult<AttachmentDto>.Fail("请选择文件");
            }
            var result = await _service.UploadAsync(docType, docId, file, GetUserId());
            return ApiResult<AttachmentDto>.Success(result, "上传附件成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult<AttachmentDto>.Fail(ex.Message);
        }
    }

    [HttpGet("list")]
    public async Task<ApiResult<List<AttachmentDto>>> GetList(
        [FromQuery] string docType,
        [FromQuery] long docId)
    {
        var result = await _service.GetListAsync(docType, docId);
        return ApiResult<List<AttachmentDto>>.Success(result);
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> Download(long id)
    {
        var result = await _service.DownloadAsync(id);
        if (result == null)
        {
            return NotFound(ApiResult.Fail("附件不存在"));
        }
        return File(result.Value.fileBytes, result.Value.contentType, result.Value.fileName);
    }

    [HttpDelete("{id}")]
    public async Task<ApiResult> Delete(long id)
    {
        try
        {
            var result = await _service.DeleteAsync(id, GetUserId());
            if (!result)
            {
                return ApiResult.Fail("附件不存在");
            }
            return ApiResult.Ok("删除附件成功");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }
}
