using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AbyssOverlay;

public partial class HelpOverlayWindow : Window
{
    private readonly AppConfig _config;
    public ObservableCollection<PriorityRow> PriorityRows { get; } = new();
    public ObservableCollection<string> Notes { get; } = new();

    private const int WmNchittest = 0x0084;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;

    private const int ResizeBorder = 8;

    public HelpOverlayWindow(AppConfig config)
    {
        InitializeComponent();
        _config = config;
        DataContext = this;

        Left = _config.HelpLeft;
        Top = _config.HelpTop;
        Width = _config.HelpWidth;
        Height = _config.HelpHeight;
        Opacity = _config.HelpOpacity;
        Topmost = true;

        if (!_config.HelpVisible)
        {
            Hide();
        }

        RootBorder.MouseLeftButtonDown += (_, e) =>
        {
            if (!_config.HelpLocked && e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };

        Loaded += (_, _) =>
        {
            ApplyLockState();
            var src = (HwndSource)PresentationSource.FromVisual(this)!;
            src.AddHook(WndProc);
        };
    }

    public void SetMessage(string title, string message)
    {
        RoomTitle.Text = string.IsNullOrWhiteSpace(title) ? "Abyss Help" : title;
        RoomSubtitle.Text = "Подсказка";
        MessageText.Text = message ?? string.Empty;

        PriorityRows.Clear();
        Notes.Clear();
        DataPanel.Visibility = Visibility.Collapsed;
        MessagePanel.Visibility = Visibility.Visible;
    }

    public void SetRoom(RoomInfo room)
    {
        RoomTitle.Text = room.Name;
        RoomSubtitle.Text = "Комната";

        PriorityRows.Clear();
        foreach (var item in room.Priority)
        {
            PriorityRows.Add(new PriorityRow
            {
                Num = item.Num,
                Target = item.Target,
                Role = string.IsNullOrWhiteSpace(item.Role) ? string.Empty : $"[{item.Role}]",
                Details = item.Details
            });
        }

        Notes.Clear();
        if (room.Notes.Count == 0)
        {
            Notes.Add("—");
        }
        else
        {
            foreach (var note in room.Notes)
            {
                Notes.Add(note);
            }
        }

        MessagePanel.Visibility = Visibility.Collapsed;
        DataPanel.Visibility = Visibility.Visible;
    }

    public sealed class PriorityRow
    {
        public string Num { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public void ApplyLockState()
    {
        if (_config.HelpLocked)
        {
            ResizeMode = ResizeMode.NoResize;
            Win32Interop.EnableClickThrough(this, true);
        }
        else
        {
            Win32Interop.EnableClickThrough(this, false);
            ResizeMode = ResizeMode.CanResize;
        }
        Topmost = true;
        HelpResizeGrip.Visibility = _config.HelpLocked ? Visibility.Collapsed : Visibility.Visible;
    }

    public void ApplyVisibility()
    {
        if (_config.HelpVisible)
        {
            Show();
            Activate();
        }
        else
        {
            Hide();
        }
    }

    public void ApplyOpacity()
    {
        Opacity = Math.Clamp(_config.HelpOpacity, 0.2, 1.0);
    }

    public void SaveGeometry()
    {
        if (WindowState != WindowState.Normal)
        {
            WindowState = WindowState.Normal;
        }

        _config.HelpLeft = Left;
        _config.HelpTop = Top;
        _config.HelpWidth = Width;
        _config.HelpHeight = Height;
        _config.Save();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmNchittest && !_config.HelpLocked)
        {
            var p = GetCursorPosition(lParam);
            var rect = new Rect(Left, Top, ActualWidth, ActualHeight);

            var onLeft = p.X - rect.Left <= ResizeBorder;
            var onRight = rect.Right - p.X <= ResizeBorder;
            var onTop = p.Y - rect.Top <= ResizeBorder;
            var onBottom = rect.Bottom - p.Y <= ResizeBorder;

            if (onLeft && onTop) { handled = true; return new IntPtr(HtTopLeft); }
            if (onRight && onTop) { handled = true; return new IntPtr(HtTopRight); }
            if (onLeft && onBottom) { handled = true; return new IntPtr(HtBottomLeft); }
            if (onRight && onBottom) { handled = true; return new IntPtr(HtBottomRight); }
            if (onLeft) { handled = true; return new IntPtr(HtLeft); }
            if (onRight) { handled = true; return new IntPtr(HtRight); }
            if (onTop) { handled = true; return new IntPtr(HtTop); }
            if (onBottom) { handled = true; return new IntPtr(HtBottom); }
        }

        return IntPtr.Zero;
    }

    private static System.Windows.Point GetCursorPosition(IntPtr lParam)
    {
        var x = (short)(lParam.ToInt32() & 0xFFFF);
        var y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
        return new System.Windows.Point(x, y);
    }
}
