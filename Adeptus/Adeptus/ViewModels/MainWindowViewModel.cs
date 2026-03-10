using Adeptus.Models;
using Adeptus.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Adeptus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<PageViewModel> Pages { get; } = [];

    [ObservableProperty]
    public partial PageViewModel? SelectedPage { get; set; }

    [ObservableProperty]
    public partial int TotalIssues { get; protected set; }

    [ObservableProperty]
    public partial int OpenedIssues { get; protected set; }

    [ObservableProperty]
    public partial int ShownIssues { get; protected set; }

    [ObservableProperty]
    public partial string DatabaseFilePath { get; protected set; } = string.Empty;

    [ObservableProperty]
    public partial string DatabaseFileName { get; protected set; } = string.Empty;

    [ObservableProperty]
    public partial string WindowTitle { get; protected set; } = "Adeptus";

    private TablePageViewModel TablePage
    {
        get
        {
            if (Pages.Count == 0)
                throw new Exception("Database is not opened");
            if (Pages.First() is not TablePageViewModel vm)
                throw new Exception("Invalid pages structure");
            return vm;
        }
    }

    [RelayCommand]
    private async Task CreateDatabase()
    {
        try
        {
            var filePath = await DialogManager.SaveFileDialog(this, "Create Database");
            if (filePath is null)
                return;

            await LoadDatabase(filePath);
        }
        catch (Exception e)
        {
            ResetDatabase();
            DialogManager.ShowError(e.Message, "Failed to create database");
        }
    }

    [RelayCommand]
    private async Task OpenDatabase()
    {
        try
        {
            var filePaths = await DialogManager.OpenFileDialog(this, "Open Database");
            if (filePaths is null)
                return;

            var filePath = filePaths.First();

            await LoadDatabase(filePath);
        }
        catch (Exception e)
        {
            ResetDatabase();
            DialogManager.ShowError(e.Message, "Failed to open database");
        }
    }

    [RelayCommand]
    private async Task DemoLoadDatabase()
    {
        try
        {
            await LoadDatabase("demo.adeptus");
        }
        catch (Exception e)
        {
            ResetDatabase();
            DialogManager.ShowError(e.Message, "Failed to load database");
        }
    }

    private void ResetDatabase()
    {
        Pages.Clear();
        DatabaseFilePath = string.Empty;
        DatabaseFileName = string.Empty;
        WindowTitle = "Adeptus";
        TotalIssues = 0;
        ShownIssues = 0;
        OpenedIssues = 0;
    }

    private async Task LoadDatabase(string filePath)
    {
        ResetDatabase();

        DatabaseFilePath = filePath;
        DatabaseFileName = Path.GetFileName(filePath);
        WindowTitle = $"{DatabaseFileName} - Adeptus";

        var tablePage = new TablePageViewModel();
        tablePage.OnIssuesLoaded += UpdateIssueCounters;
        Pages.Add(tablePage);

        AppDbContext.Migrate(filePath);

        await tablePage.LoadIssues(filePath);

        DialogManager.ShowInfo(DatabaseFileName, "Data loaded");
    }

    private void UpdateIssueCounters()
    {
        var issues = TablePage.Issues;
        var opened = 0;
        foreach (var issue in issues)
        {
            if (!issue.IsDone)
                opened++;
        }
        TotalIssues = issues.Count;
        ShownIssues = issues.Count;
        OpenedIssues = opened;
    }

    [RelayCommand]
    private async Task DemoShowInputDialog()
    {
        var dialogViewModel = new InputDialogViewModel("Type some text:", "");
        var text = await DialogManager.ShowDialog<string?>(this, "Text Input", dialogViewModel);
        DialogManager.ShowInfo(string.IsNullOrEmpty(text) ? "Dialog was canceled" : $"The text entered: \"{text}\"");
    }

    [RelayCommand]
    private static void DemoShowInformation()
    {
        DialogManager.ShowInfo("Information", "This is information.");
    }

    [RelayCommand]
    private static void DemoShowError()
    {
        DialogManager.ShowError("Error", "Something went wrong. :-(");
    }

    [RelayCommand]
    private async Task ShowAboutDialog()
    {
    }

    private void ClosePageRequested(PageViewModel page)
    {
        Pages.Remove(page);
    }

    [RelayCommand]
    private async Task MakeNewIssue()
    {
        //var page = new IssuePageViewModel(ClosePageRequested);
        //Pages.Add(page);
        //SelectedPage = page;
        try
        {
            var vm = new CreateIssueDialogViewModel();
            var newIssueData = await vm.ShowDialog(this, "Create Issue");
            if (newIssueData != null)
            {
                using var db = new AppDbContext(DatabaseFilePath);
                var newIssue = await db.CreateIssue(newIssueData);
                TablePage.Issues.Add(newIssue);
                TablePage.SelectedIssue = newIssue;
                UpdateIssueCounters();
                DialogManager.ShowInfo($"New issue #{newIssue.Id} created");
            }
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

        TotalIssues = 255;
        OpenedIssues = 21;
        ShownIssues = 247;
        DatabaseFilePath = "/home/user/Adeptus/databases/Demo.adeptus";
        DatabaseFileName = "Demo.adeptus";
    }
}
