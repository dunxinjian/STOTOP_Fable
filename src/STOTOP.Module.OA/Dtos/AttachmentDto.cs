namespace STOTOP.Module.OA.Dtos;

public class AttachmentDto
{
    public long Id { get; set; }
    public string BizDocType { get; set; } = string.Empty;
    public long BizDocId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public long UploaderId { get; set; }
    public string UploaderName { get; set; } = string.Empty;
    public DateTime UploadTime { get; set; }
}
