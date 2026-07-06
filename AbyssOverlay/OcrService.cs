using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AbyssOverlay;

public sealed class OcrService
{
    private readonly string? _tesseractPath;
    private string? _tessdataDir;
    private readonly HashSet<string> _checkedLangs = new(StringComparer.OrdinalIgnoreCase);
    public string? LastError { get; private set; }
    public string? LastTesseractPath => _tesseractPath;

    public OcrService()
    {
        _tesseractPath = DetectTesseract();
        _tessdataDir = DetectTessdataDir(_tesseractPath);
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_tesseractPath);

    public async Task<string> RecognizeAsync(Bitmap bmp, string lang = "rus+eng")
    {
        if (string.IsNullOrWhiteSpace(_tesseractPath))
        {
            return string.Empty;
        }

        LastError = null;

        await EnsureTessdataAsync(lang);

        var tempDir = Path.Combine(Path.GetTempPath(), "abyss_ocr");
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, $"cap_{Guid.NewGuid():N}.png");
        Bitmap? processed = null;

        try
        {
            processed = Preprocess(bmp);
            processed.Save(inputPath, System.Drawing.Imaging.ImageFormat.Png);

            var psi = new ProcessStartInfo
            {
                FileName = _tesseractPath,
                Arguments = $"\"{inputPath}\" stdout -l {lang} --oem 3 --psm 6",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (!string.IsNullOrWhiteSpace(_tessdataDir))
            {
                psi.Environment["TESSDATA_PREFIX"] = _tessdataDir;
            }

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return string.Empty;
            }

            var output = await proc.StandardOutput.ReadToEndAsync();
            var err = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                LastError = string.IsNullOrWhiteSpace(err) ? $"Tesseract exit code {proc.ExitCode}" : err.Trim();
                return string.Empty;
            }

            return output;
        }
        catch
        {
            LastError = "OCR failed";
            return string.Empty;
        }
        finally
        {
            try { File.Delete(inputPath); } catch { }
            try { processed?.Dispose(); } catch { }
        }
    }

    private static string? DetectTesseract()
    {
        var portableRoot = Path.Combine(AppContext.BaseDirectory, "tesseract");
        if (Directory.Exists(portableRoot))
        {
            var found = Directory.GetFiles(portableRoot, "tesseract.exe", SearchOption.AllDirectories);
            if (found.Length > 0)
            {
                return found[0];
            }
        }

        var toolsRoot = Path.Combine(AppContext.BaseDirectory, "tools", "tesseract");
        if (Directory.Exists(toolsRoot))
        {
            var found = Directory.GetFiles(toolsRoot, "tesseract.exe", SearchOption.AllDirectories);
            if (found.Length > 0)
            {
                return found[0];
            }
        }

        var candidates = new[]
        {
            "tesseract",
            @"C:\\Program Files\\Tesseract-OCR\\tesseract.exe",
            @"C:\\Program Files (x86)\\Tesseract-OCR\\tesseract.exe"
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                return c;
            }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "tesseract",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadLine();
                proc.WaitForExit(1000);
                if (!string.IsNullOrWhiteSpace(output) && File.Exists(output))
                {
                    return output.Trim();
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? DetectTessdataDir(string? tesseractPath)
    {
        var appTess = Path.Combine(AppContext.BaseDirectory, "tessdata");
        if (Directory.Exists(appTess))
        {
            return appTess;
        }

        var cwdTess = Path.Combine(Environment.CurrentDirectory, "tessdata");
        if (Directory.Exists(cwdTess))
        {
            return cwdTess;
        }

        if (!string.IsNullOrWhiteSpace(tesseractPath))
        {
            var dir = Path.GetDirectoryName(tesseractPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                var candidate = Path.Combine(dir, "tessdata");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private async Task EnsureTessdataAsync(string lang)
    {
        if (_tessdataDir == null)
        {
            _tessdataDir = Path.Combine(AppContext.BaseDirectory, "tessdata");
        }

        Directory.CreateDirectory(_tessdataDir);

        var langs = lang.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var l in langs)
        {
            if (_checkedLangs.Contains(l))
            {
                continue;
            }

            var path = Path.Combine(_tessdataDir, $"{l}.traineddata");
            if (!File.Exists(path) || new FileInfo(path).Length < 1024 * 100)
            {
                var url = $"https://github.com/tesseract-ocr/tessdata_fast/raw/main/{l}.traineddata";
                try
                {
                    using var http = new System.Net.Http.HttpClient();
                    var data = await http.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(path, data);
                }
                catch (Exception ex)
                {
                    LastError = $"Failed to download {l}.traineddata: {ex.Message}";
                }
            }

            _checkedLangs.Add(l);
        }
    }

    private static Bitmap Preprocess(Bitmap src)
    {
        var scale = 2;
        var w = Math.Max(1, src.Width * scale);
        var h = Math.Max(1, src.Height * scale);
        var dst = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(dst);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        using var attrs = new System.Drawing.Imaging.ImageAttributes();
        var cm = new System.Drawing.Imaging.ColorMatrix(new[]
        {
            new float[] {0.3f, 0.3f, 0.3f, 0, 0},
            new float[] {0.59f, 0.59f, 0.59f, 0, 0},
            new float[] {0.11f, 0.11f, 0.11f, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
        });
        attrs.SetColorMatrix(cm);

        g.DrawImage(src, new Rectangle(0, 0, w, h), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attrs);
        return dst;
    }
}
