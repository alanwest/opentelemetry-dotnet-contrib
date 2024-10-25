// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTelemetry.Instrumentation.SqlClient.Tests;

internal static class SqlObfuscatorTestCases
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IEnumerable<object[]> GetTestCases()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var input = JsonSerializer.Deserialize<TestCase[]>(
            assembly.GetManifestResourceStream("SqlObfuscatorTestCases.json")!,
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

    public class TestCase
    {
        public string Name { get; set; } = string.Empty;

        public string Sql { get; set; } = string.Empty;

        public IEnumerable<string> Obfuscated { get; set; } = [];

        public IEnumerable<string> Dialects { get; set; } = [];

        public override string ToString()
        {
            // This is used by Visual Studio's test runner to identify the test case.
            return $"{this.Name}";
        }
    }
}
