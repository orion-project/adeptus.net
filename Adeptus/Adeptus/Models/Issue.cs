using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Adeptus.Models;

public static class Tags
{
    public const string History = "<adeptus-history/>";
}

public static class Context
{
    // TODO: extract to settings
    public const string Place = "home";
}

public class Issue
{
    private string? _filePath;

    /// <summary>
    /// Issue identifier (auto-generated)
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Short issue description, one row without markdown and line breakes
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// If the issue is completed (solved, closed)
    /// </summary>
    public bool Done { get; private set; }

    /// <summary>
    /// A workplace where the issue has been last updated, e.g. "home", "work"
    /// </summary>
    public string Place { get; private set; }

    /// <summary>
    /// A date when the issue has been updated
    /// </summary>
    public DateTime Updated { get; private set; }

    public string Tags {get; private set;} = string.Empty;

    /// <summary>
    /// The flag shows that the issue was loaded with errros.
    /// In this case, <see cref="Title"/> contains an error message
    /// and <see cref="GetDetails"/> returns exception stack trace.
    /// Such issue can not be edited in the app, its file must be fixed manually.
    /// </summary>
    public bool Invalid {get; private set;}


    /// <summary>
    /// More items added to the issue during processing - comments, status changes
    /// If the <see cref="History"/> is null, this means the Issue is only quick-read
    /// and contains only basic information which is enough to show it in the table row.
    /// Use the <see cref="Load"/> method to fill the History and get the full issue data.
    /// </summary>
    public List<HistoryItem>? History { get; private set; }

    private static readonly Regex FileNameRegex = new(
        @"^(?<id>\d+)(?<done>\.done)?\.md$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Line delimiter.
    /// Because we are going to be cross-platform, don't use Environment.NewLine.
    /// </summary>
    private static readonly char NewLine = '\n';

    /// <summary>
    /// Creates an Issue reading only several first lines of the given issue file
    /// to obtain a basic information about the issue, which is enough to show it in the table row.
    /// Use the <see cref="Load"/> method to fill the History and get the full issue data.
    /// </summary>
    public static Issue? QuickLoad(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        Match fileNameMatch = FileNameRegex.Match(fileName);
        if (!fileNameMatch.Success)
        {
            return null;
        }

        int id = int.Parse(fileNameMatch.Groups["id"].Value);
        bool done = fileNameMatch.Groups["done"].Success;

        DateTime? updated = null;
        string place = string.Empty;
        string title = string.Empty;

        try
        {
            int lineNo = 0;
            using var reader = new StreamReader(filePath);
            while (!reader.EndOfStream)
            {
                lineNo++;
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (updated is null)
                {
                    (updated, place) = IssueParser.ParseHeaderLine(line, lineNo);
                }
                else
                {
                    title = line.Trim();
                    break;
                }
            }
            if (updated is null || title is null)
            {
                throw new AppError("Issue file seems to be empty");
            }
        }
        catch (Exception e)
        {
            return new Issue(id, e.Message, e.StackTrace?.ToString())
            {
                _filePath = filePath,
                Invalid = true
            };
        }

        return new Issue(id, title)
        {
            _filePath = filePath,
            Updated = updated.Value,
            Place = place,
            Done = done,
        };
    }

    public Issue(int id, string title, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("A title must be provided");
        }

        string history = title.Trim();
        if (!string.IsNullOrWhiteSpace(details))
        {
            history += $"{NewLine}{NewLine}{details.Trim()}";
        }

        Id = id;
        Title = title.Trim();
        Done = false;
        Place = Context.Place;
        Updated = DateTime.Now;
        History =
        [
            // The first history item contains
            // - the full issue description: title + text
            // - the issue creation date and place
            new HistoryItem(history)
        ];
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            throw new AppError($"Issue #{Id} is now quick-loaded yet");
        }

        int lineNo = 0;
        using var reader = new StreamReader(_filePath);
        while (!reader.EndOfStream)
        {
            lineNo++;
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // TODO
        }
    }

    public void Save(string dir)
    {
        _filePath = Path.Combine(dir, $"{Id}.md");

        using var stream = new StreamWriter(_filePath, false, Encoding.UTF8);

        // The current state on the top to make it easy readable
        stream.WriteLine($"# {Updated:s} ({Place})");
        stream.WriteLine();
        // TODO: write tags here
        stream.WriteLine(Title);
        stream.WriteLine();

        if (History != null)
        {
            foreach (var item in History)
            {
                item.Write(stream);
            }
        }
    }

    /// <summary>
    /// Returns detailed issue description in markdown format.
    /// </summary>
    public string GetDetails()
    {
        if (History == null || History.Count == 0)
        {
            throw new AppError("Bad issue format");
        }

        var history = History.First();
        string details;
        // If the issue description has been edited,
        // then the actual description is the last one edit
        // Here we suppose they are sorted by date during loading
        if (history.Edits != null && history.Edits.Count > 0)
        {
            details = history.Edits.Last().Text;
        }
        else
        {
            details = history.Text;
        }
        var lines = details
            .Trim()         // just in case, there should not be odd whitespaces
            .Split(NewLine) // don't use Environment.NewLine for cross-platform-ability
            .Skip(1)        // the title
            .SkipWhile(string.IsNullOrWhiteSpace) // empty lines between title and details
            ;
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.Append(line);
        }
        return sb.ToString();
    }
}

public class HistoryItem(string text)
{
    public string Text { get; private set; } = text;

    public DateTime Date { get; private set; } = DateTime.Now;

    public string Place { get; private set; } = Context.Place;

    public List<HistoryEditItem>? Edits { get; private set; }

    public void Write(StreamWriter stream)
    {
        stream.WriteLine($"## {Date:s} ({Place}) {Tags.History}");
        stream.WriteLine();
        stream.WriteLine(Text);
        stream.WriteLine();

        if (Edits != null)
        {
            foreach (var edit in Edits)
            {
                edit.Write(stream);
            }
        }
    }
}

public class HistoryEditItem(string text)
{
    public string Text { get; private set; } = text;

    public string Place { get; private set; } = Context.Place;

    public DateTime Date { get; private set; } = DateTime.Now;

    public void Write(StreamWriter stream)
    {
        stream.WriteLine($"### {Date:s} ({Place}) ${Tags.History}");
        stream.WriteLine();
    }
}

static class IssueParser
{
    private static readonly Regex headerRegex = new(
        @"^#\s+(?<updated>\S+)\s+\((?<place>[^)]+)\)\s*$",
        RegexOptions.Compiled
    );

    public static (DateTime date, string place) ParseHeaderLine(string line, int lineNo)
    {
        var match = headerRegex.Match(line);
        if (!match.Success)
        {
            throw new AppError($"Line {lineNo}: invalid header format");
        }

        if (!DateTime.TryParseExact(
            match.Groups["updated"].Value,
            "s",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date
        ))
        {
            throw new AppError($"Line {lineNo}: invalid date");
        }

        string place = match.Groups["place"].Value.Trim();

        return (date, place);
    }
}
