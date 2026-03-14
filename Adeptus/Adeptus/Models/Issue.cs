using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
    /// More items added to the issue during processing - comments, status changes
    /// </summary>
    public List<HistoryItem>? History { get; private set; }

    public Issue(int id, string title, string details)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new Exception("A title must be provided");
        }

        string history = title.Trim();
        if (!string.IsNullOrWhiteSpace(details))
        {
            // Because we are going to be cross-platform, don't use Environment.NewLine
            history += "\n\n" + details.Trim();
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

    public void Save(string dir)
    {
        using var stream = new StreamWriter(Path.Combine(dir, $"{Id}.md"), false, Encoding.UTF8);

        // The current state on the top to meke it easy readable
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
    /// Detailed issue description in markdown format
    /// </summary>
    public string Details
    {
        get
        {
            if (History == null || History.Count == 0)
            {
                throw new Exception("Bad issue format");
            }
            var history = History.First();
            string details;
            // If the issue description has been edited,
            // then teh actual description is the last one edit
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
                .Trim()        // just in case, there should not be odd whitespaces
                .Split('\n')   // don't use Environment.NewLine for cross-platform-ability
                .Skip(1)       // the title
                .SkipWhile(line => string.IsNullOrWhiteSpace(line)) // empty lines between title and details
                ;
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.Append(line);
            }
            return sb.ToString();
        }
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
