using Adeptus.Models;
using System;

namespace Adeptus.ViewModels;

public class IssuePageViewModel : PageViewModel
{
    public Issue Issue { get; }

    public IssuePageViewModel(Issue issue, Action<PageViewModel> closeAction) : base(closeAction)
    {
        Issue = issue;
    }

    public string Header => $"#{Issue.Id} {Issue.Title}";
}

public class DesignIssuePageViewModel : IssuePageViewModel
{
    public DesignIssuePageViewModel() : base(new Issue(42, "Sample design issue"), _ => { })
    {
    }
}
