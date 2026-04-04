using Adeptus.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace Adeptus.ViewModels;

public partial class TablePageViewModel(Action showSelectedIssue) : PageViewModel()
{
    public ObservableCollection<Issue> Issues { get; private set; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowSelectedIssueCommand))]
    public partial Issue? SelectedIssue { get; set; }

    public void DatabaseLoaded(Database database)
    {
        Issues.Clear();

        foreach (Issue issue in database.Issues)
        {
            Issues.Add(issue);
        }
    }

    public void NewIssueCreated(Issue issue)
    {
        Issues.Add(issue);
        SelectedIssue = issue;
    }

    private bool CanShowSelectedIssue() => SelectedIssue is not null;

    [RelayCommand(CanExecute = nameof(CanShowSelectedIssue))]
    private void ShowSelectedIssue()
    {
        showSelectedIssue.Invoke();
    }
}

public class DesignTablePageViewModel : TablePageViewModel
{
    public DesignTablePageViewModel() : base(() => { })
    {
        Issues.Add(new(35, "Изменять единицы измерения в редакторах параметров горячими клавишами", ""));
        Issues.Add(new(103, "Скриптовые пользовательские элементы с произвольным набором параметров", ""));
    }
}
