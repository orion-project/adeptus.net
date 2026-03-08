using Adeptus.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Adeptus.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TablePageViewModel _tablePageViewModel = new();

    public ObservableCollection<PageViewModel> Pages { get; } = [];

    public ObservableCollection<string> Results { get; } = [];

    [ObservableProperty]
    public partial PageViewModel? SelectedPage { get; set; }

    public MainWindowViewModel()
    {
        Pages.Add(_tablePageViewModel);
    }

    [RelayCommand]
    private async Task OpenDatabase()
    {
        await _tablePageViewModel.LoadIssues();

        var results = await DialogManager.OpenFileDialog(this, "Select some files");

        if (results is null)
            return;

        foreach (var result in results)
        {
            Results.Insert(0, $"file added: {result}");
        }
    }

    [RelayCommand]
    private async Task DemoLoadDatabase()
    {
        await _tablePageViewModel.LoadIssues();
    }

    [RelayCommand]
    private async Task DemoShowInputDialog()
    {
        var dialogViewModel = new InputDialogViewModel("Type some text:", "");
        var text = await DialogManager.ShowDialogWindow<string?>(this, "Text Input", dialogViewModel);
        Results.Add(string.IsNullOrEmpty(text) ? "Dialog was canceled" : $"The text entered: \"{text}\"");
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
