using System;

namespace Adeptus.ViewModels;

public class IssuePageViewModel : PageViewModel
{
    private static int _prevPageIndex = 0;

    private readonly int _pageIndex = ++_prevPageIndex;

    public IssuePageViewModel(Action<PageViewModel> closeAction) : base(closeAction)
    {
    }

    public string Header
    {
        get => $"[#{_pageIndex}] Issue";
    }
}

public class DesignIssuePageViewModel : IssuePageViewModel
{
    public DesignIssuePageViewModel() : base(_ => { })
    {
    }
}
