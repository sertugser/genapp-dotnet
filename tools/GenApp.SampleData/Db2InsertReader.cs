using System.Text;
using System.Text.RegularExpressions;

namespace GenApp.SampleData;

// One "Insert Into <DB2DBID>.table (columns) Values (values);" statement of base/cntl/db2cre.jcl.
// A value is the literal text: a quoted string without its quotes, or a number. The sample data has no NULL.
internal sealed record Db2Insert(string Table, IReadOnlyList<string> Columns, IReadOnlyList<string> Values, int Line);

// Reads the sample rows of the INSERT step of db2cre.jcl (:412-1247). The statements are plain SQL, so only the
// table name, the column list and the value list are needed.
internal static partial class Db2InsertReader
{
    public static IReadOnlyList<Db2Insert> Read(string path)
    {
        var text = File.ReadAllText(path);
        var inserts = new List<Db2Insert>();

        foreach (Match match in InsertStart().Matches(text))
        {
            var line = text.AsSpan(0, match.Index).Count('\n') + 1;
            var columns = match.Groups["columns"].Value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var values = ReadValues(text, match.Index + match.Length, line);

            if (columns.Length != values.Count)
            {
                throw new FormatException(
                    $"db2cre.jcl:{line}: {columns.Length} columns but {values.Count} values.");
            }

            inserts.Add(new Db2Insert(match.Groups["table"].Value.ToLowerInvariant(), columns, values, line));
        }

        return inserts;
    }

    // Reads from just after "Values (" to the closing parenthesis. Quoted strings may contain commas and
    // parentheses; '' inside a quoted string is one quote. Spaces are kept only inside quotes (' ', ' 12b').
    private static List<string> ReadValues(string text, int start, int line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\'')
            {
                if (!quoted)
                {
                    quoted = true;
                }
                else if (i + 1 < text.Length && text[i + 1] == '\'')
                {
                    current.Append('\'');
                    i++;
                }
                else
                {
                    quoted = false;
                }
            }
            else if (quoted)
            {
                current.Append(c);
            }
            else if (c is ',' or ')')
            {
                values.Add(current.ToString());
                current.Clear();
                if (c == ')')
                {
                    return values;
                }
            }
            else if (!char.IsWhiteSpace(c))
            {
                current.Append(c);
            }
        }

        throw new FormatException($"db2cre.jcl:{line}: the value list is not closed.");
    }

    [GeneratedRegex(@"Insert\s+Into\s+<DB2DBID>\.(?<table>\w+)\s*\((?<columns>[^)]*)\)\s*Values\s*\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex InsertStart();
}
