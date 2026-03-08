using System;

namespace Adeptus.ViewModels;

public class IssuePageViewModel : PageViewModel
{
    private static int _prevPageIndex = 0;

    private readonly int _pageIndex = ++_prevPageIndex;

    public IssuePageViewModel(Action<PageViewModel>? closeAction = null) : base(closeAction)
    {
    }

    public string Header
    {
        get => $"[#{_pageIndex}] Issue";
    }
}
