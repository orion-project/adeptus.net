using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Templates;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    private static Dictionary<string, DialogGeometry>? _dialogGeometries;

    private static string GetDialogGeometryPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "orion-project.org",
        "Adeptus",
        "dialog-geometry.json");

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

    private sealed class DialogGeometry
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
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

        RestoreDialogGeometry(dialog, content, ownerWindow);

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

        SaveDialogGeometry(dialog);

        dialog.Close(result);
    }

    private static string GetDialogKey(object? dialogContent)
    {
        var type = dialogContent?.GetType();
        return type?.FullName ?? type?.Name ?? "UnknownDialog";
    }

    private static void RestoreDialogGeometry(Window dialog, object dialogContent, Window ownerWindow)
    {
        var dialogGeometries = GetDialogGeometries();

        var dialogKey = GetDialogKey(dialogContent);
        if (!dialogGeometries.TryGetValue(dialogKey, out var geometry))
        {
            return;
        }

        if (geometry.Width <= 0 || geometry.Height <= 0)
        {
            return;
        }

        if (!IsGeometryValidForCurrentScreens(ownerWindow, geometry))
        {
            return;
        }

        dialog.SizeToContent = SizeToContent.Manual;
        dialog.Width = geometry.Width;
        dialog.Height = geometry.Height;
        dialog.WindowStartupLocation = WindowStartupLocation.Manual;
        dialog.Position = new PixelPoint((int)Math.Round(geometry.X), (int)Math.Round(geometry.Y));
    }

    private static bool IsGeometryValidForCurrentScreens(Window ownerWindow, DialogGeometry geometry)
    {
        if (!double.IsFinite(geometry.X) || !double.IsFinite(geometry.Y) ||
            !double.IsFinite(geometry.Width) || !double.IsFinite(geometry.Height))
        {
            return false;
        }

        var screens = ownerWindow.Screens.All;
        if (screens.Count == 0)
        {
            return true;
        }

        var geometryLeft = geometry.X;
        var geometryTop = geometry.Y;
        var geometryRight = geometry.X + geometry.Width;
        var geometryBottom = geometry.Y + geometry.Height;

        foreach (var screen in screens)
        {
            var area = screen.WorkingArea;

            if (geometry.Width > area.Width || geometry.Height > area.Height)
            {
                continue;
            }

            var intersects = geometryRight > area.X &&
                             geometryLeft < area.X + area.Width &&
                             geometryBottom > area.Y &&
                             geometryTop < area.Y + area.Height;

            if (intersects)
            {
                return true;
            }
        }

        return false;
    }

    private static void SaveDialogGeometry(Window dialog)
    {
        var bounds = dialog.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var dialogGeometries = GetDialogGeometries();

        dialogGeometries[GetDialogKey(dialog.Content)] = new DialogGeometry
        {
            Width = bounds.Width,
            Height = bounds.Height,
            X = dialog.Position.X,
            Y = dialog.Position.Y,
        };

        var path = GetDialogGeometryPath();
        try
        {
            var directory = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Could not resolve dialog geometry directory path.");

            Directory.CreateDirectory(directory);

            File.WriteAllText(path,
                JsonSerializer.Serialize(dialogGeometries, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Do not block dialog closing if geometry persistence fails.
        }
    }

    private static Dictionary<string, DialogGeometry> GetDialogGeometries()
    {
        if (_dialogGeometries is not null)
        {
            return _dialogGeometries;
        }

        _dialogGeometries = new Dictionary<string, DialogGeometry>(StringComparer.Ordinal);

        var path = GetDialogGeometryPath();
        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<Dictionary<string, DialogGeometry>>(File.ReadAllText(path));
                if (loaded is not null)
                {
                    foreach (var item in loaded)
                    {
                        _dialogGeometries[item.Key] = item.Value;
                    }
                }
            }
            catch
            {
                // Ignore invalid/corrupted geometry file and continue with defaults.
            }
        }

        return _dialogGeometries;
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
