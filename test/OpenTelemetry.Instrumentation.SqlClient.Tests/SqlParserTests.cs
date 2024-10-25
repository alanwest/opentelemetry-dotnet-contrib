// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using OpenTelemetry.Instrumentation.SqlClient.Implementation;
using Xunit;

namespace OpenTelemetry.Instrumentation.SqlClient.Tests;

public class SqlParserTests
{
    public static IEnumerable<object[]> TestData => SqlParserTestCases.GetTestCases();

    [Theory]
    [MemberData(nameof(TestData))]
    internal void SqlParserTest(SqlParserTestCases.TestCase testCase)
    {
        var parsed = SqlParser.GetParsedDatabaseStatement(CommandType.Text, testCase.Input);
        Assert.Equal(testCase.Operation, parsed.OperationName);
        Assert.Equal(testCase.Table, parsed.CollectionName);
    }
}
