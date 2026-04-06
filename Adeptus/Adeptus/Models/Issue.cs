using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Adeptus.Models;

public static class Context
{
    // TODO: extract to settings
    public const string Place = "home";
}

public class Issue
{
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

    /// <summary>
    /// Full file path where the issue is stored.
    /// </summary>
    public string FilePath { get; private set; }

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
    public IList<HistoryItem>? History { get; private set; }

    public bool IsFullyLoaded => History != null;

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
        (int? id, bool? done) = IssueParser.ParseFileName(filePath);
        if (id is null || done is null)
            return null;

        var issue = new Issue
        { 
            Id = id.Value,
            Done = done.Value,
            FilePath = filePath,
        };

        issue.Load(LoadMode.LatestInfo);

        return issue;
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
        FilePath = string.Empty;
        History =
        [
            // The first history item contains
            // - the full issue description: title + text
            // - the issue creation date and place
            new HistoryItem
            {
                Text = history,
                Date = DateTime.Now,
                Place = Context.Place,
            }
        ];
    }

    private Issue()
    {
    }

    public void Load()
    {
        Load(LoadMode.FullHistory);
    }

    private enum LoadMode { LatestInfo, FullHistory }

    private void Load(LoadMode mode)
    {
        int lineNo = 0;
        string? line = null;
        try
        {
            if (string.IsNullOrEmpty(FilePath))
                throw new AppError("Issue is not quick-loaded yet");

            DateTime? lastestUpdated = null;
            string latestPlace = string.Empty;
            string latestTitle = string.Empty;
            List<HistoryItem>? history = null;
            List<string>? historyLines = null;
            HistoryItem? historyItem = null;
            HistoryEditItem? historyEditItem = null;

            string HistoryItemText() => string.Join(NewLine, historyLines!).Trim();

            void FinalizeHistoryItem()
            {
                if (historyItem == null)
                    return;

                if (historyEditItem != null)
                {
                    historyEditItem.Text = HistoryItemText();
                    historyItem.AddEditItem(historyEditItem);
                    historyEditItem = null;
                }
                else
                {
                    historyItem.Text = HistoryItemText();
                }
                history!.Add(historyItem);
                historyItem = null;
            }

            using var reader = new StreamReader(FilePath);
            while (!reader.EndOfStream)
            {
                lineNo++;
                line = reader.ReadLine();
                if (line == null)
                    continue;

                if (lastestUpdated is null)
                {
                    line = line.Trim();
                    if (line.Length == 0)
                        continue;

                    (lastestUpdated, latestPlace) = IssueParser.ParseHeaderLine(line);
                }
                else if (history is null)
                {
                    line = line.Trim();
                    if (line.Length == 0)
                        continue;

                    latestTitle = line;

                    if (mode == LoadMode.LatestInfo)
                        break;

                    history = [];
                }
                else if (line.EndsWith(IssueParser.HistoryMarker))
                {
                    line = line[..^IssueParser.HistoryMarker.Length];

                    if (line.StartsWith("###"))
                    {
                        if (historyItem == null)
                            throw new AppError("Header of level 2 (##) expected");

                        if (historyEditItem != null)
                        {
                            historyEditItem.Text = HistoryItemText();
                            historyItem.AddEditItem(historyEditItem);
                            historyEditItem = null;
                        }
                        else
                        {
                            historyItem.Text = HistoryItemText();
                        }

                        (DateTime date, string place) = IssueParser.ParseHeaderLine(line);
                        historyEditItem = new HistoryEditItem { Date = date, Place = place };
                        historyLines = [];
                    }
                    else if (line.StartsWith("##"))
                    {
                        FinalizeHistoryItem();

                        (DateTime date, string place) = IssueParser.ParseHeaderLine(line);
                        historyItem = new HistoryItem { Date = date, Place = place };
                        historyLines = [];
                    }
                    else
                    {
                        throw new AppError("Header line expected");
                    }
                }
                else
                {
                    historyLines?.Add(line);
                }
            }

            FinalizeHistoryItem();

            if (lastestUpdated is null || latestTitle is null)
            {
                throw new AppError("Issue file seems to be empty");
            }

            Updated = lastestUpdated.Value;
            Title = latestTitle;
            Place = latestPlace;
            History = history;
        }
        catch (Exception e)
        {
            string title = lineNo > 0 ? $"Line {lineNo}: {e.Message}" : e.Message;

            Invalid = true;
            Title = title;
            History =
            [
                new HistoryItem
                {
                    Text = $"""
                            {title}

                            ```
                            {e.StackTrace}
                            ```

                            Line {lineNo}:

                            ```
                            {line}
                            ```
                            """,
                    Date = DateTime.Now,
                    Place = Context.Place,
                }
            ];
        }
    }

    public void Save(string dir)
    {
        FilePath = Path.Combine(dir, $"{Id}.md");

        using var stream = new StreamWriter(FilePath, false, Encoding.UTF8);

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
            throw new AppError("Issue is not fully loaded yet");
        }

        var history = History.First();
        string details;
        // If the issue description has been edited,
        // then the actual description is the last one edit
        // Here we suppose they are sorted by date during loading
        if (history.Edits != null && history.Edits.Count > 0)
        {
            details = history.Edits.Last().Text ?? string.Empty;
        }
        else
        {
            details = history.Text ?? string.Empty;
        }
        details = string.Join(Environment.NewLine,
            details
            .Trim()         // just in case, there should not be odd whitespaces
            .Split(NewLine) // don't use Environment.NewLine for cross-platform-ability
            .Skip(1)        // the title
            .SkipWhile(string.IsNullOrWhiteSpace) // empty lines between title and details
            );
        return details;
    }
}
