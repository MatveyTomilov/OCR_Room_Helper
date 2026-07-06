using System;
using System.IO;
using System.Windows;

namespace AbyssOverlay;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Log(args.Exception, "DispatcherUnhandledException");
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                Logger.Log(ex, "UnhandledException");
            }
        };

        var appDir = AppContext.BaseDirectory;
        var cwd = Environment.CurrentDirectory;
        var preferred = FindFirstFile(new[]
        {
            Path.Combine(appDir, "T6_Exotic.xlsx"),
            Path.Combine(cwd, "T6_Exotic.xlsx"),
            Path.Combine(Directory.GetParent(cwd)?.FullName ?? cwd, "T6_Exotic.xlsx"),
            Path.Combine(Directory.GetParent(appDir)?.FullName ?? appDir, "T6_Exotic.xlsx"),
            Path.Combine(appDir, "12312.xlsx"),
            Path.Combine(cwd, "12312.xlsx"),
            Path.Combine(Directory.GetParent(cwd)?.FullName ?? cwd, "12312.xlsx"),
            Path.Combine(Directory.GetParent(appDir)?.FullName ?? appDir, "12312.xlsx")
        });
        var xlsxPath = preferred ?? Path.Combine(appDir, "T6_Exotic.xlsx");

        var configPath = Path.Combine(appDir, "config.json");
        var config = AppConfig.Load(configPath);

        var roomStore = new RoomStore(xlsxPath);
        var ocr = new OcrService();

        Logger.Init(Path.Combine(appDir, "AbyssOverlay.log"));
        Logger.Log("Starting app");

        var helpWindow = new HelpOverlayWindow(config);
        var settingsWindow = new SettingsWindow(config, roomStore, ocr, helpWindow);
        var mainWindow = new MainWindow(config, roomStore, ocr, settingsWindow, helpWindow);

        MainWindow = mainWindow;
        mainWindow.Show();

        if (config.IsFirstRun)
        {
            settingsWindow.Show();
            settingsWindow.Activate();
            Logger.Log("First run: opened settings window");
        }
    }

    private static string? FindFirstFile(string[] candidates)
    {
        foreach (var c in candidates)
        {
            if (File.Exists(c))
            {
                return c;
            }
        }

        return null;
    }
}
