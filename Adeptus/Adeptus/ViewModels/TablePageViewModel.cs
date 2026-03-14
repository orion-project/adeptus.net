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
        Issues.Clear();

        // TODO

        OnIssuesLoaded?.Invoke();
    }
}

public class DesignTablePageViewModel : TablePageViewModel
{
    public DesignTablePageViewModel() : base()
    {
        Issues.Add(new(35, "Изменять единицы измерения в редакторах параметров горячими клавишами", ""));
        Issues.Add(new(103, "Скриптовые пользовательские элементы с произвольным набором параметров", ""));
    }
}
