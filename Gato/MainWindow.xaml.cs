using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using Gato.Models;
using Gato.Services;
using Gato.ViewModels;
using Gato.Views;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Gato;

public partial class MainWindow : FluentWindow
{
    public MainViewModel ViewModel { get; }
    public bool AllowApplicationClose { get; set; }
    private bool _viewModelReady;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, updateAccents: true);
        ViewModel = viewModel;
        DataContext = viewModel;
        _viewModelReady = true;
        LinkedToggle.IsChecked = viewModel.SelectedProfile?.Linked ?? true;
        ViewModel.PreviewChanged += OnPreviewChanged;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += (_, _) => SystemThemeWatcher.UnWatch(this);
        ViewModel.UpdatePreview();
    }

    private void OnPreviewChanged(ushort[][] curves, string colorCount) => Dispatcher.Invoke(() =>
    {
        LutGraph.SetCurves(curves);
        ColorCountText.Text = $"LUT: {colorCount} / 16,777,216";
    });

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedProfile))
        {
            LinkedToggle.IsChecked = ViewModel.SelectedProfile?.Linked ?? true;
            RedChannelRadio.IsChecked = true;
            ProfileNameTextBox.Text = ViewModel.SelectedProfile?.Name ?? string.Empty;
            UpdateHotkeyText();
        }
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e) => ViewModel.CreateProfile();

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProfile is null) return;
        var dialog = new InputNameDialog(ViewModel.SelectedProfile.Name) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        if (ViewModel.SaveAs(dialog.ProfileName) is null)
            MessageDialog.Show("Profile name cannot be empty.", owner: this);
    }

    private void ProfileName_LostFocus(object sender, RoutedEventArgs e) => CommitProfileName();

    private void ProfileName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        CommitProfileName();
        Keyboard.ClearFocus();
    }

    private void CommitProfileName()
    {
        if (!_viewModelReady) return;
        if (!ViewModel.RenameSelectedProfile(ProfileNameTextBox.Text))
        {
            ProfileNameTextBox.Text = ViewModel.SelectedProfile?.Name ?? string.Empty;
            return;
        }
        ProfileNameTextBox.Text = ViewModel.SelectedProfile?.Name ?? string.Empty;
        ProfileListBox.Items.Refresh();
    }

    private void DeleteProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProfile is null) return;
        if (MessageDialog.Confirm($"Delete profile '{ViewModel.SelectedProfile.Name}'?", owner: this))
            ViewModel.DeleteSelectedProfile();
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => ViewModel.ResetSelectedChannel();

    private void Linked_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded && LinkedToggle.IsChecked is bool isLinked)
            ViewModel.SetLinked(isLinked);
    }

    private void Channel_Checked(object sender, RoutedEventArgs e)
    {
        if (!_viewModelReady || sender is not System.Windows.Controls.RadioButton { IsChecked: true } radio)
            return;
        ViewModel.SelectedChannel = radio.Tag?.ToString() ?? "Red";
    }

    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            Hide();
    }

    private void StartupSwitch_Changed(object sender, RoutedEventArgs e)
    {
        if (!_updatingStartupSwitch && IsLoaded && LaunchOnStartupSwitch.IsChecked is bool enabled)
        {
            try
            {
                new AutoStartService().SetEnabled(enabled);
                ViewModel.UpdateLaunchOnStartup(enabled);
            }
            catch (Exception exception)
            {
                MessageDialog.Show($"Could not update startup settings: {exception.Message}", owner: this);
                _updatingStartupSwitch = true;
                LaunchOnStartupSwitch.IsChecked = !enabled;
                _updatingStartupSwitch = false;
            }
        }
    }

    private bool _recordingHotkey;
    private bool _updatingStartupSwitch;

    private void Hotkey_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _recordingHotkey = true;
        HotkeyTextBox.Text = "Press a key combination (Esc to cancel)";
        HotkeyTextBox.Focus();
    }

    private void Hotkey_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_recordingHotkey) return;
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            _recordingHotkey = false;
            UpdateHotkeyText();
            return;
        }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            HotkeyTextBox.Text = "Press a key combination";
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var requiredModifiers = modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift);
        if (requiredModifiers == ModifierKeys.None)
        {
            MessageDialog.Show("Hold at least one modifier key: Ctrl, Alt, or Shift.", owner: this);
            return;
        }

        _recordingHotkey = false;
        if (!ViewModel.SetHotkey(ViewModel.SelectedProfile?.Id ?? 0, modifiers, key))
            MessageDialog.Show("This hotkey is unavailable or conflicts with another application. Choose another combination.", owner: this);
        UpdateHotkeyText();
    }

    private void Hotkey_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (!_recordingHotkey) return;
        if (Keyboard.Modifiers == ModifierKeys.None)
        {
            _recordingHotkey = false;
            UpdateHotkeyText();
        }
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProfile is not { } profile) return;
        ViewModel.ClearHotkey(profile.Id);
        UpdateHotkeyText();
    }

    private void UpdateHotkeyText()
    {
        var binding = ViewModel.SelectedProfile?.Hotkey;
        HotkeyTextBox.Text = binding is null ? "Set hotkey" : $"{binding.Modifiers}+{binding.Key}";
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!AllowApplicationClose)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
