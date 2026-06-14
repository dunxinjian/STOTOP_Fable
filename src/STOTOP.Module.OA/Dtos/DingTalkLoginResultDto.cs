namespace STOTOP.Module.OA.Dtos;

public class DingTalkLoginRequest
{
    public string AuthCode { get; set; } = string.Empty;
    public long OrgId { get; set; }
}

public class DingTalkLoginResultDto
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserName { get; set; }
}
