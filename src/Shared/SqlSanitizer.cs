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

    private static readonly Regex AllDialectsRegex = new Regex(string.Join("|", SingleQuote, DoubleQuote, DollarQuote, OracleQuote, Comment, MultilineComment, Uuid, Hex, Boolean, Number), regexOptions);
    private static readonly Regex AllUnmatchedRegex = new Regex("'|\"|/\\*|\\*/|\\$", regexOptions);
    private static readonly Regex MySqlDialectRegex = new Regex(string.Join("|", SingleQuote, DoubleQuote, Comment, MultilineComment, Hex, Boolean, Number), regexOptions);
    private static readonly Regex MySqlUnmatchedRegex = new Regex("'|\"|/\\*|\\*/", regexOptions);
    private static readonly Regex PostgresDialectRegex = new Regex(string.Join("|", SingleQuote, DollarQuote, Comment, MultilineComment, Uuid, Boolean, Number), regexOptions);
    private static readonly Regex PostgresUnmatchedRegex = new Regex("'|/\\*|\\*/|\\$(?!\\?)", regexOptions);
    private static readonly Regex OracleDialectRegex = new Regex(string.Join("|", SingleQuote, OracleQuote, Comment, MultilineComment, Number), regexOptions);
    private static readonly Regex OracleUnmatedRegex = new Regex("'|/\\*|\\*/", regexOptions);

    private static RegexOptions regexOptions = RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled;

    public static string GetSanitizedSql(string sql, SqlDialect dialect)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return sql ?? string.Empty;
        }

        var sanitizedSql = string.Empty;
        switch (dialect)
        {
            case SqlDialect.MySql:
                sanitizedSql = MySqlDialectRegex.Replace(sql, "?");
                return CheckForUnmatchedPairs(MySqlUnmatchedRegex, sanitizedSql);
            case SqlDialect.Oracle:
                sanitizedSql = OracleDialectRegex.Replace(sql, "?");
                return CheckForUnmatchedPairs(OracleUnmatedRegex, sanitizedSql);
            case SqlDialect.Postgres:
                sanitizedSql = PostgresDialectRegex.Replace(sql, "?");
                return CheckForUnmatchedPairs(PostgresUnmatchedRegex, sanitizedSql);
            default:
                sanitizedSql = AllDialectsRegex.Replace(sql, "?");
                return CheckForUnmatchedPairs(AllUnmatchedRegex, sanitizedSql);
        }
    }

    /// <summary>
    /// Checks to see if there are any unclosed quotes or comments remaining in the sanitized SQL.
    /// If there are then ? is returned to prevent leaking sensitive data.
    /// </summary>
    private static string CheckForUnmatchedPairs(Regex regex, string sanitizedSql)
    {
        return regex.Match(sanitizedSql).Success == true ? "?" : sanitizedSql;
    }
}
