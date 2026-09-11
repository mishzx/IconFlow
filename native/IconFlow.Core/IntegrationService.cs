using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace IconFlow.Core;

public sealed class IntegrationService(DataStore store)
{
    private const string FolderMenu = @"Software\Classes\Directory\shell\IconFlow.ChangeIcon";
    private const string ShortcutMenu = @"Software\Classes\lnkfile\shell\IconFlow.ChangeIcon";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static readonly string[] HistoricalRunKeys =
    [
        RunKey,
        @"Software\Microsoft\Windows\CurrentVersion\RunOnce"
    ];
    private static readonly string[] StartupApprovalKeys =
    [
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32"
    ];

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int length, char[]? name);

    public bool HasPackageIdentity
    {
        get
        {
            var length = 0;
            return GetCurrentPackageFullName(ref length, null) == 122;
        }
    }

    public bool IsModernMenuInstalled
    {
        get
        {
            if (HasPackageIdentity) return true;
            try
            {
                using var packages = Registry.CurrentUser.OpenSubKey(
                    @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages");
                return packages?.GetSubKeyNames().Any(name =>
                    name.StartsWith("IconFlow_", StringComparison.OrdinalIgnoreCase)) == true;
            }
            catch { return false; }
        }
    }

    public string ModernMenuStatus => IsModernMenuInstalled
        ? "Windows 11 新版一级菜单组件已启用。"
        : "当前为便携兼容模式；安装发布目录中的新版菜单组件后可进入 Windows 11 一级菜单。";

    public void SetContextMenu(bool enabled)
    {
        if (!enabled)
        {
            Registry.CurrentUser.DeleteSubKeyTree(FolderMenu, false);
            Registry.CurrentUser.DeleteSubKeyTree(ShortcutMenu, false);
        }
        else
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("无法确定程序路径。");
            RegisterMenu(FolderMenu, executable);
            RegisterMenu(ShortcutMenu, executable);
        }
        store.Data.Settings.ContextMenu = enabled;
        store.Save();
    }

    private static void RegisterMenu(string keyPath, string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        key.SetValue(null, "使用 IconFlow 更换图标");
        key.SetValue("Icon", executable);
        using var command = key.CreateSubKey("command");
        command.SetValue(null, $"\"{executable}\" --change-icon \"%1\"");
    }

    public void SetLaunchAtLogin(bool enabled)
    {
        RemoveHistoricalStartupEntries();
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("无法确定程序路径。");
            key.SetValue("IconFlow", $"\"{executable}\" --hidden");
        }
        else key.DeleteValue("IconFlow", false);
        store.Data.Settings.LaunchAtLogin = enabled;
        store.Save();
    }

    public bool IsLaunchAtLoginRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue("IconFlow") is string command && command.Contains(Environment.ProcessPath ?? "\0", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsContextMenuRegistered()
    {
        using var folder = Registry.CurrentUser.OpenSubKey(FolderMenu + @"\command");
        return folder?.GetValue(null) is string command && command.Contains(Environment.ProcessPath ?? "\0", StringComparison.OrdinalIgnoreCase);
    }

    public void ReconcileSafeDefaults()
    {
        var launch = IsLaunchAtLoginRegistered();
        var context = IsContextMenuRegistered();
        if (store.Data.Settings.LaunchAtLogin == launch && store.Data.Settings.ContextMenu == context) return;
        store.Data.Settings.LaunchAtLogin = launch;
        store.Data.Settings.ContextMenu = context;
        store.Save();
    }

    public void RemoveAllIntegrations()
    {
        Registry.CurrentUser.DeleteSubKeyTree(FolderMenu, false);
        Registry.CurrentUser.DeleteSubKeyTree(ShortcutMenu, false);
        RemoveHistoricalStartupEntries();
        store.Data.Settings.ContextMenu = false;
        store.Data.Settings.LaunchAtLogin = false;
        store.Save();
    }

    public static void RemoveHistoricalStartupEntries()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
                foreach (var path in HistoricalRunKeys)
                {
                    using var key = currentUser.OpenSubKey(path, writable: true);
                    if (key is null) continue;
                    foreach (var name in key.GetValueNames())
                    {
                        var command = key.GetValue(name)?.ToString() ?? string.Empty;
                        if (name.Contains("IconFlow", StringComparison.OrdinalIgnoreCase) ||
                            command.Contains("IconFlow", StringComparison.OrdinalIgnoreCase))
                            key.DeleteValue(name, false);
                    }
                }

                foreach (var path in StartupApprovalKeys)
                {
                    using var key = currentUser.OpenSubKey(path, writable: true);
                    if (key is null) continue;
                    foreach (var name in key.GetValueNames().Where(name =>
                                 name.Contains("IconFlow", StringComparison.OrdinalIgnoreCase)))
                        key.DeleteValue(name, false);
                }
            }
            catch (PlatformNotSupportedException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
