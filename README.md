# Gato

Gato is a Windows desktop utility for adjusting display gamma through the Windows display gamma ramp. It is a modern rewrite inspired by the original Gapa application.

## Features

- Adjust gamma, brightness, and contrast for each detected monitor.
- Work with linked RGB values or tune the red, green, and blue channels independently.
- Enable or disable correction for individual monitors.
- Preview the current tone curve and LUT in the interface.
- Save and switch between profiles, and assign a keyboard shortcut to a profile.
- Keep the app available from the notification area (system tray), switch profiles there, reset all monitors, or exit.
- Optionally start Gato when you sign in to Windows.

## Requirements

- Windows 10 or later with a compatible display driver.
- The **.NET 10 Desktop Runtime** (x64) to run a framework-dependent release. The Desktop Runtime includes the components required by WPF. Download it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0).

Gato changes the software gamma ramp exposed by Windows; it does not change the monitor's hardware calibration or OSD settings. Whether a display accepts gamma-ramp changes depends on the graphics driver and display configuration.

## Run Gato

1. Download and extract a release package.
2. Keep the extracted folder in a location where your Windows account can write files.
3. Run `Gato.exe`.

On first launch, Gato creates `config.json` beside the executable. Profiles and preferences are stored there, so keep this file if you want to preserve your settings when moving or replacing the application.

## Quick start

1. Select a monitor and enable its correction.
2. Adjust gamma, brightness, and contrast. Use linked RGB for the same adjustment on all channels, or switch to independent channels for color-specific correction.
3. Create or select a profile to save different settings for later use.
4. To rename a profile, edit its name in the profile name field; the change is saved when the field loses focus or you press Enter.
5. Use **Set Hotkey** to record a profile shortcut. A shortcut must include Ctrl, Alt, or Shift along with a key.

Use **Hide**, the window's minimize button, or the close button to hide Gato in the system tray. Left-click the tray icon to reopen the window. Right-click it to open the menu, switch profiles, reset all monitors, or exit Gato. The app continues running in the tray until you choose **Exit**.

## Build from source

### Prerequisites

- Windows
- .NET 10 SDK
- Visual Studio with the **.NET desktop development** workload (optional if using the .NET CLI)

Open `Gato/Gato.slnx` from this directory in Visual Studio, select the `Gato` startup project, and build or run the desired configuration.

To publish a framework-dependent, single-file x64 build from the repository root:

```powershell
dotnet publish src/Gato/Gato.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o build/
```

The target computer must have the .NET 10 Desktop Runtime x64 installed. Keep the generated configuration file outside the distributable package when preparing a clean release; users' `config.json` files contain their own settings.

## Configuration and support

`config.json` is stored next to `Gato.exe`. Back it up before replacing an existing installation. A folder that Windows protects from application writes, such as `Program Files`, may prevent Gato from saving its configuration unless write access is provided.

Gato relies on the Windows display gamma-ramp API. Some graphics drivers, remote desktop sessions, or virtual displays may not support these adjustments consistently.

## Third-party components

Gato uses third-party NuGet libraries and includes components identified by those libraries' notices. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for package versions, upstream links, and license information. Preserve upstream notices when redistributing Gato.

## License

Gato is licensed under the [MIT License](LICENSE).
