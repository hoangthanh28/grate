﻿using SqlServer.TestInfrastructure;
using TestCommon.TestInfrastructure;

namespace SqlServer.Running_MigrationScripts;

[Collection(nameof(SqlServerTestContainer))]
// ReSharper disable once InconsistentNaming
public class Environment_scripts : TestCommon.Generic.Running_MigrationScripts.Environment_scripts, IClassFixture<DependencyService>
{
    protected override IGrateTestContext Context { get; }

    protected override ITestOutputHelper TestOutput { get; }

    public Environment_scripts(SqlServerTestContainer testContainer, DependencyService dependencyService, ITestOutputHelper testOutput)
    {
        Context = new SqlServerGrateTestContext(dependencyService.ServiceProvider, testContainer);
        TestOutput = testOutput;
    }
}
