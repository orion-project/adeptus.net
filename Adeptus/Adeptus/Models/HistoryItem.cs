using System;
using System.Collections.Generic;
using System.IO;

namespace Adeptus.Models;

public class HistoryItem
{
    public string? Text { get; set; }

    public DateTime Date { get; set; }

    public string? Place { get; set; }

    public List<HistoryEditItem>? Edits { get; set; }

    public void Write(StreamWriter stream)
    {
        stream.WriteLine($"## {Date:s} ({Place}) {IssueParser.HistoryMarker}");
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

    public void AddEditItem(HistoryEditItem editItem)
    {
        Edits ??= [];
        Edits.Add(editItem);
    }
}

public class HistoryEditItem
{
    public string? Text { get; set; }

    public string? Place { get; set; }

    public DateTime Date { get; set; }

    public void Write(StreamWriter stream)
    {
        stream.WriteLine($"### {Date:s} ({Place}) ${IssueParser.HistoryMarker}");
        stream.WriteLine();
    }
}
