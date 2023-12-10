using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestCommon.TestInfrastructure;
public class SimpleService
{
    public IServiceProvider ServiceProvider { get; }
    public SimpleService()
    {
        var s_logLevel = Environment.GetEnvironmentVariable("Logging__LogLevel__Default");
        ServiceProvider = new ServiceCollection()
            .AddLogging(opt =>
            {
                opt.AddConsole();
                if (Enum.TryParse(typeof(LogLevel), s_logLevel, out var logLevel))
                {
                    opt.SetMinimumLevel((LogLevel)logLevel);
                }
                else
                {
                    opt.SetMinimumLevel(LogLevel.None);
                }
            })
            .BuildServiceProvider();
    }
}
