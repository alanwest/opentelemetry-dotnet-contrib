// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Instrumentation.SqlClient.Implementation;
using Xunit;

namespace OpenTelemetry.Instrumentation.SqlClient.Tests;

public class SqlObfuscatorTests
{
    public static IEnumerable<object[]> TestData => SqlObfuscatorTestCases.GetTestCases();

    [Theory]
    [MemberData(nameof(TestData))]
    internal void Test(SqlObfuscatorTestCases.TestCase testCase)
    {
        var obfuscated = SqlSanitizer.GetObfuscatedSql(testCase.Sql, SqlDialect.MsSql);

        if (testCase.Dialects.Contains("mssql"))
        {
            Assert.Contains(obfuscated, testCase.Obfuscated);
        }
    }
}
