using Adeptus.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Adeptus.Views;

public partial class TablePageView : UserControl
{
    public TablePageView()
    {
        InitializeComponent();
    }

    private void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Only trigger when double-tapping a data row, not the header
        if (e.Source is not Control control)
            return;
        if (control.FindAncestorOfType<DataGridRow>() is null)
            return;

        if (DataContext is TablePageViewModel vm && vm.ShowSelectedIssueCommand.CanExecute(null))
            vm.ShowSelectedIssueCommand.Execute(null);
    }
}
