// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;

namespace OpenTelemetry.Instrumentation.SqlClient.Implementation;

/// <summary>
/// This class is responsible for masking potentially sensitive parameters in SQL (and SQL-like)
/// statements and queries.
/// </summary>
internal sealed class SqlProcessor
{
    private const SqlDialect Dialect = SqlDialect.MsSql;

    private readonly bool sanitizeSql;
    private readonly bool parseSql;

    public SqlProcessor(bool sanitizeSql, bool parseSql)
    {
        this.sanitizeSql = sanitizeSql;
        this.parseSql = parseSql;
    }

    public (string? QueryText, string? OperationName, string? CollectionName) Parse(CommandType commandType, string statement)
    {
        if (string.IsNullOrEmpty(statement))
        {
            return (QueryText: string.Empty, OperationName: null, CollectionName: null);
        }

        string? sanitizedSql = statement;
        (string? OperationName, string? CollectionName) parsedSql = SqlParser.NullResultTuple;

        // TODO: Cache these results. Design a cache that will not grow unbounded.
        if (this.sanitizeSql)
        {
            sanitizedSql = SqlSanitizer.GetObfuscatedSql(statement, Dialect);
        }

        if (this.parseSql)
        {
            parsedSql = SqlParser.GetParsedDatabaseStatement(commandType, statement);
        }

        return (QueryText: sanitizedSql, parsedSql.OperationName, parsedSql.CollectionName);
    }
}
