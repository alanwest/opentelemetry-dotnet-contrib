// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenTelemetry.Instrumentation.SqlClient.Implementation;

/// <summary>
/// Extracts features from SQL statement that are used to make a metric name.
///
/// This uses ad-hoc scanning techniques.
/// The scanner uses simple regular expressions.
/// The scanner must be fast, as it is called for every SQL statement executed in the profiled code.
/// The scanner is not a full parser; there are many constructs it can not handle, such as sequential statements (;),
/// and the scanner has been extended in an ad-hoc manner as the need arises.
///
/// Database tracing is one of our largest sources of agent overhead.
/// The issue is that many applications issue hundreds or thousands of database queries per transaction,
/// so our db tracers are invoked much more often then other tracers.
/// Our database tracers are also usually doing a lot more than other tracers,
/// like parsing out SQL statements.  Just tread carefully with that in mind when making changes here.
///
/// When it comes to it, most users aren't going to want us to do really sophisticated sql parsing
/// if it comes at the expense of increased overhead.
/// </summary>
internal static class SqlParser
{
    internal static readonly (string? OperationName, string? CollectionName) NullResultTuple = (null, null);

    private const RegexOptions PatternSwitches = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;

    private const char SemiColon = ';';
    private const string SelectPhrase = @"^\bselect\b.*?\s+";
    private const string InsertPhrase = @"^insert\s+into\s+";
    private const string UpdatePhrase = @"^update\s+";
    private const string DeletePhrase = @"^delete\s+";
    private const string CreatePhrase = @"^create\s+";
    private const string DropPhrase = @"^drop\s+";
    private const string AlterPhrase = @"^alter\s+";
    private const string CallPhrase = @"^call\s+";
    private const string SetPhrase = @"^set\s+@?";
    private const string DeclarePhrase = @"^declare\s+@?";

    // Shortcut phrases.  Parsers determine if they are applicable by checking the start of a cleaned version of the statement
    // for specific keywords.  If they are deemed applicable, the more expensive regEx is run against the statement to
    // extract the information and build the SqlStatementInfo.
    private const string InsertPhraseShortcut = "insert";
    private const string UpdatePhraseShortcut = "update";
    private const string DeletePhraseShortcut = "delete";
    private const string CreatePhraseShortcut = "create";
    private const string DropPhraseShortcut = "drop";
    private const string AlterPhraseShortcut = "alter";
    private const string CallPhraseShortcut = "call";
    private const string SetPhraseShortcut = "set";
    private const string DeclarePhraseShortcut = "declare";
    private const string ExecuteProcedure1Shortcut = "exec";
    private const string ExecuteProcedure2Shortcut = "execute";
    private const string ExecuteProcedure3Shortcut = "sp_";
    private const string ShowPhraseShortcut = "show";
    private const string WaitforPhraseShortcut = "waitfor";

    // Regex to match only single SQL statements (i.e. no semicolon other than at the end)
    private const string SingleSqlStatementPhrase = @"^[^;]*[\s;]*$";

    private const string CommentPhrase = @"/\*.*?\*/"; // The ? makes the searching lazy
    private const string LeadingSetPhrase = @"^(?:\s*\bset\b.+?\;)+(?!(\s*\bset\b))";
    private const string StartObjectNameSeparator = @"[\s\(\[`\""]*";
    private const string EndObjectNameSeparator = @"[\s\)\]`\""]*";
    private const string ValidObjectName = @"([^,;\[\s\]\(\)`\""\.]*)";
    private const string FromPhrase = @"from\s+";
    private const string VariableNamePhrase = @"([^\s(=,]*).*";
    private const string ObjectTypePhrase = @"([^\s]*)";
    private const string CallObjectPhrase = @"([^\s(,]*).*";
    private const string MetricNamePhrase = @"^[a-z0-9.\$_]*$";

    // Regex Strings
    private const string SelectString = SelectPhrase + FromPhrase + @"(?:" + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")(?:\." + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")*";
    private const string InsertString = InsertPhrase + @"(?:" + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")(?:\." + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")*";
    private const string UpdateString = UpdatePhrase + @"(?:" + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")(?:\." + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")*";
    private const string DeleteString = DeletePhrase + "(" + FromPhrase + @")?(?:" + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")(?:\." + StartObjectNameSeparator + ValidObjectName + EndObjectNameSeparator + @")*";
    private const string CreateString = CreatePhrase + ObjectTypePhrase;
    private const string DropString = DropPhrase + ObjectTypePhrase;
    private const string AlterString = AlterPhrase + ObjectTypePhrase + ".*";
    private const string CallString = CallPhrase + CallObjectPhrase;
    private const string SetString = SetPhrase + VariableNamePhrase;
    private const string DeclareString = DeclarePhrase + VariableNamePhrase;

    // as for example
    //   ... FROM Northwind.dbo.[Order Details] AS OrdDet ...
    // This was first noticed for the SELECT ... FROM statements from the Northwind example DB.

    // Order these statement parsers in descending order of frequency of use.
    // We'll do a linear search through the table to find the appropriate matcher.
    private static readonly ParseStatement StatementParser = CreateCompoundStatementParser();

    private static readonly KeyValuePair<char, char>[] Bookends =
    [
        new('[', ']'),
        new('"', '"'),
        new('\'', '\''),
        new('(', ')'),
        new('`', '`'),
    ];

    private static readonly Regex CommentPattern = new(CommentPhrase, RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex LeadingSetPattern = new(LeadingSetPhrase, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex ValidMetricNameMatcher = new(MetricNamePhrase, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SingleSqlStatementMatcher = new(SingleSqlStatementPhrase, PatternSwitches);
    private static readonly Regex SelectRegex = new(SelectString, PatternSwitches);
    private static readonly Regex InsertRegex = new(InsertString, PatternSwitches);
    private static readonly Regex UpdateRegex = new(UpdateString, PatternSwitches);
    private static readonly Regex DeleteRegex = new(DeleteString, PatternSwitches);
    private static readonly Regex CreateRegex = new(CreateString, PatternSwitches);
    private static readonly Regex DropRegex = new(DropString, PatternSwitches);
    private static readonly Regex AlterRegex = new(AlterString, PatternSwitches);
    private static readonly Regex CallRegex = new(CallString, PatternSwitches);
    private static readonly Regex SetRegex = new(SetString, PatternSwitches);
    private static readonly Regex DeclareRegex = new(DeclareString, PatternSwitches);
    private static readonly Regex ExecuteProcedureRegex1 = new(@"^exec\s+(?:[^\s=]+\s*=\s*)?([^\s(,;]+)", PatternSwitches);
    private static readonly Regex ExecuteProcedureRegex2 = new(@"^execute\s+(?:[^\s=]+\s*=\s*)?([^\s(,;]+)", PatternSwitches);
    private static readonly Regex ExecuteProcedureRegex3 = new(@"^(sp_\s*[^\s]*).*", PatternSwitches);
    private static readonly char[] Period = ['.'];

    private delegate (string? OperationName, string? CollectionName) ParseStatement(CommandType commandType, string commandText, string statement);

    public static (string? OperationName, string? CollectionName) GetParsedDatabaseStatement(CommandType commandType, string commandText)
    {
        try
        {
            switch (commandType)
            {
                case CommandType.TableDirect:
                    return GetTuple("select", null);
                case CommandType.StoredProcedure:
                    return GetTuple("ExecuteProcedure", FixDatabaseObjectName(commandText));
            }

            // Remove comments.
            var statement = CommentPattern.Replace(commandText, string.Empty).TrimStart();

            if (!IsSingleSqlStatement(statement))
            {
                // Remove leading SET commands

                // Trimming any trailing semicolons is necessary to avoid having the LeadingSetPattern
                // match a SQL statement that ONLY contains SET commands, which would leave us with nothing
                statement = statement.TrimEnd(SemiColon);
                statement = LeadingSetPattern.Replace(statement, string.Empty).TrimStart();
            }

            return StatementParser(commandType, commandText, statement);
        }
        catch
        {
            return NullResultTuple;
        }
    }

    private static (string? OperationName, string? CollectionName) GetTuple(string? operationName, string? collectionName)
    {
        return (OperationName: operationName, CollectionName: collectionName);
    }

    private static bool IsValidName(string name)
    {
        return ValidMetricNameMatcher.IsMatch(name);
    }

    private static bool IsSingleSqlStatement(string sql)
    {
        return SingleSqlStatementMatcher.IsMatch(sql);
    }

    private static ParseStatement CreateCompoundStatementParser()
    {
        var parsers = new ParseStatement[]
        {
            // selects are tricky with the set crap on the front of the statement.. /* fooo */set
            // leave those out of the dictionary
            new DefaultStatementParser("select", SelectRegex, string.Empty).ParseStatement,
            new ShowStatementParser().ParseStatement,
            new DefaultStatementParser("insert", InsertRegex, InsertPhraseShortcut).ParseStatement,
            new DefaultStatementParser("update", UpdateRegex, UpdatePhraseShortcut).ParseStatement,
            new DefaultStatementParser("delete", DeleteRegex, DeletePhraseShortcut).ParseStatement,

            new DefaultStatementParser("ExecuteProcedure", ExecuteProcedureRegex1, ExecuteProcedure1Shortcut).ParseStatement,
            new DefaultStatementParser("ExecuteProcedure", ExecuteProcedureRegex2, ExecuteProcedure2Shortcut).ParseStatement,

            // Invocation of a conventionally named stored procedure.
            new DefaultStatementParser("ExecuteProcedure", ExecuteProcedureRegex3, ExecuteProcedure3Shortcut).ParseStatement,

            new DefaultStatementParser("create", CreateRegex, CreatePhraseShortcut).ParseStatement,
            new DefaultStatementParser("drop", DropRegex, DropPhraseShortcut).ParseStatement,
            new DefaultStatementParser("alter", AlterRegex, AlterPhraseShortcut).ParseStatement,
            new DefaultStatementParser("call", CallRegex, CallPhraseShortcut).ParseStatement,

            // See http://msdn.microsoft.com/en-us/library/ms189484.aspx
            // The set statement targets a local identifier whose name may start with @.  We just scan over the @.
            new DefaultStatementParser("set", SetRegex, SetPhraseShortcut).ParseStatement,

            // See http://msdn.microsoft.com/en-us/library/ms188927.aspx
            // The declare statement targets a local identifier whose name may start with @.  We just scan over the @.
            new DefaultStatementParser("declare", DeclareRegex, DeclarePhraseShortcut).ParseStatement,

            SelectVariableStatementParser.ParseStatement,

            // The Waitfor statement is [probably] only in Transact SQL.  There are multiple variations of the statement.
            // See http://msdn.microsoft.com/en-us/library/ms187331.aspx
            new WaitforStatementParser().ParseStatement,
        };

        // The parsers params are used inside a return function that is referenced by a static class member.
        // This effectively makes theses parsers stay with agent entire time.
        return (commandType, commandText, statement) =>
        {
            foreach (var parser in parsers)
            {
                var parsedStatement = parser(commandType, commandText, statement);
                if (parsedStatement != NullResultTuple)
                {
                    return parsedStatement;
                }
            }

            return NullResultTuple;
        };
    }

    private static string FixDatabaseObjectName(string s)
    {
#pragma warning disable CA1307 // Specify StringComparison for clarity
        int index = s.IndexOf('.');
#pragma warning restore CA1307 // Specify StringComparison for clarity
        return index > 0
            ? new StringBuilder(s.Length)
                .Append(RemoveBookendsAndLower(s.Substring(0, index)))
                .Append('.')
                .Append(FixDatabaseName(s.Substring(index + 1)))
                .ToString()
            : RemoveBookendsAndLower(s);
    }

    /// <summary>
    /// Remove "bookend" characters (brackets, quotes, parenthesis) and convert to lower case.
    /// </summary>
    private static string RemoveBookendsAndLower(string s)
    {
#pragma warning disable CA1308 // Normalize strings to uppercase
        return RemoveBracketsQuotesParenthesis(s).ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
    }

    private static string FixDatabaseName(string s)
    {
        StringBuilder sb = new StringBuilder(s.Length);
        bool first = true;
        foreach (string segment in s.Split(Period))
        {
            if (!first)
            {
                sb.Append(Period);
            }
            else
            {
                first = false;
            }

            sb.Append(RemoveBookendsAndLower(segment));
        }

        return sb.ToString();
    }

    private static string RemoveBracketsQuotesParenthesis(string value)
    {
        if (value.Length < 3)
        {
            return value;
        }

        var first = 0;
        var last = value.Length - 1;
        foreach (var kvp in Bookends)
        {
            while (value[first] == kvp.Key && value[last] == kvp.Value)
            {
                first++;
                last--;
            }
        }

        if (first != 0)
        {
            var length = value.Length - (first * 2);
            value = value.Substring(first, length);
        }

        return value;
    }

    public class DefaultStatementParser
    {
        private readonly Regex pattern;
        private readonly string? shortcut;
        private readonly string key;

        public DefaultStatementParser(string key, Regex pattern, string shortcut)
        {
            this.key = key;
            this.pattern = pattern;

            if (!string.IsNullOrEmpty(shortcut))
            {
                this.shortcut = shortcut;
            }
        }

        public virtual (string? OperationName, string? CollectionName) ParseStatement(CommandType commandType, string commandText, string statement)
        {
            if (!string.IsNullOrEmpty(this.shortcut) && !statement.StartsWith(this.shortcut, StringComparison.CurrentCultureIgnoreCase))
            {
                return NullResultTuple;
            }

            var matcher = this.pattern.Match(statement);
            if (!matcher.Success)
            {
                return NullResultTuple;
            }

            var model = "unknown";
            foreach (Group g in matcher.Groups)
            {
                var str = g.ToString();
                if (!string.IsNullOrEmpty(str))
                {
                    model = str;
                }
            }

            if (string.Equals(model, "select", StringComparison.OrdinalIgnoreCase))
            {
                model = "(subquery)";
            }
            else
            {
                model = FixDatabaseObjectName(model);
                if (!this.IsValidModelName(model))
                {
                    model = "ParseError";
                }
            }

            return this.CreateParsedDatabaseStatement(commandText, model);
        }

        protected virtual bool IsValidModelName(string name)
        {
            return IsValidName(name);
        }

        protected virtual (string? OperationName, string? CollectionName) CreateParsedDatabaseStatement(string commandText, string model)
        {
#pragma warning disable CA1308 // Normalize strings to uppercase
            return GetTuple(this.key, model.ToLowerInvariant());
#pragma warning restore CA1308 // Normalize strings to uppercase
        }
    }

    private class SelectVariableStatementParser
    {
        private static readonly Regex SelectMatcher = new(@"^select\s+([^\s,]*).*", PatternSwitches);
        private static readonly Regex FromMatcher = new(@"\s+from\s+", PatternSwitches);

        public static (string? OperationName, string? CollectionName) ParseStatement(CommandType commandType, string commandText, string statement)
        {
            var matcher = SelectMatcher.Match(statement);
            if (matcher.Success)
            {
                string model = FromMatcher.Match(statement).Success
                    ? "(subquery)"
                    : "VARIABLE";

                return GetTuple("select", model);
            }

            return NullResultTuple;
        }
    }

    private class ShowStatementParser : DefaultStatementParser
    {
        public ShowStatementParser()
            : base("show", new Regex(@"^\s*show\s+(.*)$", PatternSwitches), ShowPhraseShortcut)
        {
        }

        protected override bool IsValidModelName(string name)
        {
            return true;
        }

        protected override (string? OperationName, string? CollectionName) CreateParsedDatabaseStatement(string commandText, string model)
        {
            if (model.Length > 50)
            {
                model = model.Substring(0, 50);
            }

            return GetTuple("show", model);
        }
    }

    /// <summary>
    /// The Waitfor statement is [probably] only in Transact SQL.  There are multiple variations of the statement.
    /// See https://docs.microsoft.com/en-us/sql/t-sql/language-elements/waitfor-transact-sql.
    /// </summary>
    private class WaitforStatementParser : DefaultStatementParser
    {
        public WaitforStatementParser()
            : base("waitfor", new Regex(@"^waitfor\s+(delay|time)\s+([^\s,(;]*).*", PatternSwitches), WaitforPhraseShortcut)
        {
        }

        // All time stamps we match with the Regex are assumed to be valid "names" for our purpose.
        protected override bool IsValidModelName(string name)
        {
            return true;
        }

        protected override (string? OperationName, string? CollectionName) CreateParsedDatabaseStatement(string commandText, string model)
        {
            // We drop the time string in this.model on the floor.  It may contain quotes, colons, periods, etc.
            return GetTuple("waitfor", "time");
        }
    }
}
