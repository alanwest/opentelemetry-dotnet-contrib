// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTelemetry.Instrumentation.SqlClient.Tests;

public static class SqlTestCases
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IEnumerable<object[]> GetTestCases()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var input = JsonSerializer.Deserialize<TestCase[]>(
            assembly.GetManifestResourceStream("tests.json")!,
            JsonSerializerOptions);
        return GetArgumentsFromTestCaseObject(input!);
    }

    private static List<object[]> GetArgumentsFromTestCaseObject(IEnumerable<TestCase> input)
    {
        var result = new List<object[]>();

        foreach (var testCase in input)
        {
            result.Add(new object[] { testCase });
        }

        return result;
    }
}
