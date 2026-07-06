using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using Screen = System.Windows.Forms.Screen;

namespace AbyssOverlay;

public partial class RegionSelectWindow : Window
{
    private System.Windows.Point? _start;
    private System.Windows.Shapes.Rectangle? _rect;

    public Rect? SelectedRegion { get; private set; }

    public RegionSelectWindow()
    {
        InitializeComponent();

        var bounds = Screen.PrimaryScreen?.Bounds ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
        foreach (var screen in Screen.AllScreens)
        {
            bounds = System.Drawing.Rectangle.Union(bounds, screen.Bounds);
        }

        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        RootCanvas.MouseLeftButtonDown += OnMouseDown;
        RootCanvas.MouseMove += OnMouseMove;
        RootCanvas.MouseLeftButtonUp += OnMouseUp;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedRegion = null;
            Close();
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(RootCanvas);
        _rect = new System.Windows.Shapes.Rectangle
        {
            Stroke = System.Windows.Media.Brushes.Red,
            StrokeThickness = 2
        };
        System.Windows.Controls.Canvas.SetLeft(_rect, _start.Value.X);
        System.Windows.Controls.Canvas.SetTop(_rect, _start.Value.Y);
        RootCanvas.Children.Clear();
        RootCanvas.Children.Add(_rect);
        Mouse.Capture(RootCanvas);
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_start == null || _rect == null)
        {
            return;
        }

        var pos = e.GetPosition(RootCanvas);
        var x = Math.Min(pos.X, _start.Value.X);
        var y = Math.Min(pos.Y, _start.Value.Y);
        var w = Math.Abs(pos.X - _start.Value.X);
        var h = Math.Abs(pos.Y - _start.Value.Y);
        System.Windows.Controls.Canvas.SetLeft(_rect, x);
        System.Windows.Controls.Canvas.SetTop(_rect, y);
        _rect.Width = w;
        _rect.Height = h;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_start == null || _rect == null)
        {
            return;
        }

        var pos = e.GetPosition(RootCanvas);
        var x = Math.Min(pos.X, _start.Value.X);
        var y = Math.Min(pos.Y, _start.Value.Y);
        var w = Math.Abs(pos.X - _start.Value.X);
        var h = Math.Abs(pos.Y - _start.Value.Y);

        SelectedRegion = new Rect(Left + x, Top + y, Math.Max(1, w), Math.Max(1, h));
        Mouse.Capture(null);
        Close();
    }
}
