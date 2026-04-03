using Adeptus.Services;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adeptus.ViewModels;

public partial class MainWindowViewModel
{
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
    private async Task DemoOpenFileDialog()
    {
        try
        {
            IEnumerable<string> filePaths = await DialogManager.OpenFileDialog(this, null, selectMany: true);
            if (!filePaths.Any())
            {
                return;
            }

            DialogManager.ShowInfo(String.Join("\n", filePaths));
        }
        catch (Exception e)
        {
            DialogManager.ShowError(e.Message);
        }
    }

    [RelayCommand]
    private async Task DemoSaveFileDialog()
    {
        try
        {
            string? filePath = await DialogManager.SaveFileDialog(this);
            if (filePath is null)
            {
                return;
            }

            DialogManager.ShowInfo(filePath);
        }
        catch (Exception e)
        {
            DialogManager.ShowError(e.Message);
        }
    }
}
