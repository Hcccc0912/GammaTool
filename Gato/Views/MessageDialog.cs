using System.Windows;
using System.Windows.Controls;

namespace Gato.Views;

internal static class MessageDialog
{
    public static void Show(string message, string title = "Gato", Window? owner = null) =>
        ShowCore(message, title, owner, confirm: false);

    public static bool Confirm(string message, string title = "Gato", Window? owner = null) =>
        ShowCore(message, title, owner, confirm: true);

    private static bool ShowCore(string message, string title, Window? owner, bool confirm)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 380,
            SizeToContent = SizeToContent.Height,
            MinHeight = 150,
            MaxWidth = 520,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            Owner = owner,
            ShowInTaskbar = false
        };

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(20, 20, 20, 18),
            MaxWidth = 480
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12, 0, 16, 16)
        };
        var result = false;

        if (confirm)
        {
            var yes = new Button { Content = "Yes", MinWidth = 80, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            yes.Click += (_, _) => { result = true; dialog.DialogResult = true; };
            buttons.Children.Add(yes);
        }

        var close = new Button { Content = confirm ? "No" : "OK", MinWidth = 80 };
        close.Click += (_, _) =>
        {
            if (confirm) dialog.DialogResult = false;
            else dialog.Close();
        };
        buttons.Children.Add(close);

        var content = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        content.Children.Add(buttons);
        content.Children.Add(text);
        dialog.Content = content;
        dialog.ShowDialog();
        return result;
    }
}
