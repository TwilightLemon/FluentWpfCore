# FluentWpfCore (Preview)
A WPF library providing core Fluent Design controls, materials, and visual effects.  
[README_Zh-CN.md](README_Zh-CN.md)

[<img src="https://img.shields.io/badge/license-MIT-yellow"/>](LICENSE.txt)
![C#](https://img.shields.io/badge/lang-C%23-orange)
![WPF](https://img.shields.io/badge/UI-WPF-b33bb3)
![GitHub Repo stars](https://img.shields.io/github/stars/TwilightLemon/FluentWpfCore)

## ✨ Features

### 🪟 Window Material System
- **Multiple material types** — Acrylic, Mica, MicaAlt and other modern window materials
- **Flexible combination** — combine material effects with rounded corners, shadows, and DWM animations
- **Cross-version compatibility** — supports older Windows 10 Composition APIs and the Windows 11 System Backdrop APIs
- **Dark mode support** — built-in light/dark mode switching (primarily for Mica effects)
- **Custom composition color** — customize background color and opacity for Acrylic

### 🎨 Enhanced Basic Controls
- **FluentPopup** — popup with acrylic background, rounded corners, shadow and sliding animations
- **SmoothScrollViewer** — provides smooth, fluid scrolling
- **Fluent-style templates** — modern styles and templates for Menu, ContextMenu and ToolTip

### 🔧 Low-level Capabilities
- **DWM integration** — effects are rendered via DWM; best results on Windows 11
- **WindowChrome compatibility** — works with WPF's native WindowChrome
- **Theme-agnostic** — does not force a UI style; integrates with any existing theme

> FluentWpfCore does not ship a full set of high-level UI controls. Instead, it provides low-level capabilities compatible with any theme so you can add modern visual effects without changing your existing UI style.

## 🔧 System Requirements

- Windows 10 1809 or later (some features require Windows 11)

### Feature support

| Feature | Windows 10 1809+ | Windows 11 |
|------|-----------------|------------|
| Acrylic (Composition) | ✅ | ✅ |
| Acrylic (System Backdrop) | ❌ | ✅ |
| Mica | ❌ | ✅ |
| Window corners | ❌ | ✅ |
| DWM animations | ✅ | ✅ |

### Supported .NET versions
- .NET 10.0 Windows
- .NET 8.0 Windows
- .NET 6.0 Windows
- .NET Framework 4.5 ~ 4.8

## 📦 Installation

### NuGet Package Manager
```powershell
Install-Package FluentWpfCore
```

### .NET CLI
```powershell
dotnet add package FluentWpfCore
```

### PackageReference
```xml
<PackageReference Include="FluentWpfCore" Version="1.0.0" />
```

## 📖 Usage Guide

### Window Materials

FluentWpfCore provides comprehensive window material support (Acrylic, Mica, etc.) and combinations of DWM effects. Use combinations of the following categories:

| Category | Options | Notes |
| ---- | ---- | ---- |
| Window material | Acrylic\Mica\MicaAlt | Options: dark mode, composition color, keep-acrylic-when-unfocused (Acrylic) |
| Corner style | Round\SmallRound\DoNotRound\Default |  |
| Window shadow | On\Off | Tied to corner style and DWM availability |

#### Example — creating an Acrylic window with a custom composition color, rounded corners, shadow and DWM animation, removing the native title bar and buttons:
```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:fluent="https://github.com/TwilightLemon/FluentWpfCore"
        Title="MainWindow"
        Width="800"
        Height="450"

        Background="Transparent"
        fluent:DwmAnimation.EnableDwmAnimation="True"
        fluent:WindowMaterial.WindowCorner="Round">
    
    <Window.Resources>
        <WindowChrome x:Key="windowChrome" CaptionHeight="32" />
    </Window.Resources>
    
    <fluent:WindowMaterial.Material>
        <fluent:WindowMaterial x:Name="windowMaterial"
                               CompositonColor="#CC6699FF"
                               IsDarkMode="False"
                               MaterialMode="Acrylic"
                               UseWindowComposition="True"
                               WindowChromeEx="{StaticResource windowChrome}" />
    </fluent:WindowMaterial.Material>
    
    <!--Content-->
</Window>
```

##### Property Summary

| Property | Type | Description |
|------|------|------|
| `MaterialMode` | `MaterialType` | Material type: `None`, `Acrylic`, `Mica`, `MicaAlt` |
| `IsDarkMode` | `bool` | Whether to use dark mode (relevant to Mica/MicaAlt; less pronounced for Acrylic) |
| `UseWindowComposition` | `bool` | Use window composition APIs (Windows 10 1809+; applicable to Acrylic) |
| `WindowChromeEx` | `WindowChrome` | Custom WindowChrome configuration |
| `CompositonColor` | `Color` | Composition color used by Acrylic when `UseWindowComposition=true` |

#### Window Corners

Forces Windows 11-style rounded corners on a window to override WPF or DWM defaults. Enabling rounded corners can also bring window shadow (DWM-dependent, Windows 11 only) and borders.

You can enable corners in XAML or on the native HWND in code:
```xml
<Window xmlns:fluent="https://github.com/TwilightLemon/FluentWpfCore"
        fluent:WindowMaterial.WindowCorner="Round"
        ...>
```

```csharp
using FluentWpfCore.Interop;
MaterialApis.SetWindowCorner(hwnd, corner);
```

Supported corner types:
- `Default` — system default
- `DoNotRound` — disable rounding
- `Round` — rounded corners
- `RoundSmall` — small rounded corners

Recommended scenarios:
- Use corners when relying on Acrylic with `UseWindowComposition=true` because DWM defaults may produce square corners without shadow
- Control the corner style of ToolTip, Popup and other transient windows
- Customize window border styling even when using `WindowChrome` or `AllowsTransparency`

#### DWM Animations

Enable native window animations (maximize/minimize) while removing the native title bar and buttons:

```xml
<Window xmlns:fluent="https://github.com/TwilightLemon/FluentWpfCore"
        fluent:DwmAnimation.EnableDwmAnimation="True"
        ...>
```

Note: enabling DWM animations will ignore `Window.ResizeMode`. If you need `ResizeMode="NoResize"`, set `WindowChrome.ResizeBorderThickness="0"` instead.

#### Combining Effects

You can combine the above behaviors freely. Examples:

##### Plain Mica native window
```xml
<Window Background="Transparent"
        ...>
    <fluent:WindowMaterial.Material>
        <fluent:WindowMaterial IsDarkMode="False"
                               MaterialMode="Mica"/>
    </fluent:WindowMaterial.Material>
</Window>
```

This enables Mica as the background while keeping native title bar, buttons, borders and animations.

##### Mica window with a custom title bar while preserving native animations and borders
```xml
<Window Background="Transparent"
        fluent:DwmAnimation.EnableDwmAnimation="True"
        ...>
    <Window.Resources>
        <WindowChrome x:Key="windowChrome" CaptionHeight="32" />
    </Window.Resources>
    <fluent:WindowMaterial.Material>
        <fluent:WindowMaterial IsDarkMode="False"
                               MaterialMode="Mica"
                               WindowChromeEx="{StaticResource windowChrome}"/>
    </fluent:WindowMaterial.Material>
</Window>
```

##### Keep Acrylic when unfocused, with rounded corners and shadow
```xml
<Window Background="Transparent"
        fluent:DwmAnimation.EnableDwmAnimation="True"
        fluent:WindowMaterial.WindowCorner="Round">
    <Window.Resources>
        <WindowChrome x:Key="windowChrome" CaptionHeight="32" />
    </Window.Resources>
    <fluent:WindowMaterial.Material>
        <fluent:WindowMaterial x:Name="windowMaterial"
                               CompositonColor="#CC6699FF"
                               IsDarkMode="False"
                               MaterialMode="Acrylic"
                               UseWindowComposition="True"
                               WindowChromeEx="{StaticResource windowChrome}"/>
    </fluent:WindowMaterial.Material>
</Window>
```

When `UseWindowComposition="True"` a different API path is used to enable legacy material effects on Windows 10.

##### Acrylic window with square corners and no shadow
```xml
<Window Background="Transparent"
        fluent:DwmAnimation.EnableDwmAnimation="True">
    <Window.Resources>
        <WindowChrome x:Key="windowChrome" CaptionHeight="32" />
    </Window.Resources>
    <fluent:WindowMaterial.Material>
        <fluent:WindowMaterial x:Name="windowMaterial"
                               CompositonColor="#CC6699FF"
                               IsDarkMode="False"
                               MaterialMode="Acrylic"
                               UseWindowComposition="True"
                               WindowChromeEx="{StaticResource windowChrome}"/>
    </fluent:WindowMaterial.Material>
</Window>
```

### FluentPopup

`FluentPopup` is an enhanced popup control with an acrylic background, rounded corners, shadow and custom enter/exit animations, and optional follow-window-moving behavior:

```xml
<Button x:Name="ShowPopupBtn" Content="Show Popup" />

<fluent:FluentPopup x:Name="testPopup"
                    Background="{DynamicResource PopupBackgroundColor}"
                    ExtPopupAnimation="Slide"
                    FollowWindowMoving="False"
                    Placement="Bottom"
                    WindowCorner="Round"
                    PlacementTarget="{Binding ElementName=ShowPopupBtn}"
                    StaysOpen="False">
    <Grid Width="180" Height="120">
        <TextBlock Text="Hello FluentWpfCore!"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
    </Grid>
</fluent:FluentPopup>
```

#### Properties

| Property | Type | Description |
|------|------|------|
| `Background` | `SolidColorBrush` | Popup background color (only solid colors supported) |
| `ExtPopupAnimation` | `ExPopupAnimation` | Animation type: `None`, `Slide`, `Fade` |
| `FollowWindowMoving` | `bool` | Whether the popup follows window movement |
| `WindowCorner` | `WindowCorner` | Corner style for the popup |

If you need non-solid backgrounds, keep the popup background transparent and provide custom visuals inside the popup content.

### SmoothScrollViewer

A smooth scrolling ScrollViewer that enhances the native WPF control.
- Supports mouse wheel and touchpad input
- Supports horizontal and vertical scrolling, toggleable with Shift key; native touchpad horizontal scrolling supported
- Customizable physics models for different scrolling animation effects

```xml
<fluent:SmoothScrollViewer>
    <StackPanel>
        <!-- content -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

#### Properties

| Property | Type | Default | Description |
|------|------|---------|------|
| `IsEnableSmoothScrolling` | `bool` | `true` | Enable or disable smooth scrolling animation |
| `PreferredScrollOrientation` | `Orientation` | `Vertical` | Preferred scroll direction: `Vertical` or `Horizontal` |
| `AllowTogglePreferredScrollOrientationByShiftKey` | `bool` | `true` | Allow toggling scroll orientation by holding Shift key |
| `Physics` | `IScrollPhysics` | `DefaultScrollPhysics` | Scrolling physics model that controls animation behavior |

#### Scroll Physics Models

`SmoothScrollViewer` uses a pluggable physics model through the `IScrollPhysics` interface, allowing you to customize scrolling behavior. Two built-in implementations are provided:

##### DefaultScrollPhysics (Velocity-based)

Uses velocity decay with friction for a natural momentum feel. The total scroll distance equals the input delta.

| Property | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| `Smoothness` | `double` | `0.72` | `0~1` | Scroll smoothness; higher values result in smoother, longer-lasting scrolling; lower values stop faster |

```xml
<fluent:SmoothScrollViewer>
    <fluent:SmoothScrollViewer.Physics>
        <fluent:DefaultScrollPhysics Smoothness="0.8" />
    </fluent:SmoothScrollViewer.Physics>
    <!-- content -->
</fluent:SmoothScrollViewer>
```

##### ExponentialScrollPhysics (Exponential easing)

Uses exponential decay function for smooth scrolling with a "fast start, slow end" feel.

| Property | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| `DecayRate` | `double` | `8.0` | `1~20` | Decay rate; higher values reach target position faster |
| `StopThreshold` | `double` | `0.5` | `0.1~5` | Stop threshold; scrolling stops when remaining distance is below this value |

```xml
<fluent:SmoothScrollViewer>
    <fluent:SmoothScrollViewer.Physics>
        <fluent:ExponentialScrollPhysics DecayRate="10" StopThreshold="0.5" />
    </fluent:SmoothScrollViewer.Physics>
    <!-- content -->
</fluent:SmoothScrollViewer>
```

#### Usage Examples

##### Basic usage
```xml
<fluent:SmoothScrollViewer>
    <StackPanel>
        <TextBlock Text="Item 1" Height="100" />
        <TextBlock Text="Item 2" Height="100" />
        <TextBlock Text="Item 3" Height="100" />
        <!-- more items -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

##### Configure scroll orientation
```xml
<!-- Horizontal scrolling by default -->
<fluent:SmoothScrollViewer PreferredScrollOrientation="Horizontal"
                           HorizontalScrollBarVisibility="Auto"
                           VerticalScrollBarVisibility="Disabled">
    <StackPanel Orientation="Horizontal">
        <!-- content -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

##### Toggle between vertical and horizontal with Shift key
```xml
<fluent:SmoothScrollViewer AllowTogglePreferredScrollOrientationByShiftKey="True"
                           HorizontalScrollBarVisibility="Auto"
                           VerticalScrollBarVisibility="Auto">
    <!-- Hold Shift while scrolling to switch orientation -->
    <Grid>
        <!-- content -->
    </Grid>
</fluent:SmoothScrollViewer>
```

##### Temporarily disable smooth scrolling
```xml
<fluent:SmoothScrollViewer IsEnableSmoothScrolling="False">
    <!-- Falls back to standard ScrollViewer behavior -->
    <StackPanel>
        <!-- content -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

##### Use with ItemsControl for large lists
```xml
<fluent:SmoothScrollViewer>
    <ItemsControl ItemsSource="{Binding Items}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <VirtualizingStackPanel />
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border Height="50" Background="LightGray" Margin="5">
                    <TextBlock Text="{Binding}" VerticalAlignment="Center" Margin="10" />
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</fluent:SmoothScrollViewer>
```

##### Custom physics implementation
You can implement your own physics model by implementing the `IScrollPhysics` interface:

```csharp
public class CustomScrollPhysics : IScrollPhysics
{
    public bool IsStable { get; private set; }
    
    public void OnScroll(double delta)
    {
        // Handle scroll input, delta is the scroll amount
        IsStable = false;
    }
    
    public double Update(double currentOffset, double dt)
    {
        // Calculate and return new offset based on current offset and delta time
        // Set IsStable = true when animation should stop
        return newOffset;
    }
}
```

Then apply it to the SmoothScrollViewer:
```csharp
smoothScrollViewer.Physics = new CustomScrollPhysics();
```


### Fluent-style Menu

Since menu styling involves templates and resources, include the theme resources first.

#### 1. Merge resource dictionary

In `App.xaml` merge the FluentWpfCore theme resources:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- FluentWpfCore default theme -->
            <ResourceDictionary Source="pack://application:,,,/FluentWpfCore;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
        <SolidColorBrush x:Key="ForegroundColor" Color="#FF0E0E0E" />
        
        <!-- Overrideable colors -->
        <SolidColorBrush x:Key="AccentColor" Color="#FFFF8541" />
    </ResourceDictionary>
</Application.Resources>
```

| Overrideable color key | Description |
|--------|------|
| `AccentColor` | Accent color |
| `PopupBackgroundColor` | Popup background color |
| `MaskColor` | Mask color used for hover highlights |

#### 2. Apply global styles (optional)

```xml
<!-- ContextMenu style -->
<Style BasedOn="{StaticResource FluentContextMenuStyle}" TargetType="{x:Type ContextMenu}">
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}" />
</Style>

<!-- MenuItem style -->
<Style BasedOn="{StaticResource FluentMenuItemStyle}" TargetType="MenuItem">
    <Setter Property="Height" Value="36" />
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}" />
    <Setter Property="VerticalContentAlignment" Value="Center" />
</Style>

<!-- TextBox ContextMenu -->
<Style TargetType="TextBox">
    <Setter Property="ContextMenu" Value="{StaticResource FluentTextBoxContextMenu}" />
</Style>

<!-- ToolTip style -->
<Style TargetType="{x:Type ToolTip}">
    <Setter Property="fluent:FluentStyle.UseFluentStyle" Value="True" />
    <Setter Property="Background" Value="{DynamicResource PopupBackgroundColor}" />
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}" />
</Style>
```

#### Menu example
```xml
<Menu Background="Transparent"
      Foreground="{DynamicResource ForegroundColor}"
      WindowChrome.IsHitTestVisibleInChrome="True">
    <MenuItem Header="_File">
        <MenuItem Header="_New" />
        <MenuItem Header="_Open" />
        <MenuItem Header="_Recent Files">
            <MenuItem Header="File1.txt" />
        </MenuItem>
    </MenuItem>
    <MenuItem Header="_Help" />
</Menu>
```

#### ContextMenu example
```xml
<TextBlock Text="Right Click Me">
    <TextBlock.ContextMenu>
        <ContextMenu>
            <MenuItem Header="Menu Item 1"
                      Icon="📋"
                      InputGestureText="Ctrl+C" />
            <MenuItem Header="Menu Item 2">
                <MenuItem Header="Child Item 1" />
                <MenuItem Header="Child Item 2" />
                <MenuItem Header="Child Item 3"
                          IsCheckable="True"
                          IsChecked="True" />
            </MenuItem>
            <MenuItem Header="Menu Item 3" />
        </ContextMenu>
    </TextBlock.ContextMenu>
</TextBlock>
```

#### ToolTip example
```xml
<TextBlock Text="Hover over me"
           ToolTipService.ShowDuration="3000">
    <TextBlock.ToolTip>
        <ToolTip>
            <TextBlock Text="This is a FluentWpfCore ToolTip!"/>
        </ToolTip>
    </TextBlock.ToolTip>
</TextBlock>
```
Or simply:
```xml
<TextBlock Text="Hover over me"
           ToolTip="This is a FluentWpfCore ToolTip!"/>
```

## 🤝 Contributing

Issues and pull requests are welcome!

## 📄 License

This project is open source under the [MIT](https://opensource.org/licenses/MIT) license.

## 🙏 Thanks

Thanks to all contributors to FluentWpfCore!

## 🧷 Related Tutorials

- Fluent Window: [WPF: Emulating native UWP window styles — Acrylic/Mica, custom title bar, native DWM animations (includes helper classes)](https://blog.twlmgatito.cn/posts/window-material-in-wpf/)
- Fluent Popup & ToolTip: [Using WindowMaterial in Popup and ToolTip on Win10/Win11](https://blog.twlmgatito.cn/posts/wpf-use-windowmaterial-in-popup-and-tooltip/)
- Fluent ScrollViewer: [Smooth ScrollViewer implemented with CompositionTarget.Rendering supporting wheel, touchpad, touch and pen](https://blog.twlmgatito.cn/posts/wpf-fluent-scrollviewer-with-all-device-supported/)
- Fluent Menu: [Using Fluent-style acrylic effects for ContextMenu in WPF](https://blog.twlmgatito.cn/posts/wpf-fluent-contextmenu-with-arcrylic/)

---

Made with ❤️ by [TwilightLemon](https://github.com/TwilightLemon)