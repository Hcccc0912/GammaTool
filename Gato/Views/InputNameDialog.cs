using System.Windows;
using System.Windows.Controls;

namespace Gato.Views;

internal sealed class InputNameDialog : Window
{
    private readonly TextBox _nameBox;
    public string ProfileName => _nameBox.Text.Trim();

    public InputNameDialog(string initialName)
    {
        Title = "Save Profile As";
        Width = 360;
        Height = 150;
        MinWidth = 360;
        MinHeight = 150;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _nameBox = new TextBox { Text = initialName, Margin = new Thickness(0, 8, 0, 12) };
        var save = new Button { Content = "Save", MinWidth = 76, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "Cancel", MinWidth = 76, IsCancel = true };
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageDialog.Show("Profile name cannot be empty.", owner: this);
                return;
            }
            DialogResult = true;
        };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(save);
        buttons.Children.Add(cancel);
        var content = new StackPanel { Margin = new Thickness(16) };
        content.Children.Add(new TextBlock { Text = "Profile name" });
        content.Children.Add(_nameBox);
        content.Children.Add(buttons);
        Content = content;
        Loaded += (_, _) => { _nameBox.Focus(); _nameBox.SelectAll(); };
    }
}
