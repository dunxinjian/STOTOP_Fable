using Microsoft.Extensions.DependencyInjection;

namespace STOTOP.Module.CardFlow.Services.Handlers;

public class ClassificationHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _handlerMap = new(StringComparer.OrdinalIgnoreCase);

    public ClassificationHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>注册Handler类型映射</summary>
    public void Register<T>(string handlerType) where T : IClassificationHandler
    {
        _handlerMap[handlerType] = typeof(T);
    }

    /// <summary>根据handlerType创建Handler实例</summary>
    public IClassificationHandler? Create(string handlerType)
    {
        if (!_handlerMap.TryGetValue(handlerType, out var type))
            return null;

        return _serviceProvider.CreateScope().ServiceProvider.GetService(type) as IClassificationHandler;
    }

    /// <summary>获取所有已注册的Handler类型列表</summary>
    public List<string> GetRegisteredTypes()
    {
        return _handlerMap.Keys.ToList();
    }
}
