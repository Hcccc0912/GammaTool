using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Gato.Interop;
using Gato.Models;
using Gato.Services;
using Gato.ViewModels;
using Gato.Views;
using Wpf.Ui.Tray.Controls;

namespace Gato;

public partial class App : Application
{
    private const string MutexName = "Gato_SingleInstance_Mutex";
    private Mutex? _instanceMutex;
    private bool _ownsMutex;
    private HotkeyService? _hotkeyService;
    private MainViewModel? _viewModel;
    private MainWindow? _mainWindow;
    private NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out _ownsMutex);
        if (!_ownsMutex)
        {
            ActivateExistingWindow();
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        var startupStage = "Initialize configuration service";
        try
        {
            var configService = new ConfigService();
            startupStage = "Detect monitors and load configuration";
            var monitorService = new MonitorService();
            var gammaRampService = new GammaRampService();
            _hotkeyService = new HotkeyService();
            _viewModel = new MainViewModel(configService, monitorService, gammaRampService, _hotkeyService);
            _viewModel.Profiles.CollectionChanged += (_, _) =>
            {
                if (_trayIcon is not null) _trayIcon.Menu = BuildTrayMenu();
            };
            startupStage = "Read startup settings";
            var autoStartService = new AutoStartService();
            _viewModel.Config.LaunchOnStartup = autoStartService.IsEnabled();
            _viewModel.SaveSelectedProfile();

            startupStage = "Create main window";
            _mainWindow = new MainWindow(_viewModel);
            MainWindow = _mainWindow;
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            startupStage = "Apply profile and register hotkeys";
            _viewModel.InitializeSelectedProfile();
            if (!_viewModel.RegisterSavedHotkeys())
                MessageDialog.Show("Some profile hotkeys could not be registered. They may conflict with another application.");

            var startSilently = e.Args.Contains("--silent", StringComparer.OrdinalIgnoreCase);
            // Create a PresentationSource without exposing a partially initialized
            // first frame. WPF-UI.Tray needs the shown window to register its icon.
            _mainWindow.Opacity = 0;
            _mainWindow.ShowInTaskbar = false;

            // WPF-UI.Tray resolves its parent source from Application.MainWindow.
            // Show() creates that PresentationSource; EnsureHandle() alone does not.
            _mainWindow.Show();

            startupStage = "Initialize system tray icon";
            InitializeTrayIcon();

            if (startSilently)
            {
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.UpdateLayout();
                _mainWindow.Opacity = 1;
                _mainWindow.ShowInTaskbar = true;
                _mainWindow.InvalidateVisual();
                _mainWindow.Activate();
            }

            if (startSilently)
            {
                _mainWindow.Opacity = 1;
                _mainWindow.ShowInTaskbar = true;
            }
        }
        catch (Exception exception)
        {
            MessageDialog.Show($"Gato failed to start (stage: {startupStage})\n\n{exception}");
            if (_mainWindow is not null) _mainWindow.AllowApplicationClose = true;
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Unregister();
        }
        _hotkeyService?.Dispose();
        if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void InitializeTrayIcon()
    {
        if (_mainWindow is null || _viewModel is null) return;
        // WPF-UI's tray icon is a FrameworkElement and must belong to a window's
        // visual tree. Its tray manager also needs the shown main window's source.
        _trayIcon = _mainWindow.GatoTrayIcon;
        if (PresentationSource.FromVisual(_mainWindow) is not HwndSource)
            throw new InvalidOperationException("The main window has no WPF presentation source for the tray icon.");
        _trayIcon.Menu = BuildTrayMenu();
#pragma warning disable CS8622 // WPF-UI.Tray event delegate nullability does not match the WPF routed event signature.
        _trayIcon.LeftClick += OnTrayIconClick;
        _trayIcon.LeftDoubleClick += OnTrayIconClick;
#pragma warning restore CS8622
        _trayIcon.Register();
        if (!_trayIcon.IsRegistered)
            throw new InvalidOperationException("Windows did not register the Gato notification area icon.");
    }

    private void OnTrayIconClick(object? sender, RoutedEventArgs e) => ShowMainWindow();

    private ContextMenu BuildTrayMenu()
    {
        var menu = new ContextMenu();
        if (_viewModel is null) return menu;

        foreach (var profile in _viewModel.Profiles)
        {
            var item = new MenuItem { Header = profile.Name, Tag = profile.Id };
            item.Click += (_, _) =>
            {
                var target = _viewModel.Profiles.FirstOrDefault(p => p.Id == (int)item.Tag);
                if (target is not null) _viewModel.SelectedProfile = target;
            };
            menu.Items.Add(item);
        }

        if (menu.Items.Count > 0) menu.Items.Add(new Separator());
        var open = new MenuItem { Header = "Open Gato" };
        open.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(open);
        var reset = new MenuItem { Header = "Reset all monitors" };
        reset.Click += (_, _) => _viewModel.ResetAllMonitors();
        menu.Items.Add(reset);
        menu.Items.Add(new Separator());
        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) =>
        {
            if (_mainWindow is not null) _mainWindow.AllowApplicationClose = true;
            Shutdown();
        };
        menu.Items.Add(exit);
        return menu;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        if (!_mainWindow.IsVisible) _mainWindow.Show();
        if (_mainWindow.WindowState == WindowState.Minimized) _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private static void ActivateExistingWindow()
    {
        var handle = NativeMethods.FindWindow(null, "Gato");
        if (handle == IntPtr.Zero) return;
        NativeMethods.ShowWindow(handle, 9); // SW_RESTORE
        NativeMethods.SetForegroundWindow(handle);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageDialog.Show("Gato encountered an unhandled error and will close.");
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() => MessageDialog.Show("Gato encountered an unhandled error and will close.")));
    }
}
