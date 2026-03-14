using Adeptus.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Adeptus.ViewModels;

public partial class CreateIssueDialogViewModel : DialogViewModel<CreateIssueDialogViewModel.IssueCreateData>
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Details { get; set; }

    public record IssueCreateData(string Title, string Details);

    protected override IssueCreateData GetResult() => new(Title, Details);

    protected override bool CanAccept() => !string.IsNullOrWhiteSpace(Title);
}

