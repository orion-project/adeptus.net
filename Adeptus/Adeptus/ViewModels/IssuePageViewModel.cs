using Adeptus.Models;
using System;

namespace Adeptus.ViewModels;

public class IssuePageViewModel : PageViewModel
{
    public Issue Issue { get; }

    public string Title { get; protected set; }

    public string Details { get; protected set; }

    public DateTime Updated {  get; protected set; }

    public IssuePageViewModel(Issue issue, Action<PageViewModel> closeAction) : base(closeAction)
    {
        Issue = issue;
        Title = issue.Title;
        Details = issue.GetDetails();
        Updated = issue.Updated;
    }

    public string Header => $"#{Issue.Id} {Issue.Title}";
}

public class DesignIssuePageViewModel : IssuePageViewModel
{
    public DesignIssuePageViewModel() : base(new Issue(42, "Sample design issue"), _ => { })
    {
        Title = "Sample design issue";
        Details = """
            This is an issue description in markdown format.

            Links [Google](http://google.com)

            Code block or `inline` code:

            ```
            var foo = 42;
            ```

            ## Subsection

            And there is more text here.
            """;
        Updated = DateTime.Now;
    }
}
