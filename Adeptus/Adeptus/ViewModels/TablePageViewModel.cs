using Adeptus.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Adeptus.ViewModels;

public partial class TablePageViewModel : PageViewModel
{
    public ObservableCollection<Issue> Issues { get; private set; } = [];

    [ObservableProperty]
    public partial Issue? SelectedIssue { get; set; }

    public Action? OnIssuesLoaded { get; set; }

    public async Task LoadIssues(string fileName)
    {
        using var db = new AppDbContext(fileName);
        var issues = await db.GetIssues();

        Issues.Clear();
        foreach (var issue in issues)
        {
            Issues.Add(issue);
        }

        OnIssuesLoaded?.Invoke();
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
