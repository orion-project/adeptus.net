using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Adeptus.ViewModels;

public partial class InputDialogViewModel(string prompt = "Input text:", string? defaultValue = null) : DialogViewModel<string>
{

    /// <summary>
    /// Gets or sets the prompt text to display
    /// </summary>
    [ObservableProperty]
    public partial string PromptText { get; set; } = prompt;

    /// <summary>
    /// Gets or sets the text that the user has entered
    /// </summary>
    [ObservableProperty]
    [Required]
    [NotifyCanExecuteChangedFor(nameof(AcceptCommand))]
    public partial string InputText { get; set; } = defaultValue ?? string.Empty;

    protected override bool CanAccept() => !string.IsNullOrEmpty(InputText);

    protected override string GetResult() => InputText.Trim();
}
