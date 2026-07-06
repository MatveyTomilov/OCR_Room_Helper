using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbyssOverlay;

public sealed class AppConfig
{
    public int? RegionLeft { get; set; }
    public int? RegionTop { get; set; }
    public int? RegionWidth { get; set; }
    public int? RegionHeight { get; set; }

    public double ButtonLeft { get; set; } = 20;
    public double ButtonTop { get; set; } = 20;

    public double HelpLeft { get; set; } = 80;
    public double HelpTop { get; set; } = 80;
    public double HelpWidth { get; set; } = 520;
    public double HelpHeight { get; set; } = 380;

    public bool HelpVisible { get; set; } = true;
    public bool HelpLocked { get; set; } = false;
    public double HelpOpacity { get; set; } = 0.9;

    public int DisplayMonitor { get; set; } = 1;

    public string OcrLang { get; set; } = "rus+eng";

    [JsonIgnore]
    public string ConfigPath { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsFirstRun { get; set; }

    public static AppConfig Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                cfg.ConfigPath = path;
                cfg.IsFirstRun = false;
                return cfg;
            }
        }
        catch
        {
        }

        return new AppConfig { ConfigPath = path, IsFirstRun = true };
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            return;
        }

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ConfigPath, json);
    }
}
