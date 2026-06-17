using STOTOP.Module.CardFlow.Services.Redaction;
using Xunit;

namespace STOTOP.Module.CardFlow.Tests.Redaction;

public class FieldMaskerTests
{
    [Theory]
    [InlineData("13800138000", "phone", "138****8000")]
    [InlineData("110101199003075678", "idCard", "1101**********5678")]
    [InlineData("6222021234567890123", "bankCard", "**** **** **** 0123")]
    [InlineData("zhangsan@corp.com", "email", "z***@corp.com")]
    [InlineData("张三丰", "name", "张**")]
    [InlineData("ABCDEFGH", null, "AB****GH")]
    [InlineData("ABC", null, "****")]
    public void Mask_AppliesPattern(string input, string? pattern, string expected)
    {
        Assert.Equal(expected, FieldMasker.Mask(input, pattern));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Mask_EmptyReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, FieldMasker.Mask(input, "phone"));
    }

    [Fact]
    public void Mask_ShortPhoneFallsBackToGeneric()
    {
        Assert.Equal("****", FieldMasker.Mask("123", "phone"));
    }
}
