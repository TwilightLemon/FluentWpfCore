using System;
using FluentWpfCore.Helpers;
using FluentWpfCore.ScrollPhysics;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentWpfCore.Controls;

/// <summary>
/// 平滑滚动控件，提供流畅的滚动动画效果，支持鼠标、触控板、触摸和触控笔输入
/// Smooth scrolling control providing fluid scrolling animations, supports mouse, touchpad, touch and stylus input
/// </summary>
public class SmoothScrollViewer : ScrollViewer
{
    private const double LogicalOffsetUpdateInterval = 1.0 / 24.0; // 24Hz for ScrollBar updates / 滚动条更新频率
    private const int WM_MOUSEHWHEEL = 0x020E; // Horizontal mouse wheel message / 水平鼠标滚轮消息

    private double _logicalOffsetVertical;   // The actual ScrollViewer vertical offset / 实际的ScrollViewer垂直偏移量
    private double _currentVisualOffsetVertical; // The target visual vertical offset (smooth) / 目标视觉垂直偏移量（平滑）
    private double _visualDeltaVertical;     // Visual vertical offset delta from logical offset / 视觉垂直偏移量与逻辑偏移量的差值

    private double _logicalOffsetHorizontal;   // The actual ScrollViewer horizontal offset / 实际的ScrollViewer水平偏移量
    private double _currentVisualOffsetHorizontal; // The target visual horizontal offset (smooth) / 目标视觉水平偏移量（平滑）
    private double _visualDeltaHorizontal;     // Visual horizontal offset delta from logical offset / 视觉水平偏移量与逻辑偏移量的差值

    private long _lastTimestamp;
    private double _logicalOffsetUpdateAccumulator;
    private bool _isRendering;

    private TranslateTransform? _transform;
    private UIElement? _content;
    private ScrollBar? _PART_VerticalScrollBar, _PART_HorizontalScrollBar;
    private HwndSource? _hwndSource;

    private IScrollPhysics _verticalScrollPhysics = new DefaultScrollPhysics();
    private IScrollPhysics _horizontalScrollPhysics = new DefaultScrollPhysics();

    /// <summary>
    /// 获取或设置滚动物理模型，控制滚动动画的行为
    /// Gets or sets the scroll physics model that controls scrolling animation behavior
    /// </summary>
    public IScrollPhysics Physics
    {
        get => _verticalScrollPhysics;
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _verticalScrollPhysics = value.Clone();
            _horizontalScrollPhysics = value.Clone();
        }
    }

    /// <summary>
    /// 初始化 SmoothScrollViewer 控件
    /// Initializes a new instance of SmoothScrollViewer
    /// </summary>
    public SmoothScrollViewer()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Initialization

    /// <summary>
    /// 应用控件模板，获取滚动条部件
    /// Applies control template and gets scrollbar parts
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _PART_VerticalScrollBar=base.GetTemplateChild("PART_VerticalScrollBar") as ScrollBar;
        _PART_HorizontalScrollBar=base.GetTemplateChild("PART_HorizontalScrollBar") as ScrollBar;
    }

    /// <summary>
    /// 控件加载时初始化变换和消息钩子
    /// Initializes transform and message hook when control is loaded
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Content is UIElement element)
        {
            _content = element;
            _transform = new TranslateTransform();
            element.RenderTransform = _transform;
            element.RenderTransformOrigin = new Point(0, 0);
        }
        else
        {
            throw new InvalidOperationException("SmoothScrollViewer.Content must be a UIElement.");
        }

        // Remove any existing hook before reassigning _hwndSource to avoid multiple registrations
        // 移除现有钩子以避免重复注册
        _hwndSource?.RemoveHook(WndProc);

        // Hook into the window's message loop for horizontal mouse wheel (touchpad horizontal scroll)
        // 钩入窗口消息循环以处理水平鼠标滚轮（触控板横向滚动）
        var window = Window.GetWindow(this);
        if (window != null)
        {
            _hwndSource = PresentationSource.FromVisual(window) as HwndSource;
            _hwndSource?.AddHook(WndProc);
        }
    }

    /// <summary>
    /// 控件卸载时清理资源和消息钩子
    /// Cleans up resources and message hook when control is unloaded
    /// </summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopRendering();
        
        // Remove the hook when unloaded / 卸载时移除钩子
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
    }

    /// <summary>
    /// 窗口消息处理过程，用于捕获水平鼠标滚轮消息
    /// Window message procedure for capturing horizontal mouse wheel messages
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_MOUSEHWHEEL)
        {
            // Check if this control should handle the message
            if (IsVisible && IsEnabled && IsEnableSmoothScrolling && CanScrollHorizontal)
            {
                // Check if mouse is over this control using hit testing
                var mousePos = Mouse.GetPosition(this);
                if (mousePos.X >= 0 && mousePos.X <= ActualWidth && 
                    mousePos.Y >= 0 && mousePos.Y <= ActualHeight)
                {
                    // Verify the control is actually under the mouse (not obscured by other elements)
                    if (InputHitTest(mousePos) is DependencyObject{ } hitElement)
                    {
                        // Check if there's a nested SmoothScrollViewer that should handle this instead
                        var nestedScrollViewer = FindParentSmoothScrollViewer(hitElement);
                        if (nestedScrollViewer == this)
                        {
                            // Extract the delta from wParam (high word)
                            int delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                            HandleScroll(0, -delta);
                            handled = true;
                        }
                    }
                }
            }
        }
        return IntPtr.Zero;
    }


    /// <summary>
    /// 处理滚动输入并启动动画
    /// Handles scroll input and starts animation
    /// </summary>
    /// <param name="deltaVertical">垂直滚动量 / Vertical scroll delta</param>
    /// <param name="deltaHorizontal">水平滚动量 / Horizontal scroll delta</param>
    /// <param name="isPreciseMode">是否为精确模式（触摸、触控笔）/ Whether in precise mode (touch, stylus)</param>
    private void HandleScroll(double deltaVertical, double deltaHorizontal, bool isPreciseMode=false)
    {
        if (deltaVertical == 0 && deltaHorizontal == 0) return;

        // Lock logical offset at scroll start
        if (!_isRendering)
        {
            _logicalOffsetVertical = VerticalOffset;
            _currentVisualOffsetVertical = _logicalOffsetVertical;
            _visualDeltaVertical = 0;

            _logicalOffsetHorizontal = HorizontalOffset;
            _currentVisualOffsetHorizontal = _logicalOffsetHorizontal;
            _visualDeltaHorizontal = 0;
        }

        _verticalScrollPhysics.IsPreciseMode = isPreciseMode;
        _horizontalScrollPhysics.IsPreciseMode = isPreciseMode;

        if (deltaVertical != 0)
        {
            _verticalScrollPhysics.OnScroll(deltaVertical);
        }

        if (deltaHorizontal != 0)
        {
            _horizontalScrollPhysics.OnScroll(deltaHorizontal);
        }

        StartRendering();
    }

    #endregion

    #region Input

    /// <summary>
    /// 处理鼠标滚轮事件，支持Shift键切换滚动方向
    /// Handles mouse wheel events with support for Shift key to toggle scroll direction
    /// </summary>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (!IsEnableSmoothScrolling)
        {
            base.OnMouseWheel(e);
            return;
        }

        e.Handled = true;

        // Determine scroll orientation
        bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        Orientation effectiveOrientation = PreferredScrollOrientation;
        
        if (AllowTogglePreferredScrollOrientationByShiftKey && isShiftPressed)
        {
            effectiveOrientation = effectiveOrientation == Orientation.Vertical ? Orientation.Horizontal : Orientation.Vertical;
        }

        if (effectiveOrientation == Orientation.Vertical && CanScrollVertical)
        {
            HandleScroll(e.Delta, 0);
        }
        else if (effectiveOrientation == Orientation.Horizontal && CanScrollHorizontal)
        {
            HandleScroll(0, e.Delta);
        }
    }

    /// <summary>
    /// 处理触摸/触控笔操作开始事件
    /// Handles manipulation starting events for touch/stylus input
    /// </summary>
    protected override void OnManipulationStarting(ManipulationStartingEventArgs e)
    {
        base.OnManipulationStarting(e);

        if (!IsEnableSmoothManipulating)
        {
            return;
        }

        e.Mode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
        e.ManipulationContainer = this;
        e.Handled = true;
    }

    /// <summary>
    /// 处理触摸/触控笔操作增量事件
    /// Handles manipulation delta events for touch/stylus input
    /// </summary>
    protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
    {
        if (!IsEnableSmoothManipulating)
        {
            base.OnManipulationDelta(e);
            return;
        }

        Vector translation = e.DeltaManipulation.Translation;
        double deltaH = CanScrollHorizontal ? translation.X : 0;
        double deltaV = CanScrollVertical ? translation.Y : 0;

        if (deltaH == 0 && deltaV == 0)
        {
            base.OnManipulationDelta(e);
            return;
        }

        HandleScroll(deltaV, deltaH,true);
        e.Handled = true;
    }

    /// <summary>
    /// 处理触摸/触控笔惯性滚动开始事件
    /// Handles manipulation inertia starting events for touch/stylus input
    /// </summary>
    protected override void OnManipulationInertiaStarting(ManipulationInertiaStartingEventArgs e)
    {
        base.OnManipulationInertiaStarting(e);

        if (!IsEnableSmoothManipulating)
        {
            return;
        }

        if (e.TranslationBehavior != null)
        {
            double speed = e.InitialVelocities.LinearVelocity.Length; // DIP per ms / DIP每毫秒
            double decel = MathExtension.Clamp(speed / 600.0, 0.0012, 0.012);
            e.TranslationBehavior.DesiredDeceleration = decel;
        }

        e.Handled = true;
    }

    /// <summary>
    /// 处理滚动变化事件，同步逻辑偏移和视觉偏移
    /// Handles scroll changed events, synchronizes logical and visual offsets
    /// </summary>
    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        base.OnScrollChanged(e);

        bool hasVerticalChange = e.VerticalChange != 0;
        bool hasHorizontalChange = e.HorizontalChange != 0;

        if (!hasVerticalChange && !hasHorizontalChange) return;

        if (hasVerticalChange)
        {
            _logicalOffsetVertical = e.VerticalOffset;

            if (_isRendering)
            {
                _visualDeltaVertical = _currentVisualOffsetVertical - _logicalOffsetVertical;
                _transform!.Y = -_visualDeltaVertical;
            }
            else
            {
                _visualDeltaVertical = 0;
                if (_transform != null)
                {
                    _transform.Y = 0;
                }
            }
        }

        if (hasHorizontalChange)
        {
            _logicalOffsetHorizontal = e.HorizontalOffset;

            if (_isRendering)
            {
                _visualDeltaHorizontal = _currentVisualOffsetHorizontal - _logicalOffsetHorizontal;
                _transform!.X = -_visualDeltaHorizontal;
            }
            else
            {
                _visualDeltaHorizontal = 0;
                if (_transform != null)
                {
                    _transform.X = 0;
                }
            }
        }
    }

    #endregion

    #region Rendering Loop

    /// <summary>
    /// 启动渲染循环进行平滑滚动动画
    /// Starts rendering loop for smooth scrolling animation
    /// </summary>
    private void StartRendering()
    {
        if (_isRendering) return;

        _lastTimestamp = Stopwatch.GetTimestamp();
        _logicalOffsetUpdateAccumulator = 0;
        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
        _content!.IsHitTestVisible = false;
    }

    /// <summary>
    /// 停止渲染循环并同步最终滚动位置
    /// Stops rendering loop and synchronizes final scroll position
    /// </summary>
    private void StopRendering()
    {
        if (!_isRendering) return;

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;

        double finalOffsetVertical = MathExtension.Clamp(_currentVisualOffsetVertical, 0, ScrollableHeight);
        double finalOffsetHorizontal = MathExtension.Clamp(_currentVisualOffsetHorizontal, 0, ScrollableWidth);

        ScrollToVerticalOffset(finalOffsetVertical);
        ScrollToHorizontalOffset(finalOffsetHorizontal);

        _PART_VerticalScrollBar?.SetBinding(ScrollBar.ValueProperty,new Binding("VerticalOffset") { RelativeSource=new RelativeSource(RelativeSourceMode.TemplatedParent),Mode=BindingMode.OneWay});
        _PART_HorizontalScrollBar?.SetBinding(ScrollBar.ValueProperty,new Binding("HorizontalOffset") { RelativeSource=new RelativeSource(RelativeSourceMode.TemplatedParent),Mode=BindingMode.OneWay});

        _visualDeltaVertical = 0;
        _logicalOffsetVertical = finalOffsetVertical;
        _transform!.Y = 0;

        _visualDeltaHorizontal = 0;
        _logicalOffsetHorizontal = finalOffsetHorizontal;
        _transform!.X = 0;
        
        _content!.IsHitTestVisible = true;
    }

    /// <summary>
    /// 渲染循环回调，每帧更新滚动位置
    /// Rendering loop callback that updates scroll position every frame
    /// </summary>
    private void OnRendering(object? sender, EventArgs e)
    {
        long now = Stopwatch.GetTimestamp();
        double dt = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = now;

        _currentVisualOffsetVertical = MathExtension.Clamp(_verticalScrollPhysics.Update(_currentVisualOffsetVertical, dt), 0, ScrollableHeight);
        _currentVisualOffsetHorizontal = MathExtension.Clamp(_horizontalScrollPhysics.Update(_currentVisualOffsetHorizontal, dt), 0, ScrollableWidth);

        if (_verticalScrollPhysics.IsStable && _horizontalScrollPhysics.IsStable)
        {
            StopRendering();
            return;
        }

        _logicalOffsetUpdateAccumulator += dt;
        if (_logicalOffsetUpdateAccumulator >= LogicalOffsetUpdateInterval)
        {
            _logicalOffsetUpdateAccumulator = 0;
            ScrollToVerticalOffset(_currentVisualOffsetVertical);
            ScrollToHorizontalOffset(_currentVisualOffsetHorizontal);
        }

        _visualDeltaVertical = _currentVisualOffsetVertical - _logicalOffsetVertical;
        _transform!.Y = -_visualDeltaVertical;
        _PART_VerticalScrollBar?.Value = _currentVisualOffsetVertical;

        _visualDeltaHorizontal = _currentVisualOffsetHorizontal - _logicalOffsetHorizontal;
        _transform!.X = -_visualDeltaHorizontal;
        _PART_HorizontalScrollBar?.Value = _currentVisualOffsetHorizontal;
    }

    #endregion

    #region Helpers & Properties

    /// <summary>
    /// 获取是否可以垂直滚动
    /// Gets whether vertical scrolling is possible
    /// </summary>
    public bool CanScrollVertical
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.ExtentHeight - scrollInfo.ViewportHeight > 0;
            }

            return ExtentHeight > ViewportHeight;
        }
    }

    /// <summary>
    /// 获取是否可以水平滚动
    /// Gets whether horizontal scrolling is possible
    /// </summary>
    public bool CanScrollHorizontal
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.ExtentWidth - scrollInfo.ViewportWidth > 0;
            }

            return ExtentWidth - ViewportWidth > 0;
        }
    }

    /// <summary>
    /// 获取是否可以向上滚动
    /// Gets whether scrolling up is possible
    /// </summary>
    public bool CanScrollUp
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.VerticalOffset > 0.5;
            }

            return VerticalOffset > 0.5;
        }
    }

    /// <summary>
    /// 获取是否可以向下滚动
    /// Gets whether scrolling down is possible
    /// </summary>
    public bool CanScrollDown
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.VerticalOffset + scrollInfo.ViewportHeight < scrollInfo.ExtentHeight - 0.5;
            }

            return VerticalOffset + ViewportHeight < ExtentHeight - 0.5;
        }
    }

    /// <summary>
    /// 获取是否可以向左滚动
    /// Gets whether scrolling left is possible
    /// </summary>
    public bool CanScrollLeft
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.HorizontalOffset > 0.5;
            }

            return HorizontalOffset > 0.5;
        }
    }

    /// <summary>
    /// 获取是否可以向右滚动
    /// Gets whether scrolling right is possible
    /// </summary>
    public bool CanScrollRight
    {
        get
        {
            if (ScrollInfo is IScrollInfo scrollInfo)
            {
                return scrollInfo.HorizontalOffset + scrollInfo.ViewportWidth < scrollInfo.ExtentWidth - 0.5;
            }

            return HorizontalOffset + ViewportWidth < ExtentWidth - 0.5;
        }
    }



    /// <summary>
    /// 获取或设置是否启用触摸/触控笔的平滑操作
    /// Gets or sets whether smooth manipulation for touch/stylus is enabled
    /// </summary>
    public bool IsEnableSmoothManipulating
    {
        get { return (bool)GetValue(IsEnableSmoothManipulatingProperty); }
        set { SetValue(IsEnableSmoothManipulatingProperty, value); }
    }

    /// <summary>
    /// IsEnableSmoothManipulating 依赖属性
    /// IsEnableSmoothManipulating dependency property
    /// </summary>
    public static readonly DependencyProperty IsEnableSmoothManipulatingProperty =
        DependencyProperty.Register(nameof(IsEnableSmoothManipulating), typeof(bool), typeof(SmoothScrollViewer), new PropertyMetadata(false,OnIsEnableSmoothManipulatingChanged));

    private static void OnIsEnableSmoothManipulatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if(d is SmoothScrollViewer{ } sv)
        {
            sv.IsManipulationEnabled = e.NewValue is true;
        }
    }

    /// <summary>
    /// 获取或设置是否启用平滑滚动动画
    /// Gets or sets whether smooth scrolling animation is enabled
    /// </summary>
    public bool IsEnableSmoothScrolling
    {
        get { return (bool)GetValue(IsEnableSmoothScrollingProperty); }
        set { SetValue(IsEnableSmoothScrollingProperty, value); }
    }

    /// <summary>
    /// IsEnableSmoothScrolling 依赖属性
    /// IsEnableSmoothScrolling dependency property
    /// </summary>
    public static readonly DependencyProperty IsEnableSmoothScrollingProperty =
        DependencyProperty.Register(nameof(IsEnableSmoothScrolling), typeof(bool), typeof(SmoothScrollViewer), new PropertyMetadata(true, OnIsEnableSmoothScrollingChanged));

    private static void OnIsEnableSmoothScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SmoothScrollViewer scrollViewer && e.NewValue is bool enabled)
        {
            if (!enabled && scrollViewer._isRendering)
            {
                // Stop smooth scrolling immediately if disabled during animation
                // 如果在动画期间禁用，立即停止平滑滚动
                scrollViewer.StopRendering();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否允许通过Shift键切换滚动方向
    /// Gets or sets whether toggling scroll orientation by Shift key is allowed
    /// </summary>
    public bool AllowTogglePreferredScrollOrientationByShiftKey
    {
        get { return (bool)GetValue(AllowTogglePreferredScrollOrientationByShiftKeyProperty); }
        set { SetValue(AllowTogglePreferredScrollOrientationByShiftKeyProperty, value); }
    }

    /// <summary>
    /// 获取或设置首选滚动方向
    /// Gets or sets the preferred scroll orientation
    /// </summary>
    public Orientation PreferredScrollOrientation
    {
        get { return (Orientation)GetValue(PreferredScrollOrientationProperty); }
        set { SetValue(PreferredScrollOrientationProperty, value); }
    }

    /// <summary>
    /// AllowTogglePreferredScrollOrientationByShiftKey 依赖属性
    /// AllowTogglePreferredScrollOrientationByShiftKey dependency property
    /// </summary>
    public static readonly DependencyProperty AllowTogglePreferredScrollOrientationByShiftKeyProperty =
        DependencyProperty.Register(nameof(AllowTogglePreferredScrollOrientationByShiftKey), typeof(bool), typeof(SmoothScrollViewer), new FrameworkPropertyMetadata(true));

    /// <summary>
    /// PreferredScrollOrientation 依赖属性
    /// PreferredScrollOrientation dependency property
    /// </summary>
    public static readonly DependencyProperty PreferredScrollOrientationProperty =
        DependencyProperty.Register(nameof(PreferredScrollOrientation), typeof(Orientation), typeof(SmoothScrollViewer), new FrameworkPropertyMetadata(Orientation.Vertical));

    /// <summary>
    /// 查找最近的可水平滚动的父级 SmoothScrollViewer
    /// Finds the nearest SmoothScrollViewer parent that can scroll horizontally
    /// </summary>
    private static SmoothScrollViewer? FindParentSmoothScrollViewer(DependencyObject element)
    {
        DependencyObject? current = element;
        while (current != null)
        {
            if (current is SmoothScrollViewer ssv && ssv.CanScrollHorizontal && ssv.IsEnableSmoothScrolling)
            {
                return ssv;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
    #endregion
}
