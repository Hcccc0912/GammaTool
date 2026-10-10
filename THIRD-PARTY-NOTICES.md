# Third-Party Components

This file records third-party libraries and components used by Gato. Gato's [MIT License](LICENSE) applies to Gato's own code; it does not replace the licenses or notices for these dependencies.

## NuGet dependencies

| Package | Version | Relationship | License | Upstream |
| --- | --- | --- | --- | --- |
| CommunityToolkit.Mvvm | 8.4.2 | Direct | MIT | [Repository](https://github.com/CommunityToolkit/dotnet) · [License](https://github.com/CommunityToolkit/dotnet/blob/8.4.2/License.md) |
| WPF-UI | 4.3.0 | Direct | MIT | [Repository](https://github.com/lepoco/wpfui) · [License](https://github.com/lepoco/wpfui/blob/4.3.0/LICENSE.md) |
| WPF-UI.Tray | 4.3.0 | Direct | MIT | [Repository](https://github.com/lepoco/wpfui) · [License](https://github.com/lepoco/wpfui/blob/4.3.0/LICENSE.md) |
| WPF-UI.Abstractions | 4.3.0 | Transitive (required by WPF-UI) | MIT | [Repository](https://github.com/lepoco/wpfui) · [License](https://github.com/lepoco/wpfui/blob/4.3.0/LICENSE.md) |

## Components listed by WPF-UI

The WPF-UI 4.3.0 package's `ThirdPartyNotices.txt` identifies these components:

| Component | Version | License / terms | Upstream |
| --- | --- | --- | --- |
| VirtualizingWrapPanel | 2.0.6 | MIT | [Repository](https://github.com/sbaeumlisberger/VirtualizingWrapPanel) |
| Fluent UI System Icons | 1.1.242 | MIT | [Repository](https://github.com/microsoft/fluentui-system-icons) |
| Windows Presentation Foundation (WPF) | 8.0 | MIT | [Repository](https://github.com/dotnet/wpf) |
| Microsoft.UI.Xaml | 3.0 | MIT | [Repository](https://github.com/microsoft/microsoft-ui-xaml) |
| Segoe Fluent Icons font | 3.0 | Microsoft font terms; see the package notice and [Microsoft's documentation](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font) | [Documentation](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font) |

The package's notices and license files are included in [`ThirdPartyNotices/`](ThirdPartyNotices/): the CommunityToolkit.Mvvm 8.4.2 license and notices, and the WPF-UI 4.3.0 license and notices (also covering WPF-UI.Tray and WPF-UI.Abstractions, which share the same upstream project and MIT license). These are copies of the files in the restored NuGet packages. Review them before redistributing binaries, especially the Segoe Fluent Icons font terms.

## Platform components

Gato targets `net10.0-windows` and uses WPF and Windows display APIs. The .NET Desktop Runtime and Windows platform components are supplied by Microsoft and are not source files maintained in this repository. Refer to the license terms that accompany the runtime and Windows installation.

## Upstream notices

The CommunityToolkit.Mvvm package's notice also lists components and code contributions associated with the broader upstream project, including components that may not be part of Gato's runtime dependency graph. The copied upstream notice preserves those attributions; consult it and the corresponding exact package version when redistributing.
