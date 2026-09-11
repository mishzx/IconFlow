using Microsoft.UI.Xaml;
using IconFlow.Core;
using System.Diagnostics;
using System.Text.Json;
using Windows.Globalization;

namespace IconFlow;

public partial class App : Application
{
    private readonly Stopwatch _startup = Stopwatch.StartNew();
    private Mutex? _mainInstanceMutex;
    private Window? _window;
    public AppServices Services { get; }
    public static new App Current => (App)Application.Current;

    public App()
    {
        Services = new(reconcileIntegrations: false);
        var language = Environment.GetEnvironmentVariable("ICONFLOW_QA_LANGUAGE") ?? Services.Store.Data.Settings.Language;
        if (!string.IsNullOrWhiteSpace(language) && !language.Equals("system", StringComparison.OrdinalIgnoreCase))
            Loc.ApplyLanguage(language);
        try { InitializeComponent(); }
        catch (Exception error)
        {
            var log = Environment.GetEnvironmentVariable("ICONFLOW_QA_ERROR_FILE")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IconFlow", "startup-error.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(log)!);
            File.WriteAllText(log, error.ToString());
            throw;
        }
        try { Services.Images.EnsureBuiltInIcons(Path.Combine(AppContext.BaseDirectory, "Assets", "BuiltinIcons")); }
        catch { /* 内置包不可用时不阻止主程序启动。 */ }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var commandLine = Environment.GetCommandLineArgs();
            if (commandLine.Any(x => x.Equals("--hidden", StringComparison.OrdinalIgnoreCase)))
            {
                // 开机启动只用于完成轻量维护；不创建窗口、不驻留后台。
                Services.Store.PruneMissingHistory();
                Exit();
                return;
            }

            var changeIndex = Array.FindIndex(commandLine, x => x.Equals("--change-icon", StringComparison.OrdinalIgnoreCase));
            if (changeIndex >= 0 && changeIndex + 1 < commandLine.Length)
                _window = new QuickChangeWindow(commandLine[changeIndex + 1]);
            else
            {
                _mainInstanceMutex = new Mutex(true, @"Local\IconFlow.MainWindow", out var isFirstMainWindow);
                if (!isFirstMainWindow)
                {
                    _mainInstanceMutex.Dispose();
                    _mainInstanceMutex = null;
                    Exit();
                    return;
                }
                _window = new MainWindow();
            }
            _window.Activated += Window_Activated;
            _window.Activate();
        }
        catch (Exception error)
        {
            var log = Environment.GetEnvironmentVariable("ICONFLOW_QA_ERROR_FILE")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IconFlow", "startup-error.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(log)!);
            File.WriteAllText(log, error.ToString());
            throw;
        }
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated) return;
        ((Window)sender).Activated -= Window_Activated;
        var readyFile = Environment.GetEnvironmentVariable("ICONFLOW_QA_READY_FILE");
        if (!string.IsNullOrWhiteSpace(readyFile))
        {
            var payload = new { readyMs = _startup.ElapsedMilliseconds, pid = Environment.ProcessId, mode = sender is QuickChangeWindow ? "quick" : "main" };
            File.WriteAllText(readyFile, JsonSerializer.Serialize(payload));
        }
        if (int.TryParse(Environment.GetEnvironmentVariable("ICONFLOW_QA_AUTO_EXIT_MS"), out var delay))
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(Math.Max(100, delay));
                ((Window)sender).DispatcherQueue.TryEnqueue(() => ((Window)sender).Close());
            });
        }
    }
}
