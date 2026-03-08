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
            DialogManager.ShowError(e.Message, "Failed to load database");
        }
    }

    private async Task LoadDatabase(string filePath)
    {
        AppDbContext.Migrate(filePath);

        var tablePage = new TablePageViewModel();
        var stats = await tablePage.LoadIssues(filePath);

        TotalIssues = stats.Total;
        OpenedIssues = stats.Opened;
        ShownIssues = stats.Shown;
        DatabaseFilePath = filePath;
        DatabaseFileName = Path.GetFileName(filePath);
        WindowTitle = $"{DatabaseFileName} - Adeptus";

        Pages.Clear();
        Pages.Add(tablePage);

        DialogManager.ShowInfo(DatabaseFileName, "Data loaded");
    }

    [RelayCommand]
    private async Task DemoShowInputDialog()
    {
        var dialogViewModel = new InputDialogViewModel("Type some text:", "");
        var text = await DialogManager.ShowDialogWindow<string?>(this, "Text Input", dialogViewModel);
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
    private void MakeNewIssue()
    {
        var page = new IssuePageViewModel(ClosePageRequested);
        Pages.Add(page);
        SelectedPage = page;
    }
}

public class DesignMainWindowViewModel : MainWindowViewModel
{
    public DesignMainWindowViewModel() : base()
    {
        //Pages.Add(new DesignTablePageViewModel());
        //Pages.Add(new DesignIssuePageViewModel());
        //Pages.Add(new DesignIssuePageViewModel());

        TotalIssues = 255;
        OpenedIssues = 21;
        ShownIssues = 247;
        DatabaseFilePath = "/home/user/Adeptus/databases/Demo.adeptus";
        DatabaseFileName = "Demo.adeptus";
    }
}
