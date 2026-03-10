using Adeptus.Services;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace Adeptus.ViewModels;

public abstract partial class DialogViewModel<TResult> : ViewModelBase
{
    protected abstract TResult GetResult();

    protected virtual bool CanAccept() => true;

    [RelayCommand(CanExecute = nameof(CanAccept))]
    protected void Accept()
    {
        DialogManager.AcceptDialog(this, GetResult());
    }

    [RelayCommand]
    protected void Cancel()
    {
        DialogManager.CancelDialog(this);
    }

    public async Task<TResult?> ShowDialog(object context, string title)
    {
        return await DialogManager.ShowDialog<TResult>(context, title, this);
    }
}
