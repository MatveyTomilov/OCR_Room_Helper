using System;
using System.Windows;

namespace AbyssOverlay;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly RoomStore _store;
    private readonly HelpOverlayWindow _help;
    private bool _ready;

    public event Action? SelectRegionRequested;
    public event Action? AnalyzeRequested;
    public event Action? ReloadRequested;
    public event Action? QuitRequested;

    public SettingsWindow(AppConfig config, RoomStore store, OcrService _ocr, HelpOverlayWindow help)
    {
        _config = config;
        _store = store;
        _help = help;
        InitializeComponent();

        ShowOverlayCheck.IsChecked = _config.HelpVisible;
        LockOverlayCheck.IsChecked = _config.HelpLocked;
        OpacitySlider.Value = _config.HelpOpacity;
        OpacityValue.Text = _config.HelpOpacity.ToString("0.00");
        _ready = true;

        TitleBar.MouseLeftButtonDown += (_, e) =>
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        };

        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };

        UpdateStatus(null);
    }

    public void UpdateStatus(string? regionText)
    {
        var regionInfo = regionText ?? "Region: not set";
        var roomsInfo = $"Rooms loaded: {_store.Rooms.Count}";
        var excelInfo = $"Excel: {System.IO.Path.GetFileName(_store.ExcelPath)}";
        StatusText.Text = $"{excelInfo}\n{roomsInfo}\n{regionInfo}";
    }

    private void SelectRegion_Click(object sender, RoutedEventArgs e) => SelectRegionRequested?.Invoke();
    private void Analyze_Click(object sender, RoutedEventArgs e) => AnalyzeRequested?.Invoke();
    private void Reload_Click(object sender, RoutedEventArgs e) => ReloadRequested?.Invoke();
    private void Quit_Click(object sender, RoutedEventArgs e) => QuitRequested?.Invoke();

    private void OverlayChanged(object sender, RoutedEventArgs e)
    {
        _config.HelpVisible = ShowOverlayCheck.IsChecked == true;
        _config.HelpLocked = LockOverlayCheck.IsChecked == true;
        _config.Save();

        _help.ApplyVisibility();
        _help.ApplyLockState();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready || OpacityValue == null)
        {
            return;
        }
        _config.HelpOpacity = OpacitySlider.Value;
        OpacityValue.Text = _config.HelpOpacity.ToString("0.00");
        _config.Save();
        _help.ApplyOpacity();
    }

    private void SaveOverlay_Click(object sender, RoutedEventArgs e)
    {
        _help.SaveGeometry();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
