using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using FormsKeys = System.Windows.Forms.Keys;

namespace AbyssOverlay;

public partial class MainWindow : Window
{
    private readonly AppConfig _config;
    private readonly RoomStore _store;
    private readonly OcrService _ocr;
    private readonly SettingsWindow _settings;
    private readonly HelpOverlayWindow _help;

    private System.Windows.Point? _dragStart;
    private bool _dragMoved;

    public MainWindow(AppConfig config, RoomStore store, OcrService ocr, SettingsWindow settings, HelpOverlayWindow help)
    {
        InitializeComponent();

        _config = config;
        _store = store;
        _ocr = ocr;
        _settings = settings;
        _help = help;

        // Clamp button position to primary screen bounds
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen != null)
        {
            var bounds = screen.Bounds;
            if (_config.ButtonLeft < bounds.Left || _config.ButtonLeft > bounds.Right - Width)
            {
                _config.ButtonLeft = bounds.Left + 20;
            }
            if (_config.ButtonTop < bounds.Top || _config.ButtonTop > bounds.Bottom - Height)
            {
                _config.ButtonTop = bounds.Top + 20;
            }
        }

        Left = _config.ButtonLeft;
        Top = _config.ButtonTop;

        OverlayButton.PreviewMouseLeftButtonDown += OnDragStart;
        OverlayButton.PreviewMouseMove += OnDragMove;
        OverlayButton.PreviewMouseLeftButtonUp += OnDragEnd;

        Loaded += OnLoaded;
        Closed += OnClosed;

        _settings.SelectRegionRequested += SelectRegion;
        _settings.AnalyzeRequested += () => _ = AnalyzeAsync();
        _settings.ReloadRequested += ReloadExcel;
        _settings.QuitRequested += Quit;

        _settings.UpdateStatus(GetRegionText());
        _help.ApplyVisibility();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Logger.Log("Main window loaded");
        var src = (HwndSource)PresentationSource.FromVisual(this)!;
        src.AddHook(WndProc);
        RegisterHotkeys();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        UnregisterHotkeys();
    }

    private void ToggleSettings()
    {
        if (_settings.IsVisible)
        {
            _settings.Hide();
        }
        else
        {
            _settings.Show();
            _settings.Activate();
        }
    }

    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _dragMoved = false;
        OverlayButton.CaptureMouse();
    }

    private void OnDragMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragStart == null) return;
        var pos = e.GetPosition(this);
        var dx = pos.X - _dragStart.Value.X;
        var dy = pos.Y - _dragStart.Value.Y;
        if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
        {
            _dragMoved = true;
        }
        Left += dx;
        Top += dy;
    }

    private void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        _dragStart = null;
        OverlayButton.ReleaseMouseCapture();
        if (_dragMoved)
        {
            _config.ButtonLeft = Left;
            _config.ButtonTop = Top;
            _config.Save();
        }
        else
        {
            ToggleSettings();
        }
    }

    private void RegisterHotkeys()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        Win32Interop.RegisterHotKey(hwnd, Win32Interop.HotkeyIdSelect, Win32Interop.ModControl | Win32Interop.ModAlt, (int)FormsKeys.R);
        Win32Interop.RegisterHotKey(hwnd, Win32Interop.HotkeyIdAnalyze, Win32Interop.ModControl | Win32Interop.ModAlt, (int)FormsKeys.S);
        Win32Interop.RegisterHotKey(hwnd, Win32Interop.HotkeyIdQuit, Win32Interop.ModControl | Win32Interop.ModAlt, (int)FormsKeys.Q);
    }

    private void UnregisterHotkeys()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        Win32Interop.UnregisterHotKey(hwnd, Win32Interop.HotkeyIdSelect);
        Win32Interop.UnregisterHotKey(hwnd, Win32Interop.HotkeyIdAnalyze);
        Win32Interop.UnregisterHotKey(hwnd, Win32Interop.HotkeyIdQuit);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Interop.WmHotkey)
        {
            var id = wParam.ToInt32();
            switch (id)
            {
                case Win32Interop.HotkeyIdSelect:
                    SelectRegion();
                    handled = true;
                    break;
                case Win32Interop.HotkeyIdAnalyze:
                    _ = AnalyzeAsync();
                    handled = true;
                    break;
                case Win32Interop.HotkeyIdQuit:
                    Quit();
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    private void SelectRegion()
    {
        var selector = new RegionSelectWindow();
        selector.ShowDialog();

        if (selector.SelectedRegion is { } region)
        {
            _config.RegionLeft = (int)region.Left;
            _config.RegionTop = (int)region.Top;
            _config.RegionWidth = (int)region.Width;
            _config.RegionHeight = (int)region.Height;
            _config.Save();
        }

        _settings.UpdateStatus(GetRegionText());
    }

    private string GetRegionText()
    {
        if (_config.RegionLeft.HasValue && _config.RegionTop.HasValue && _config.RegionWidth.HasValue && _config.RegionHeight.HasValue)
        {
            return $"Region: ({_config.RegionLeft},{_config.RegionTop}) {_config.RegionWidth}x{_config.RegionHeight}";
        }
        return "Region: not set";
    }

    private void ReloadExcel()
    {
        _store.Reload();
        _settings.UpdateStatus(GetRegionText());
    }

    private void Quit()
    {
        _config.Save();
        System.Windows.Application.Current.Shutdown();
    }

    private async Task AnalyzeAsync()
    {
        if (!_ocr.IsAvailable)
        {
            _help.SetMessage("OCR", "Tesseract не найден. Установи Tesseract OCR или добавь его в PATH.");
            _help.ApplyVisibility();
            return;
        }

        if (!_config.RegionLeft.HasValue || !_config.RegionTop.HasValue || !_config.RegionWidth.HasValue || !_config.RegionHeight.HasValue)
        {
            SelectRegion();
            if (!_config.RegionLeft.HasValue)
            {
                return;
            }
        }

        var bmp = CaptureRegion(
            _config.RegionLeft!.Value,
            _config.RegionTop!.Value,
            _config.RegionWidth!.Value,
            _config.RegionHeight!.Value);

        var rawText = await _ocr.RecognizeAsync(bmp, _config.OcrLang);
        bmp.Dispose();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            var err = _ocr.LastError;
            var message = "Текст не распознан. Проверь выбранный регион и доступность Tesseract.\n" +
                          "Для полноэкранной игры используй безрамочный/оконный режим.";
            if (!string.IsNullOrWhiteSpace(err))
            {
                message += "\n\nОшибка: " + err;
            }
            _help.SetMessage("OCR", message);
            _help.ApplyVisibility();
            return;
        }

        var norm = Similarity.Normalize(rawText);
        var freq = BuildTokenFrequency(_store.Rooms.Values);

        RoomInfo? bestRoom = null;
        double bestScore = 0;
        int bestHits = 0;
        bool bestHasUnique = false;
        foreach (var room in _store.Rooms.Values)
        {
            var res = ScoreRoom(room, norm, freq);
            if (res.Score > bestScore || (Math.Abs(res.Score - bestScore) < 0.01 && res.Hits > bestHits))
            {
                bestRoom = room;
                bestScore = res.Score;
                bestHits = res.Hits;
                bestHasUnique = res.HasUnique;
            }
        }

        var allowSingle = bestHasUnique && bestHits >= 1 && bestScore >= 85;
        if (!(bestHits >= 2 || allowSingle))
        {
            bestRoom = null;
        }

        if (bestRoom == null)
        {
            _help.SetMessage("Не распознано", "Комната не распознана.\nПопробуй изменить область или повторить анализ.");
        }
        else
        {
            _help.SetRoom(bestRoom);
        }
        _help.ApplyVisibility();
    }

    private static readonly HashSet<string> TokenStop = new(StringComparer.OrdinalIgnoreCase)
    {
        "ghosting", "tangling", "starving", "renewing", "harrowing", "entangling", "blinding",
        "others", "mix", "room", "trig", "standard", "rr"
    };

    private static IEnumerable<string> ExtractTokens(string target)
    {
        var norm = Similarity.Normalize(target).Replace("/", " ").Replace("-", " ");
        if (string.IsNullOrWhiteSpace(norm)) yield break;

        foreach (var token in norm.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length < 3) continue;
            if (TokenStop.Contains(token)) continue;
            if (token.Length >= 5)
            {
                yield return token[..^1];
            }
            if (token.EndsWith("s", StringComparison.OrdinalIgnoreCase) && token.Length > 4)
            {
                yield return token[..^1];
            }
            yield return token;
        }
    }

    private static Dictionary<string, int> BuildTokenFrequency(IEnumerable<RoomInfo> rooms)
    {
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var room in rooms)
        {
            foreach (var target in room.Targets)
            {
                foreach (var token in ExtractTokens(target))
                {
                    if (freq.ContainsKey(token)) freq[token] += 1;
                    else freq[token] = 1;
                }
            }
        }
        return freq;
    }

    private sealed record RoomScore(double Score, int Hits, bool HasUnique, List<string> Matched, HashSet<string> MatchedSet);

    private static int BestToken(string token, List<string> ocrTokens, string ocrNorm)
    {
        if (string.IsNullOrWhiteSpace(token)) return 0;
        if (ocrNorm.Contains(token, StringComparison.Ordinal)) return 100;

        var best = 0;
        foreach (var o in ocrTokens)
        {
            if (o.Length < 3) continue;
            if (token.StartsWith(o, StringComparison.Ordinal) || o.StartsWith(token, StringComparison.Ordinal))
            {
                if (best < 94) best = 94;
            }
            var r = Similarity.Ratio(token, o);
            if (r > best) best = r;
            if (best >= 98) return best;
        }
        return best;
    }

    private static bool GateRoom(RoomInfo room, HashSet<string> matched)
    {
        bool HasAny(params string[] tokens)
        {
            foreach (var t in tokens)
            {
                if (matched.Contains(t)) return true;
            }
            return false;
        }

        var name = room.Name.ToLowerInvariant();
        if (name.Contains("rodiva")) return HasAny("rodiva", "rodiv", "rodivi");
        if (name.Contains("leshak") || name.Contains("leshaq")) return HasAny("leshak", "leshaq");
        if (name.Contains("vila")) return HasAny("vila");
        if (name.Contains("trig / drone") || name.Contains("trig/drone"))
        {
            return HasAny("sparkneedle", "sparkneedl", "swarmer", "swarme", "drone", "drones");
        }
        if (name.Contains("standard trig"))
        {
            var core = 0;
            if (HasAny("damavik", "damavi")) core++;
            if (HasAny("vedmak", "vedma")) core++;
            if (HasAny("kikimora", "kikimor", "kikimo")) core++;
            return core >= 2;
        }
        return true;
    }

    private static RoomScore ScoreRoom(RoomInfo room, string ocrNorm, Dictionary<string, int> freq)
    {
        const int Threshold = 85;
        var hits = 0;
        var sum = 0.0;
        var wsum = 0.0;
        var totalWeight = 0.0;
        var matched = new List<string>();
        var hasUnique = false;
        var matchedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in room.Targets)
        {
            foreach (var token in ExtractTokens(target))
            {
                tokens.Add(token);
            }
        }

        var ocrTokens = new List<string>();
        foreach (var token in ocrNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length < 3) continue;
            if (TokenStop.Contains(token)) continue;
            ocrTokens.Add(token);
        }

        foreach (var token in tokens)
        {
            var sc = BestToken(token, ocrTokens, ocrNorm);
            var count = freq.TryGetValue(token, out var c) ? c : 1;
            var weight = 1.0 / Math.Max(1, count);
            totalWeight += weight;

            if (sc >= Threshold)
            {
                if (count == 1 && sc >= 90) hasUnique = true;

                hits += 1;
                sum += weight * sc;
                wsum += weight;
                matchedSet.Add(token);
                if (matched.Count < 6 && !matched.Contains(token)) matched.Add(token);
            }
        }

        var avg = wsum > 0 ? sum / wsum : 0.0;
        var coverage = totalWeight > 0 ? wsum / totalWeight : 0.0;
        var score = avg * Math.Min(1.0, coverage * 2.0);
        if (!GateRoom(room, matchedSet))
        {
            return new RoomScore(0, 0, false, matched, matchedSet);
        }
        return new RoomScore(score, hits, hasUnique, matched, matchedSet);
    }

    private static Bitmap CaptureRegion(int left, int top, int width, int height)
    {
        var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
        return bmp;
    }
}
