using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Adeptus.Models;

static class IssueParser
{
    public const string HistoryMarker = "<adeptus-history/>";

    private static readonly Regex FileNameRegex = new(
        @"^(?<id>\d+)(?<done>\.done)?\.md$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex headerRegex = new(
        @"^#+\s+(?<updated>\S+)\s+\((?<place>[^)]+)\)\s*$",
        RegexOptions.Compiled
    );

    public static (int? id, bool? done) ParseFileName(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Match fileNameMatch = FileNameRegex.Match(fileName);
        if (!fileNameMatch.Success)
        {
            return (null, null);
        }

        int id = int.Parse(fileNameMatch.Groups["id"].Value);
        bool done = fileNameMatch.Groups["done"].Success;

        return (id, done);
    }

    public static (DateTime date, string place) ParseHeaderLine(string line)
    {
        var match = headerRegex.Match(line);
        if (!match.Success)
        {
            throw new AppError("Invalid header format");
        }

        if (!DateTime.TryParseExact(
            match.Groups["updated"].Value,
            "s",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date
        ))
        {
            throw new AppError("Invalid date");
        }

        string place = match.Groups["place"].Value.Trim();

        return (date, place);
    }
}
