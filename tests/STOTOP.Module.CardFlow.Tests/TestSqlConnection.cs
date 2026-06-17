using STOTOP.Module.System.Services;

namespace STOTOP.Module.CardFlow.Tests;

/// <summary>
/// 集成测试连接 helper。
/// 优先用环境变量 STOTOP_TEST_CONNECTION；否则从主树 src/STOTOP.WebAPI/db-connections.json
/// 现场解密系统连接（默认密钥）拼出连接串。任何明文密码都不入库——本类不含明文。
///
/// 另外提供 EnsureSystemConnectionFile：把主树的 db-connections.json 复制到测试 BaseDirectory，
/// 使 ExcelInputPlugin 内部的 DbConnectionsHelper.GetSystemConnectionString()（读 BaseDirectory）
/// 也能解析到同一个 stotop 库——这是真导入走 SqlBulkCopy 的前提。
/// </summary>
public static class TestSqlConnection
{
    // 主树 db-connections.json 路径（系统连接已用默认密钥加密，指向 stotop 开发测试库）
    private const string MainTreeConnFile =
        @"E:\STOTOP_Fable\src\STOTOP.WebAPI\db-connections.json";

    private static string? _cached;
    private static bool _resolved;

    /// <summary>
    /// 返回可用连接串；无法获取时返回 null（集成测试据此 Skip）。
    /// </summary>
    public static string? GetConnectionString()
    {
        if (_resolved) return _cached;
        _resolved = true;

        // 1. 环境变量优先
        var env = Environment.GetEnvironmentVariable("STOTOP_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(env))
        {
            _cached = env;
            return _cached;
        }

        // 2. 从主树 db-connections.json 现场解密系统连接
        try
        {
            if (!File.Exists(MainTreeConnFile)) return null;
            EnsureSystemConnectionFile();
            // 复制到 BaseDirectory 后，DbConnectionsHelper 即可解析系统连接串
            _cached = DbConnectionsHelper.GetSystemConnectionString();
        }
        catch
        {
            _cached = null;
        }

        return _cached;
    }

    /// <summary>是否具备连接能力（可达性由实际打开连接验证）。</summary>
    public static bool IsAvailable => !string.IsNullOrWhiteSpace(GetConnectionString());

    /// <summary>
    /// 把主树 db-connections.json 复制到测试 BaseDirectory（若尚不存在/内容不同）。
    /// 这样 ExcelInputPlugin 通过 DbConnectionsHelper（读 BaseDirectory）能解析到 stotop。
    /// </summary>
    public static void EnsureSystemConnectionFile()
    {
        var target = DbConnectionsHelper.GetFilePath();
        if (!File.Exists(MainTreeConnFile)) return;

        var src = File.ReadAllText(MainTreeConnFile);
        if (File.Exists(target) && File.ReadAllText(target) == src) return;

        File.WriteAllText(target, src);
    }
}
