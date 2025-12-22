# FluentWpfCore (Preview)

一个专注于底层实现的 WPF 类库，为现有 WPF 应用提供 Fluent Design 风格的窗口材质特效和基础控件支持。

[<img src="https://img.shields.io/badge/license-MIT-yellow"/>](LICENSE.txt)
![C#](https://img.shields.io/badge/lang-C%23-orange)
![WPF](https://img.shields.io/badge/UI-WPF-b33bb3)
![GitHub Repo stars](https://img.shields.io/github/stars/TwilightLemon/FluentWpfCore)

## ✨ 特性

### 🪟 窗口特效系统
- **多材质支持** - Acrylic、Mica、MicaAlt 等现代窗口材质
- **灵活组合** - 材质效果 + 圆角 + 阴影 + DWM 动画自由组合
- **跨版本兼容** - 支持 Windows 10 旧版 Composition API 和 Windows 11 System Backdrop API
- **深色模式** - 内置深浅色模式切换支持(主要针对Mica效果)
- **自定义组合色** - 为 Acrylic 效果自定义背景颜色和透明度

### 🎨 基础控件增强
- **FluentPopup** - 带有亚克力背景、圆角阴影和滑动动画的弹出窗口
- **SmoothScrollViewer** - 提供流畅平滑的滚动体验
- **Fluent 风格样式** - Menu、ContextMenu、ToolTip 的现代化样式和模板

### 🔧 底层能力
- **DWM 集成** - 所有效果均由DWM负责渲染，在Windows11上表现最佳
- **WindowChrome 兼容** - 与 WPF 原生 WindowChrome 完美协作
- **主题无关** - 不强制特定 UI 风格，可与任何现有主题集成

> FluentWpfCore不提供完整的 UI 控件集，而是提供可与任何主题兼容的底层能力，让你在不改变现有 UI 风格的前提下，为应用添加现代化的视觉效果。  

## 🔧 系统要求

- Windows 10 1809 及以上版本（部分特性需要 Windows 11）

### 特性支持

| 特性 | Windows 10 1809+ | Windows 11 |
|------|-----------------|------------|
| Acrylic (Composition) | ✅ | ✅ |
| Acrylic (System Backdrop) | ❌ | ✅ |
| Mica | ❌ | ✅ |
| 窗口圆角 | ❌ | ✅ |
| DWM 动画 | ✅ | ✅ |

### 支持的 .NET 版本
- .NET 10.0 Windows
- .NET 8.0 Windows
- .NET 6.0 Windows
- .NET Framework 4.5 ~ 4.8

## 📦 安装

### NuGet 包管理器
```powershell
Install-Package FluentWpfCore
```

### .NET CLI
```bash
dotnet add package FluentWpfCore
```

### PackageReference
```xml
<PackageReference Include="FluentWpfCore" Version="1.0.0" />
```

## 📖 使用指南

### 窗口特效

FluentWpfCore 提供全面的窗口材质特效支持，包括 Acrylic、Mica(Win11) 等效果，以及从扁平到丰富的DWM特效组合。在以下效果中自由组合使用：  

| 项目   | 分类                               | 可选项                      |
| ---- | -------------------------------- | ------------------------ |
| 窗口材质 | Acrylic\Mica\MicaAlt             | 深色模式、组合颜色、失焦保持 (Acrylic) |
| 圆角   | Round\SmallRound\DoNotRound\Default |                          |
| 窗口阴影 | On\Off                           | 与圆角效果绑定，取决于 DWM          |


#### 窗口材质
例如创建一个：亚克力材质、蓝色组合色、圆角带阴影、启用 DWM 动画的自定义窗口，并且取消了原生标题栏和按钮：
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

##### 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `MaterialMode` | `MaterialType` | 材质类型：`None`、`Acrylic`、`Mica`、`MicaAlt` |
| `IsDarkMode` | `bool` | 是否使用深色模式（适用于Mica \ MicaAlt，Acrylic效果不明显） |
| `UseWindowComposition` | `bool` | 是否使用窗口组合（Windows 10 1809 及以上，适用于Acrylic） |
| `WindowChromeEx` | `WindowChrome` | 自定义 WindowChrome 配置 |
| `CompositonColor` | `Color` | 组合模式下的背景颜色（仅对Acrylic， UseWindowComposition=True） |

#### 窗口圆角

强制为窗口应用 Windows 11 风格的圆角，以覆盖WPF或DWM默认行为。启用圆角时，同样会带来窗口阴影效果（依赖DWM，仅在Windows 11有效）和边框。

```xml
<Window xmlns:fluent="https://github.com/TwilightLemon/FluentWpfCore"
        fluent:WindowMaterial.WindowCorner="Round"
        ...>
```
或者在后端为hwnd启用：
```csharp
using FluentWpfCore.Interop;
MaterialApis.SetWindowCorner(hwnd, corner);
```

支持的圆角类型：
- `Default` - 系统默认
- `DoNotRound` - 不使用圆角
- `Round` - 圆角
- `RoundSmall` - 小圆角
  
推荐使用场景
- 在使用Acrylic(UseWindowComposition=True)时DWM默认为直角无阴影
- 控制ToolTip、Popup等弹出窗口的圆角样式
- 自定义窗口边框样式(即使使用WindowChrome或者AllowsTransparency)

#### DWM 动画

启用系统原生的窗口动画效果（最大化/最小化），同时取消原生标题栏和按钮：

```xml
<Window xmlns:fluent="https://github.com/TwilightLemon/FluentWpfCore"
        fluent:DwmAnimation.EnableDwmAnimation="True"
        ...>
```
注意，启用DWM动画后，将无视`Window.ResizeMode`属性。如果期望`ResizeMode="NoResize"`，可以使用`WindowChrome.ResizeBorderThickness="0"`来达到相同效果。

#### 组合效果
你几乎可以随意组合以上三种效果，例如：

##### 平凡的Mica原生窗口
```xml
<Window Background="Transparent"
        ...>
    <fluent:WindowMaterial.Material>
        <fluent:WindowMaterial IsDarkMode="False"
                               MaterialMode="Mica"/>
    </fluent:WindowMaterial.Material>
</Window>
```
除了背景启用Mica效果外，窗口本身保持原生样式。包括标题栏、按钮、边框和动画。

##### 自定义标题栏的Mica窗口，同时保持默认窗口动画和边框效果
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
去除了原生标题栏和按钮，将客户区拓展至整个窗口，同时保持了窗口的边框和动画效果。

##### 创建一个失焦时保持亚克力效果的窗口，同时保持圆角和阴影
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
使用`UseWindowComposition="True"`时，将调用与Acrylic/Mica/MicaAlt不同的API来启用旧版材质效果(Windows 10)。

##### 创建一个亚克力直角无阴影窗口
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
这是`MaterialMode="Acrylic"`+`UseWindowComposition="True"` 时的默认行为。
### FluentPopup

FluentPopup 是一个增强的弹出窗口控件。默认带有亚克力背景、Round圆角和阴影，以及自定义的进入/退出动画和跟随窗口移动的功能：

```xml
<Button x:Name="ShowPopupBtn" Content="Show Popup" />

<fluent:FluentPopup x:Name="testPopup"
                    Background="{DynamicResource PopupBackgroundColor}"
                    ExtPopupAnimation="SlideDown"
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

#### 属性说明

| 属性 | 类型 | 说明 |
|------|------|------|
| `Background` | `SolidColorBrush` | 弹出窗口背景色 |
| `ExtPopupAnimation` | `ExPopupAnimation` | 动画类型：`None`、`SlideUp`、`SlideDown` |
| `FollowWindowMoving` | `bool` | 是否跟随窗口移动 |
| `WindowCorner` | `WindowCorner` | 弹出窗口的圆角样式 |

注意，`Background`只支持纯色，如果需要其他的请保持透明并在内容中自定义背景。

### SmoothScrollViewer

提供平滑流畅的滚动体验，支持自定义物理模型，兼容鼠标滚轮、触控板：

```xml
<fluent:SmoothScrollViewer>
    <StackPanel>
        <!--内容-->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

#### 属性说明

| 属性 | 类型 | 默认值 | 说明 |
|------|------|---------|------|
| `IsEnableSmoothScrolling` | `bool` | `true` | 启用或禁用平滑滚动动画 |
| `PreferredScrollOrientation` | `Orientation` | `Vertical` | 首选滚动方向：`Vertical`（竖直）或 `Horizontal`（水平） |
| `AllowTogglePreferredScrollOrientationByShiftKey` | `bool` | `true` | 允许通过按住 Shift 键切换滚动方向 |
| `Physics` | `IScrollPhysics` | `DefaultScrollPhysics` | 滚动物理模型，控制动画行为 |

#### 滚动物理模型

`SmoothScrollViewer` 通过 `IScrollPhysics` 接口使用可插拔的物理模型，允许你自定义滚动行为。默认实现是 `DefaultScrollPhysics`，它提供两种不同的模式：

##### 精确模式（触控板）
用于触控板/触摸输入，使用平滑插值来跟随目标偏移量：
- **LerpFactor**（`double`，默认值：`0.5`，范围：`0~1`）— 插值系数；数值越大，滚动越快到达目标位置

##### 惯性模式（鼠标滚轮）
用于鼠标滚轮输入，使用基于速度的物理引擎并带有摩擦力：
- **MinVelocityFactor**（`double`，默认值：`1.2`，范围：`1~2`）— 起始速度倍数；数值越大，起始速度越快，滚动距离越长
- **Friction**（`double`，默认值：`0.92`，范围：`0~1`）— 速度衰减系数；数值越小，越快停下来

物理模型会自动检测输入类型并在两种模式之间切换。速度因子会根据滚动间隔时间动态调整，以获得最佳手感。

#### 使用示例

##### 基础使用
```xml
<fluent:SmoothScrollViewer>
    <StackPanel>
        <TextBlock Text="Item 1" Height="100" />
        <TextBlock Text="Item 2" Height="100" />
        <TextBlock Text="Item 3" Height="100" />
        <!-- 更多项目 -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

##### 自定义滚动物理参数
```xml
<fluent:SmoothScrollViewer>
    <fluent:SmoothScrollViewer.Physics>
        <fluent:DefaultScrollPhysics MinVelocityFactor="1.5"
                                     Friction="0.85"
                                     LerpFactor="0.6" />
    </fluent:SmoothScrollViewer.Physics>
    <StackPanel>
        <!-- 内容 -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

##### 配置滚动方向
```xml
<!-- 默认水平滚动 -->
<fluent:SmoothScrollViewer PreferredScrollOrientation="Horizontal"
                           HorizontalScrollBarVisibility="Auto"
                           VerticalScrollBarVisibility="Disabled">
    <StackPanel Orientation="Horizontal">
        <!-- 内容 -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

##### 使用 Shift 键在竖直和水平方向之间切换
```xml
<fluent:SmoothScrollViewer AllowTogglePreferredScrollOrientationByShiftKey="True"
                           HorizontalScrollBarVisibility="Auto"
                           VerticalScrollBarVisibility="Auto">
    <!-- 滚动时按住 Shift 键可切换方向 -->
    <Grid>
        <!-- 内容 -->
    </Grid>
</fluent:SmoothScrollViewer>
```

##### 临时禁用平滑滚动
```xml
<fluent:SmoothScrollViewer IsEnableSmoothScrolling="False">
    <!-- 回退到标准 ScrollViewer 行为 -->
    <StackPanel>
        <!-- 内容 -->
    </StackPanel>
</fluent:SmoothScrollViewer>
```

##### 与 ItemsControl 配合用于大列表
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

##### 自定义物理实现
你可以通过实现 `IScrollPhysics` 接口来创建自己的物理模型：

```csharp
public class CustomScrollPhysics : IScrollPhysics
{
    public bool IsStable { get; private set; }
    
    public void OnScroll(double currentOffset, double delta, bool isPrecision, 
                         double minOffset, double maxOffset, int timeIntervalMs)
    {
        // 处理滚动输入
        IsStable = false;
    }
    
    public double Update(double currentOffset, double dt, 
                         double minOffset, double maxOffset)
    {
        // 计算并返回新的偏移量
        // 当动画应该停止时设置 IsStable = true
        return newOffset;
    }
}
```

然后将其应用到 SmoothScrollViewer：
```csharp
smoothScrollViewer.Physics = new CustomScrollPhysics();
```
### Fluent 风格的 Menu
这部分内容涉及自定义控件模板和样式，所以需要先引入资源：

#### 1. 引入资源字典

在 `App.xaml` 中引入 FluentWpfCore 的主题资源：

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!--引入 FluentWpfCore 默认主题-->
            <ResourceDictionary Source="pack://application:,,,/FluentWpfCore;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
        <SolidColorBrush x:Key="ForegroundColor" Color="#FF0E0E0E" />
        
        <!--可覆盖颜色值-->
        <SolidColorBrush x:Key="AccentColor" Color="#FFFF8541" />
    </ResourceDictionary>
</Application.Resources>
```

| 可覆盖颜色值 | 说明 |
|--------|------|
| `AccentColor` | 强调色 |
| `PopupBackgroundColor` | 弹出窗口背景色 |
| `MaskColor` | 遮罩颜色，用于鼠标停留(Hover)时高亮 |

#### 2. 应用全局样式（可选）

```xml
<!--ContextMenu 样式-->
<Style BasedOn="{StaticResource FluentContextMenuStyle}" TargetType="{x:Type ContextMenu}">
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}" />
</Style>

<!--MenuItem 样式-->
<Style BasedOn="{StaticResource FluentMenuItemStyle}" TargetType="MenuItem">
    <Setter Property="Height" Value="36" />
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}" />
    <Setter Property="VerticalContentAlignment" Value="Center" />
</Style>

<!--TextBox ContextMenu-->
<Style TargetType="TextBox">
    <Setter Property="ContextMenu" Value="{StaticResource FluentTextBoxContextMenu}" />
</Style>

<!--ToolTip 样式-->
<Style TargetType="{x:Type ToolTip}">
    <Setter Property="fluent:FluentStyle.UseFluentStyle" Value="True" />
    <Setter Property="Background" Value="{DynamicResource PopupBackgroundColor}" />
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}" />
</Style>
```

#### Menu
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

#### ContextMenu

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

#### ToolTip

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

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目基于 [MIT](https://opensource.org/licenses/MIT) 许可证开源。

## 🙏 致谢

感谢所有为 FluentWpfCore 做出贡献的开发者！

## 🧷相关教程

- Fluent Window: [WPF 模拟UWP原生窗口样式——亚克力|云母材质、自定义标题栏样式、原生DWM动画 （附我封装好的类）](https://blog.twlmgatito.cn/posts/window-material-in-wpf/)
- Fluent Popup & ToolTip: [WPF中为Popup和ToolTip使用WindowMaterial特效 win10/win11](https://blog.twlmgatito.cn/posts/wpf-use-windowmaterial-in-popup-and-tooltip/)
- Fluent ScrollViewer: [WPF 使用CompositionTarget.Rendering实现平滑流畅滚动的ScrollViewer，支持滚轮、触控板、触摸屏和笔](https://blog.twlmgatito.cn/posts/wpf-fluent-scrollviewer-with-all-device-supported/)
- Fluent Menu: [WPF 为ContextMenu使用Fluent风格的亚克力材质特效](https://blog.twlmgatito.cn/posts/wpf-fluent-contextmenu-with-arcrylic/)

---

Made with ❤️ by [TwilightLemon](https://github.com/TwilightLemon)
