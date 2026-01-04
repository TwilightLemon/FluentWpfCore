using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

// EleCho.WpfSuite
// OrgEleCho
// origin source: https://github.com/OrgEleCho/EleCho.WpfSuite/blob/master/EleCho.WpfSuite.Input/Input/StylusTouchDevice.cs
// MIT License

namespace FluentWpfCore.AttachedProperties
{
    /// <summary>
    /// 触控笔触摸设备模拟器，将触控笔输入转换为触摸输入
    /// Stylus touch device simulator that converts stylus input to touch input
    /// </summary>
    public class StylusTouchDevice : TouchDevice
    {
        #region Class Members

        private static StylusTouchDevice? _device;
        private static UIElement? _currentStylusUIElement;
        private static bool _stylusMoved;
        private static Point _stylusDownPosition;

        /// <summary>
        /// 获取或设置当前触摸位置
        /// Gets or sets the current touch position
        /// </summary>
        public Point Position { get; set; }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// 注册触控笔事件处理器
        /// Registers stylus event handlers
        /// </summary>
        private static void RegisterEvents(FrameworkElement root)
        {
            root.PreviewStylusDown += StylusDown;
            root.PreviewStylusMove += StylusMove;
            root.PreviewStylusUp += StylusUp;
        }

        /// <summary>
        /// 注销触控笔事件处理器
        /// Unregisters stylus event handlers
        /// </summary>
        private static void UnregisterEvents(FrameworkElement root)
        {
            root.PreviewStylusDown -= StylusDown;
            root.PreviewStylusMove -= StylusMove;
            root.PreviewStylusUp -= StylusUp;
        }

        #endregion

        #region Private Static Methods


        /// <summary>
        /// 计算两点之间的距离
        /// Calculates distance between two points
        /// </summary>
        private static double GetPointDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        /// <summary>
        /// 触控笔按下事件处理
        /// Handles stylus down event
        /// </summary>
        private static void StylusDown(object sender, StylusDownEventArgs e)
        {
            var currentPosition = e.GetPosition(null);

            if (_device != null &&
                _device.IsActive)
            {
                _device.ReportUp();
                _device.Deactivate();
                _device = null;
            }

            _device = new StylusTouchDevice(e.Device.GetHashCode());
            _device.SetActiveSource(e.Device.ActiveSource);
            _device.Position = currentPosition;
            _device.Activate();
            _device.ReportDown();

            _stylusMoved = false;
            _stylusDownPosition = currentPosition;
            _currentStylusUIElement = null;
        }

        /// <summary>
        /// 触控笔移动事件处理
        /// Handles stylus move event
        /// </summary>
        private static void StylusMove(object sender, StylusEventArgs e)
        {
            if (sender is not DependencyObject dependencyObject)
                return;

            var currentPosition = e.GetPosition(null);

            if (_device != null &&
                _device.IsActive &&
                (_stylusMoved || GetPointDistance(_stylusDownPosition, currentPosition) >= GetMoveThreshold(dependencyObject)))
            {
                _device.Position = currentPosition;
                _device.ReportMove();

                if (sender is UIElement element &&
                    !_stylusMoved)
                {
                    _currentStylusUIElement = element;
                    e.StylusDevice.Capture(_currentStylusUIElement, CaptureMode.SubTree);
                }

                _stylusMoved = true;
            }
        }

        /// <summary>
        /// 触控笔抬起事件处理
        /// Handles stylus up event
        /// </summary>
        private static void StylusUp(object sender, StylusEventArgs e)
        {
            var currentPosition = e.GetPosition(null);

            if (_device != null &&
                _device.IsActive)
            {
                var device = _device;
                _device = null;

                device.Position = e.GetPosition(null);
                device.ReportUp();
                device.Deactivate();

                if (_currentStylusUIElement is not null)
                {
                    e.StylusDevice.Capture(null);
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 初始化触控笔触摸设备
        /// Initializes stylus touch device
        /// </summary>
        /// <param name="deviceId">设备ID / Device ID</param>
        private StylusTouchDevice(int deviceId) :
            base(deviceId)
        {
            Position = new Point();
        }

        #endregion

        #region Overridden methods

        /// <summary>
        /// 获取中间触摸点集合
        /// Gets intermediate touch points
        /// </summary>
        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            return new TouchPointCollection();
        }

        /// <summary>
        /// 获取当前触摸点
        /// Gets current touch point
        /// </summary>
        public override TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            Point point = Position;
            if (relativeTo != null)
            {
                point = this.ActiveSource.RootVisual.TransformToDescendant((Visual)relativeTo).Transform(Position);
            }

            Rect rect = new Rect(point, new Size(1, 1));

            return new TouchPoint(this, point, rect, TouchAction.Move);
        }

        #endregion



        /// <summary>
        /// 获取是否模拟触摸
        /// Gets whether touch simulation is enabled
        /// </summary>
        public static bool GetSimulate(DependencyObject obj)
        {
            return (bool)obj.GetValue(SimulateProperty);
        }

        /// <summary>
        /// 设置是否模拟触摸
        /// Sets whether touch simulation is enabled
        /// </summary>
        public static void SetSimulate(DependencyObject obj, bool value)
        {
            obj.SetValue(SimulateProperty, value);
        }

        /// <summary>
        /// 获取移动阈值（像素）
        /// Gets move threshold in pixels
        /// </summary>
        public static double GetMoveThreshold(DependencyObject obj)
        {
            return (double)obj.GetValue(MoveThresholdProperty);
        }

        /// <summary>
        /// 设置移动阈值（像素）
        /// Sets move threshold in pixels
        /// </summary>
        public static void SetMoveThreshold(DependencyObject obj, double value)
        {
            obj.SetValue(MoveThresholdProperty, value);
        }


        /// <summary>
        /// 模拟触摸附加属性
        /// Simulate touch attached property
        /// </summary>
        public static readonly DependencyProperty SimulateProperty =
            DependencyProperty.RegisterAttached("Simulate", typeof(bool), typeof(StylusTouchDevice), new PropertyMetadata(false, SimulatePropertyChanged));

        /// <summary>
        /// 移动阈值附加属性（默认3.0像素）
        /// Move threshold attached property (default 3.0 pixels)
        /// </summary>
        public static readonly DependencyProperty MoveThresholdProperty =
            DependencyProperty.RegisterAttached("MoveThreshold", typeof(double), typeof(StylusTouchDevice), new PropertyMetadata(3.0));



        private static void SimulatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement frameworkElement ||
                e.NewValue is not bool newValue)
            {
                return;
            }

            if (newValue)
            {
                RegisterEvents(frameworkElement);
            }
            else
            {
                UnregisterEvents(frameworkElement);
            }
        }
    }
}