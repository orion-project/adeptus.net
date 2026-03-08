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

    public async Task LoadIssues()
    {
        using var db = new AppDbContext();
        var data = await db.Issue.AsNoTracking().ToListAsync();

        Issues.Clear();
        foreach (var issue in data)
        {
            Issues.Add(issue);
        }
        DialogManager.ShowInfo("Loading", "Data loaded.");
    }
}

public class DesignTablePageViewModel : TablePageViewModel
{
    public static readonly DesignTablePageViewModel Instance = new ();

    public DesignTablePageViewModel() : base()
    {
        Issues.Add(new() { Id = 35, Summary = "Изменять единицы измерения в редакторах параметров горячими клавишами", Updated = DateTime.Now });
        Issues.Add(new() { Id = 103, Summary = "Скриптовые пользовательские элементы с произвольным набором параметров", Updated = DateTime.Now });
    }
}
