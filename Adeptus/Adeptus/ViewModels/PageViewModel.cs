using CommunityToolkit.Mvvm.Input;
using System;

namespace Adeptus.ViewModels;

public abstract partial class PageViewModel(Action<PageViewModel>? closeAction = null) : ViewModelBase
{
    private readonly Action<PageViewModel>? _closeAction = closeAction;

    [RelayCommand]
    private void Close()
    {
        _closeAction?.Invoke(this);
    }
}
