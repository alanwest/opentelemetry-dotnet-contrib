// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Data;

namespace OpenTelemetry.Instrumentation.SqlClient.Tests;

public class TestCase
{
    public string Name { get; set; } = string.Empty;

    public CommandType CommandType { get; set; } = CommandType.Text;

    public string SqlStatement { get; set; } = string.Empty;

    public bool IsFailure { get; set; }

    public string ExpectedDbName { get; set; } = string.Empty;

    public override string ToString()
    {
        // This is used by Visual Studio's test runner to identify the test case.
        return $"{this.Name}";
    }
}
