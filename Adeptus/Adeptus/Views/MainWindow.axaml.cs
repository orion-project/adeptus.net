using Adeptus.Services;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace Adeptus.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DialogManager.NotificationManager = new WindowNotificationManager(this)
        {
            Position = NotificationPosition.TopCenter,
        };
    }
}
