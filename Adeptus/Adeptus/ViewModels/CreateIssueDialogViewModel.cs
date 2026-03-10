using Adeptus.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Adeptus.ViewModels;

public partial class CreateIssueDialogViewModel : DialogViewModel<IssueCreateData>
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    protected override IssueCreateData GetResult() => new(Title, Description);

    protected override bool CanAccept() => !string.IsNullOrWhiteSpace(Title);
}

