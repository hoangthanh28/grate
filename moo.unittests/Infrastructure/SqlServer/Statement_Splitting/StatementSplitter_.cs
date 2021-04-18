﻿using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using moo.Infrastructure;
using moo.Migration;
using NUnit.Framework;

namespace moo.unittests.Infrastructure.SqlServer.Statement_Splitting
{
    [TestFixture]
    // ReSharper disable once InconsistentNaming
    public class StatementSplitter_
    {
        private static readonly IDatabase Database = new SqlServerDatabase(NullLogger<SqlServerDatabase>.Instance);
        private static readonly StatementSplitter Splitter = new(Database.StatementSeparatorRegex);

        [Test]
        public void Splits_and_removes_GO_statements()
        {
            var original = @"
SELECT @@VERSION;


GO
SELECT 1
";
            var batches = Splitter.Split(original);

            batches.Should().HaveCount(2);
        }
        
    }
}