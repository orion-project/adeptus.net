using System.Collections.Generic;
using System.IO;

namespace Adeptus.Models;

using SysPath = System.IO.Path;

public class Database
{
    public string Path { get; private set; }

    public string Name { get; private set; }

    public IList<Issue> Issues { get; private set; } = [];

    private int _maxId = 0;

    public Database(string path)
    {
        Path = path.TrimEnd(SysPath.DirectorySeparatorChar);
        Name = SysPath.GetFileName(Path);

        Load();
    }

    public int MakeNewId()
    {
        return ++_maxId;
    }

    public void NewIssueCreated(Issue issue)
    {
        issue.Save(Path);
        Issues.Add(issue);
    }

    private void Load()
    {
        foreach (string filePath in Directory.EnumerateFiles(Path, "*.md", SearchOption.TopDirectoryOnly))
        {
            Issue? issue = Issue.QuickLoad(filePath);
            if (issue == null)
            {
                continue;
            }

            Issues.Add(issue);

            if (issue.Id > _maxId)
            {
                _maxId = issue.Id;
            }
        }
    }
}
