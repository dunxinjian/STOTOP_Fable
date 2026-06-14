using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace STOTOP.Module.CardFlow.AutoPlugin;

/// <summary>
/// 自动插件工厂（按编码注册和创建插件实例）
/// </summary>
public class AutoPluginFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _registry = new(StringComparer.OrdinalIgnoreCase);

    public AutoPluginFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Register<T>(string code) where T : IAutoPlugin
    {
        _registry[code] = typeof(T);
    }

    public IAutoPlugin Create(string code)
    {
        if (!_registry.TryGetValue(code, out var type))
            throw new InvalidOperationException($"未注册的插件编码: {code}");

        return (IAutoPlugin)_serviceProvider.GetRequiredService(type);
    }

    /// <summary>
    /// 使用上下文中的 Scoped provider 创建插件实例，解决主 factory 为 Singleton 不能解析 Scoped 服务的问题
    /// </summary>
    public IAutoPlugin Create(string code, IServiceProvider scopedProvider)
    {
        if (!_registry.TryGetValue(code, out var type))
            throw new InvalidOperationException($"未注册的插件编码: {code}");

        return (IAutoPlugin)scopedProvider.GetRequiredService(type);
    }

    public bool IsRegistered(string code) => _registry.ContainsKey(code);

    public IReadOnlyDictionary<string, Type> GetAllRegistrations() => _registry;

    public List<PluginMetadata> GetAllMetadata()
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        return _registry.Select(kv =>
        {
            var plugin = (IAutoPlugin)sp.GetRequiredService(kv.Value);
            return plugin.GetMetadata();
        }).ToList();
    }
}
