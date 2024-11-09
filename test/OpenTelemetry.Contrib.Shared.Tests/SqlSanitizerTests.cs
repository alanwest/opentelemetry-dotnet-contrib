// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Xunit;

namespace OpenTelemetry.Instrumentation.Tests;

public class SqlSanitizerTests
{
    public static IEnumerable<object[]> TestData => SqlSanitizerTestCases.GetTestCases();

    [Theory]
    [MemberData(nameof(TestData))]
    internal void GetSanitizedSql(SqlSanitizerTestCases.TestCase testCase)
    {
        var sanitized = SqlSanitizer.GetSanitizedSql(testCase.Sql, testCase.SqlDialect);
        Assert.Contains(sanitized, testCase.Sanitized);
    }
}
