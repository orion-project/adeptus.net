using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Templates;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adeptus.Services;

/// <summary>
/// Keeps a correlation between View (as <see cref="Visual"/>) and ViewModels in runtime.
/// A view associates itself with a view model via the Register attached property.
/// See in MainWindow.axaml: `services:DialogManager.Register="{Binding}"`
/// When a view model wants to show a dialog, it queries a <see cref="Visual"/> 
/// associated with it via the <see cref="GetVisualForContext"/> method.
/// </summary>
public class DialogManager
{
    private static readonly Dictionary<object, Visual> RegistrationMapper = [];

    /// <summary>
    /// This property handles the registration of Views and ViewModel
    /// </summary>
    public static readonly AttachedProperty<object> RegisterProperty =
        AvaloniaProperty.RegisterAttached<DialogManager, Visual, object>("Register");

    public static WindowNotificationManager? NotificationManager { get; set; }

    static DialogManager()
    {
        // Add a listener to changes of the Register attached property
        RegisterProperty.Changed.AddClassHandler<Visual>((sender, e) => {
            ArgumentNullException.ThrowIfNull(sender);
            if (e.GetOldValue<object>() is { } oldValue)
            {
                RegistrationMapper.Remove(oldValue);
            }
            if (e.GetNewValue<object>() is { } newValue)
            {
                RegistrationMapper.Add(newValue, sender);
            }
        });
    }

    /// <summary>
    /// Accessor for attached property <see cref="RegisterProperty"/>.
    /// Required to make it accessible from MainWindow.axaml
    /// </summary>
    public static void SetRegister(AvaloniaObject element, object value)
    {
        element.SetValue(RegisterProperty, value);
    }

    /// <summary>
    /// Accessor for attached property <see cref="RegisterProperty"/>.
    /// Required to make it accessible from MainWindow.axaml
    /// </summary>
    public static object GetRegister(AvaloniaObject element)
    {
        return element.GetValue(RegisterProperty);
    }

    /// <summary>
    /// Returns the registered <see cref="Visual"/> for a given context or null, if none was registered
    /// </summary>
    private static Visual? GetVisualForContext(object context)
    {
        return RegistrationMapper.GetValueOrDefault(context);
    }

    /// <summary>
    /// Returns the parent <see cref="TopLevel"/> registered for the given context or null, if no TopLevel was found.
    /// </summary>
    private static TopLevel? GetTopLevelForContext(object context)
    {
        return TopLevel.GetTopLevel(GetVisualForContext(context));
    }

    /// <summary>
    /// Shows an open file dialog for a registered context, most likely a ViewModel,
    /// and returns a list of selected files or null, if the dialog has been canceled.
    /// </summary>
    public static async Task<IEnumerable<string>?> OpenFileDialog(
        object? context, string? title = null, bool selectMany = false)
    {
        ArgumentNullException.ThrowIfNull(context);

        var topLevel = GetTopLevelForContext(context)
            ?? throw new InvalidOperationException("No TopLevel was resolved for the given context.");

        var selectedFiles = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                AllowMultiple = selectMany,
                Title = title ?? "Open Database",
                FileTypeFilter =
                [
                    new("Adeptus databases") { Patterns = ["*.adeptus"] },
                    new("All files") { Patterns = ["*.*"] },
                ],
            });

        return selectedFiles.Select(s => s.Path.LocalPath);
    }

    /// <summary>
    /// Shows a save file dialog for a registered context, most likely a ViewModel,
    /// and returns a selected filename or null, if the dialog has been canceled.
    /// </summary>
    public static async Task<string?> SaveFileDialog(object? context, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var topLevel = GetTopLevelForContext(context)
            ?? throw new InvalidOperationException("No TopLevel was resolved for the given context.");

        var selectedFile = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions()
            {
                Title = title ?? "Create Database",
                FileTypeChoices =
                [
                    new("Adeptus databases") { Patterns = ["*.adeptus"] },
                ]
            });

        return selectedFile?.Path.LocalPath;
    }

    /// <summary>
    /// Shows a dialog window for a given context
    /// </summary>
    /// <param name="context">The context to use</param>
    /// <param name="windowTitle">The dialog's window title</param>
    /// <param name="content">The content to show</param>
    /// <param name="contentTemplate">Optional: An <see cref="IDataTemplate"/> to represnet the <see cref="content"/></param>
    /// <typeparam name="T">The expected type to return</typeparam>
    /// <returns>The result or null if dialog was canceled</returns>
    /// <exception cref="InvalidOperationException">The dialog window can only be shown if the app is a desktop app.</exception>
    public static async Task<T?> ShowDialog<T>(
        object? context, string windowTitle, object content, IDataTemplate? contentTemplate = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ownerWindow = GetTopLevelForContext(context) as Window
            ?? throw new InvalidOperationException("The method ShowDialogWindow can only be used on a Window");

        var dialog = new Window()
        {
            Title = windowTitle,
            Content = content,
            ContentTemplate = contentTemplate,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        // TODO: store/restore dialog size

        return await dialog.ShowDialog<T>(ownerWindow);
    }

    /// <summary>
    /// Closes a dialog window for the given context with the given result
    /// </summary>
    public static void AcceptDialog(object? context, object? result)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dialog = GetTopLevelForContext(context) as Window
            ?? throw new InvalidOperationException("The method AcceptDialog can only be used on a Window");

        dialog.Close(result);
    }

    /// <summary>
    /// Closes a dialog window for the given context without a result
    /// </summary>
    public static void CancelDialog(object? context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dialog = GetTopLevelForContext(context) as Window
            ?? throw new InvalidOperationException("The method CancelDialog can only be used on a Window");

        dialog.Close();
    }

    /// <summary>
    /// Shows an informational pop-up notification
    /// </summary>
    public static void ShowInfo(string message, string? title = null)
    {
        ShowNotificationMessage(message, title ?? "Information", NotificationType.Information);
    }

    /// <summary>
    /// Shows an error pop-up notification
    /// </summary>
    public static void ShowError(string message, string? title = null)
    {
        ShowNotificationMessage(message, title ?? "Error", NotificationType.Error, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Adds a notification to the <see cref="WindowNotificationManager"/>.
    /// </summary>
    private static void ShowNotificationMessage(
        string message, string title, NotificationType notificationType, TimeSpan? expiration = null)
    {
        var notificationManager = NotificationManager
            ?? throw new InvalidOperationException("WindowNotificationManager is not provided");

        notificationManager.Show(
            new Notification(title, message, notificationType, expiration ?? TimeSpan.FromSeconds(5)));
    }
}
