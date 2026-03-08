using Adeptus.Models;
using Adeptus.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Adeptus.ViewModels;

public class TablePageViewModel : PageViewModel
{
    public ObservableCollection<Issue> Issues { get; private set; } = [];

    public TablePageViewModel() : base()
    {
    }

    public record LoadStats(int Total, int Opened, int Shown);

    public async Task<LoadStats> LoadIssues(string fileName)
    {
        using var db = new AppDbContext(fileName);
        var data = await db.Issues.AsNoTracking().ToListAsync();

        Issues.Clear();

        int opened = 0;

        foreach (var issue in data)
        {
            Issues.Add(new()
            {
                Id = issue.Id,
                Title = issue.Title,
                IsDone = issue.IsDone,
                Updated = issue.Updated,
            });

            if (!issue.IsDone)
                opened++;
        }

        return new LoadStats(Issues.Count, opened, Issues.Count);
    }
}

public class DesignTablePageViewModel : TablePageViewModel
{
    public DesignTablePageViewModel() : base()
    {
        Issues.Add(new() { Id = 35, Title = "Изменять единицы измерения в редакторах параметров горячими клавишами", IsDone = false, Updated = DateTime.Now });
        Issues.Add(new() { Id = 103, Title = "Скриптовые пользовательские элементы с произвольным набором параметров", IsDone = true, Updated = DateTime.Now });
    }
}
