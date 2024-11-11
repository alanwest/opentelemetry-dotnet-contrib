// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;

namespace OpenTelemetry.Instrumentation;

internal static class SqlSanitizer
{
    private const string SingleQuote = "'(?:[^']|'')*?(?:\\\\'.*|'(?!'))";
    private const string DoubleQuote = "\"(?:[^\"]|\"\")*?(?:\\\\\".*|\"(?!\"))";
    private const string DollarQuote = "(\\$(?!\\d)[^$]*?\\$).*?(?:\\1|$)";
    private const string OracleQuote = "q'\\[.*?(?:\\]'|$)|q'\\{.*?(?:\\}'|$)|q'<.*?(?:>'|$)|q'\\(.*?(?:\\)'|$)";
    private const string Comment = "(?:#|--).*?(?=\\r|\\n|$)";
    private const string MultilineComment = "/\\*(?:[^/]|/[^*])*?(?:\\*/|/\\*.*)";
    private const string Uuid = "\\{?(?:[0-9a-f]\\-*){32}\\}?";
    private const string Hex = "0x[0-9a-f]+";
    private const string Boolean = "\\b(?:true|false|null)\\b";
    private const string Number = "-?\\b(?:[0-9_]+\\.)?[0-9_]+([eE][+-]?[0-9_]+)?";

    private const string AllUnmatchedPattern = "'|\"|/\\*|\\*/|\\$";
    private const string MySqlUnmatchedPattern = "'|\"|/\\*|\\*/";
    private const string PostgresUnmatchedPattern = "'|/\\*|\\*/|\\$(?!\\?)";
    private const string OracleUnmatedPattern = "'|/\\*|\\*/";

    private static readonly string AllDialectsPattern = string.Join("|", SingleQuote, DoubleQuote, DollarQuote, OracleQuote, Comment, MultilineComment, Uuid, Hex, Boolean, Number);
    private static readonly string MySqlDialectPattern = string.Join("|", SingleQuote, DoubleQuote, Comment, MultilineComment, Hex, Boolean, Number);
    private static readonly string PostgresDialectPattern = string.Join("|", SingleQuote, DollarQuote, Comment, MultilineComment, Uuid, Boolean, Number);
    private static readonly string OracleDialectPattern = string.Join("|", SingleQuote, OracleQuote, Comment, MultilineComment, Number);

    private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled;

    public static string GetSanitizedSql(string sql, SqlDialect dialect)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return sql ?? string.Empty;
        }

        string sanitizedSql;
        switch (dialect)
        {
            case SqlDialect.MySql:
                sanitizedSql = Regex.Replace(sql, MySqlDialectPattern, "?", RegexOptions);
                return CheckForUnmatchedPairs(MySqlUnmatchedPattern, sanitizedSql);
            case SqlDialect.Oracle:
                sanitizedSql = Regex.Replace(sql, OracleDialectPattern, "?", RegexOptions);
                return CheckForUnmatchedPairs(OracleUnmatedPattern, sanitizedSql);
            case SqlDialect.Postgres:
                sanitizedSql = Regex.Replace(sql, PostgresDialectPattern, "?", RegexOptions);
                return CheckForUnmatchedPairs(PostgresUnmatchedPattern, sanitizedSql);
            default:
                sanitizedSql = Regex.Replace(sql, AllDialectsPattern, "?", RegexOptions);
                return CheckForUnmatchedPairs(AllUnmatchedPattern, sanitizedSql);
        }
    }

    /// <summary>
    /// Checks to see if there are any unclosed quotes or comments remaining in the sanitized SQL.
    /// If there are then ? is returned to prevent leaking sensitive data.
    /// </summary>
    private static string CheckForUnmatchedPairs(string pattern, string sanitizedSql)
    {
        return Regex.Match(sanitizedSql, pattern, RegexOptions).Success == true ? "?" : sanitizedSql;
    }
}
