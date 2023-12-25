﻿using grate;
using grate.MariaDb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCommon.TestInfrastructure;
namespace MariaDB.TestInfrastructure;
public class DependencyService
{
    public IServiceProvider ServiceProvider { get; }
    public DependencyService()
    {
        ServiceProvider = new ServiceCollection()
            .AddLogging(opt =>
            {
                opt.AddConsole();
                opt.SetMinimumLevel(TestConfig.GetLogLevel());
            })
            .AddGrate(cfg =>
            {
                cfg.UseMariaDb();
            })
            .AddSingleton<IDatabaseConnectionFactory, MariaDbConnectionFactory>()
            .BuildServiceProvider();
    }
}
