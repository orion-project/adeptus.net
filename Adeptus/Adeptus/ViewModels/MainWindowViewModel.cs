using Adeptus.Models;
using Adeptus.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Adeptus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private const string DefaultWindowTitle = "Adeptus";

    private Database? _database;

    private Database Database
    {
        get
        {
            return _database ?? throw new AppError("Database is not opened");
        }
    }

    public ObservableCollection<PageViewModel> Pages { get; } = [];

    [ObservableProperty]
    public partial PageViewModel? SelectedPage { get; set; }

    [ObservableProperty]
    public partial int TotalIssueCount { get; protected set; }

    [ObservableProperty]
    public partial int OpenedIssueCount { get; protected set; }

    [ObservableProperty]
    public partial int ShownIssueCount { get; protected set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MakeNewIssueCommand))]
    public partial string DatabasePath { get; protected set; } = string.Empty;

    [ObservableProperty]
    public partial string DatabaseName { get; protected set; } = string.Empty;

    [ObservableProperty]
    public partial string WindowTitle { get; protected set; } = DefaultWindowTitle;

    private TablePageViewModel TablePage
    {
        get
        {
            if (Pages.Count == 0)
                throw new AppError("Database is not opened");
            if (Pages.First() is not TablePageViewModel vm)
                throw new AppError("Invalid pages structure");
            return vm;
        }
    }

    [RelayCommand]
    private async Task OpenDatabase()
    {
        try
        {
            var folderPath = await DialogManager.SelectFolderDialog(this, "Open Database");
            if (folderPath is null)
                return;

            await LoadDatabase(folderPath);
        }
        catch (Exception e)
        {
            ResetDatabase();
            DialogManager.ShowError(e.Message, "Failed to open database");
        }
    }

    private void ResetDatabase()
    {
        _database = null;
        Pages.Clear();
        DatabasePath = string.Empty;
        DatabaseName = string.Empty;
        WindowTitle = DefaultWindowTitle;
        TotalIssueCount = 0;
        ShownIssueCount = 0;
        OpenedIssueCount = 0;
    }

    private async Task LoadDatabase(string folderPath)
    {
        ResetDatabase();

        _database = new Database(folderPath);

        DatabasePath = Database.Path;
        DatabaseName = Database.Name;
        WindowTitle = $"{DatabaseName} - {DefaultWindowTitle}";

        var tablePage = new TablePageViewModel();
        tablePage.DatabaseLoaded(Database);
        Pages.Add(tablePage);

        UpdateIssueCounters();

        DialogManager.ShowInfo(Database.Path, "Database loaded");
    }

    private void UpdateIssueCounters()
    {
        var issues = Database.Issues;
        var opened = 0;
        foreach (var issue in issues)
        {
            if (!issue.Done)
                opened++;
        }
        TotalIssueCount = issues.Count;
        ShownIssueCount = issues.Count;
        OpenedIssueCount = opened;
    }

    [RelayCommand]
    private async Task ShowAboutDialog()
    {
    }

    private void ClosePageRequested(PageViewModel page)
    {
        Pages.Remove(page);
    }

    private bool IsDatabaseOpened => !string.IsNullOrWhiteSpace(DatabasePath);

    [RelayCommand(CanExecute = nameof(IsDatabaseOpened))]
    private async Task MakeNewIssue()
    {
        //var page = new IssuePageViewModel(ClosePageRequested);
        //Pages.Add(page);
        //SelectedPage = page;
        try
        {
            var vm = new CreateIssueDialogViewModel();
            var newIssueData = await vm.ShowDialog(this, "Create Issue");
            if (newIssueData is null)
                return;

            int newId = Database.MakeNewId();
            var newIssue = new Issue(newId, newIssueData.Title, newIssueData.Details);

            Database.NewIssueCreated(newIssue);
            TablePage.NewIssueCreated(newIssue);

            UpdateIssueCounters();

            DialogManager.ShowInfo($"New issue #{newIssue.Id} created");
        }
        catch (Exception e)
        {
            DialogManager.ShowError(e.Message, "Failed to create issue");
        }
    }
}

public class DesignMainWindowViewModel : MainWindowViewModel
{
    public DesignMainWindowViewModel() : base()
    {
        Pages.Add(new DesignTablePageViewModel());
        Pages.Add(new DesignIssuePageViewModel());
        Pages.Add(new DesignIssuePageViewModel());

        TotalIssueCount = 255;
        OpenedIssueCount = 21;
        ShownIssueCount = 247;
        DatabasePath = "/home/user/Adeptus/databases/Demo.adeptus";
        DatabaseName = "Demo.adeptus";
    }
}
