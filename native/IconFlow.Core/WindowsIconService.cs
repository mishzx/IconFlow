using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;

namespace IconFlow.Core;

public sealed partial class WindowsIconService(DataStore store)
{
    public TargetInfo Inspect(string targetPath)
    {
        targetPath = Normalize(targetPath);
        if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            throw new FileNotFoundException("对象不存在或已被移动。", targetPath);
        var type = GetTargetType(targetPath);
        if (type is not ("folder" or "shortcut"))
            throw new InvalidOperationException("仅支持文件夹和 .lnk 快捷方式更换图标。");
        var name = Path.GetFileName(targetPath.TrimEnd(Path.DirectorySeparatorChar));
        return new TargetInfo
        {
            Path = targetPath, Name = string.IsNullOrWhiteSpace(name) ? targetPath : name, Type = type,
            CurrentIcon = type == "folder" ? ReadFolderIcon(targetPath) : ReadShortcutIcon(targetPath)
        };
    }

    public HistoryRecord Apply(string targetPath, IconRecord icon, string method = "single")
    {
        if (!File.Exists(icon.Path)) throw new FileNotFoundException("所选图标已不存在。", icon.Path);
        var target = Inspect(targetPath);
        var before = CaptureBefore(target.Path, target.Type);
        var managedIcon = Path.Combine(store.ManagedPath, Guid.NewGuid().ToString("N") + ".ico");
        File.Copy(icon.Path, managedIcon, true);
        try
        {
            if (target.Type == "folder") ApplyFolder(target.Path, managedIcon);
            else WriteShortcutIcon(target.Path, managedIcon, 0);
            Refresh(target.Path);
        }
        catch (Exception error)
        {
            try { RestoreState(target.Path, target.Type, before); } catch { }
            try { File.Delete(managedIcon); } catch { }
            throw new InvalidOperationException("修改失败，原状态已回滚。" + error.Message, error);
        }

        var record = new HistoryRecord
        {
            TargetPath = target.Path, TargetName = target.Name, TargetType = target.Type,
            Before = before, After = new TargetState { IconPath = managedIcon, IconId = icon.Id }, Method = method
        };
        store.AddHistory(record);
        store.MarkUsed(icon.Id);
        return record;
    }

    public HistoryRecord RestoreDefault(string targetPath)
    {
        var target = Inspect(targetPath);
        var before = CaptureBefore(target.Path, target.Type);
        if (target.Type == "folder") RemoveFolderIcon(target.Path);
        else WriteShortcutIcon(target.Path, string.Empty, 0);
        Refresh(target.Path);
        var record = new HistoryRecord
        {
            TargetPath = target.Path, TargetName = target.Name, TargetType = target.Type,
            Before = before, After = new TargetState { Default = true }, Method = "restore-default"
        };
        store.AddHistory(record);
        return record;
    }

    public HistoryRecord Undo(string? recordId = null)
    {
        var record = recordId is null
            ? store.Data.History.FirstOrDefault(x => x.Status == "success" && !x.Undone)
            : store.Data.History.FirstOrDefault(x => x.Id == recordId && !x.Undone);
        if (record is null) throw new InvalidOperationException("没有可以撤销的操作。");
        RestoreState(record.TargetPath, record.TargetType, record.Before);
        record.Undone = true;
        record.UndoneAt = DateTimeOffset.Now;
        store.Save();
        return record;
    }

    private TargetState CaptureBefore(string path, string type)
    {
        if (type == "folder")
        {
            var ini = Path.Combine(path, "desktop.ini");
            var folderLocation = ReadFolderIcon(path);
            return new TargetState
            {
                DesktopIniExisted = File.Exists(ini),
                DesktopIniBase64 = File.Exists(ini) ? Convert.ToBase64String(File.ReadAllBytes(ini)) : null,
                IconLocation = folderLocation,
                IconBackupPath = BackupIconSource(folderLocation)
            };
        }
        var location = ReadShortcutIcon(path);
        return new TargetState { IconLocation = location, IconBackupPath = BackupIconSource(location) };
    }

    private string? BackupIconSource(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        var source = SplitIconLocation(location).Path;
        if (!File.Exists(source)) return null;
        var extension = Path.GetExtension(source);
        var backup = Path.Combine(store.BackupPath, Guid.NewGuid().ToString("N") + extension);
        File.Copy(source, backup, true);
        return backup;
    }

    private void RestoreState(string path, string type, TargetState before)
    {
        if (!Directory.Exists(path) && !File.Exists(path)) throw new FileNotFoundException("原对象不存在，无法恢复。", path);
        if (type == "folder")
        {
            var ini = Path.Combine(path, "desktop.ini");
            ClearProtectedAttributes(ini);
            if (before.DesktopIniExisted && before.DesktopIniBase64 is not null)
            {
                var bytes = Convert.FromBase64String(before.DesktopIniBase64);
                if (!string.IsNullOrWhiteSpace(before.IconLocation))
                {
                    var original = SplitIconLocation(before.IconLocation).Path;
                    if (!File.Exists(original) && File.Exists(before.IconBackupPath))
                    {
                        var content = bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe
                            ? Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2)
                            : Encoding.Default.GetString(bytes);
                        content = UpsertIconResource(content, before.IconBackupPath!);
                        bytes = new UnicodeEncoding(false, true).GetPreamble().Concat(new UnicodeEncoding(false, true).GetBytes(content)).ToArray();
                    }
                }
                File.WriteAllBytes(ini, bytes);
                File.SetAttributes(ini, File.GetAttributes(ini) | FileAttributes.Hidden | FileAttributes.System);
            }
            else if (File.Exists(ini)) File.Delete(ini);
        }
        else
        {
            var split = SplitIconLocation(before.IconLocation ?? string.Empty);
            if (!string.IsNullOrEmpty(split.Path) && !File.Exists(split.Path) && File.Exists(before.IconBackupPath))
                split = (before.IconBackupPath!, split.Index);
            WriteShortcutIcon(path, split.Path, split.Index);
        }
        Refresh(path);
    }

    private static string GetTargetType(string path)
        => Directory.Exists(path) ? "folder" : Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase) ? "shortcut" : "file";

    private static string Normalize(string path) => Path.GetFullPath(path.Trim().Trim('"'));

    private static void ApplyFolder(string folderPath, string iconPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        var content = File.Exists(iniPath) ? ReadIni(iniPath) : "[.ShellClassInfo]\r\n";
        content = UpsertIconResource(content, iconPath);
        ClearProtectedAttributes(iniPath);
        File.WriteAllText(iniPath, content, new UnicodeEncoding(false, true));
        File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
        File.SetAttributes(folderPath, File.GetAttributes(folderPath) | FileAttributes.ReadOnly);
    }

    private static void RemoveFolderIcon(string folderPath)
    {
        var iniPath = Path.Combine(folderPath, "desktop.ini");
        if (!File.Exists(iniPath)) return;
        var text = IconResourceLine().Replace(ReadIni(iniPath), string.Empty);
        text = IconFileLine().Replace(text, string.Empty);
        text = IconIndexLine().Replace(text, string.Empty);
        ClearProtectedAttributes(iniPath);
        if (string.IsNullOrWhiteSpace(text.Replace("[.ShellClassInfo]", string.Empty, StringComparison.OrdinalIgnoreCase))) File.Delete(iniPath);
        else
        {
            File.WriteAllText(iniPath, text, new UnicodeEncoding(false, true));
            File.SetAttributes(iniPath, FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
        }
    }

    private static string? ReadFolderIcon(string folderPath)
    {
        var ini = Path.Combine(folderPath, "desktop.ini");
        if (!File.Exists(ini)) return null;
        var content = ReadIni(ini);
        var match = IconResourceLine().Match(content);
        if (match.Success) return match.Groups[1].Value.Trim();
        var file = IconFileLine().Match(content);
        return file.Success ? file.Groups[1].Value.Trim() : null;
    }

    private static string ReadIni(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf) return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        return Encoding.Default.GetString(bytes);
    }

    private static string UpsertIconResource(string content, string iconPath)
    {
        var line = "IconResource=" + iconPath + ",0";
        if (IconResourceLine().IsMatch(content)) return IconResourceLine().Replace(content, line);
        if (!content.Contains("[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase)) content = "[.ShellClassInfo]\r\n" + content;
        var section = content.IndexOf("[.ShellClassInfo]", StringComparison.OrdinalIgnoreCase) + "[.ShellClassInfo]".Length;
        return content.Insert(section, "\r\n" + line);
    }

    private static string? ReadShortcutIcon(string shortcutPath)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            ((IPersistFile)link).Load(shortcutPath, 0);
            var builder = new StringBuilder(32768);
            link.GetIconLocation(builder, builder.Capacity, out var index);
            return builder.Length == 0 ? null : builder + "," + index;
        }
        finally { Marshal.FinalReleaseComObject(link); }
    }

    private static void WriteShortcutIcon(string shortcutPath, string iconPath, int iconIndex)
    {
        var link = (IShellLinkW)new ShellLink();
        try
        {
            var persist = (IPersistFile)link;
            persist.Load(shortcutPath, 0);
            link.SetIconLocation(iconPath, iconIndex);
            persist.Save(shortcutPath, true);
        }
        finally { Marshal.FinalReleaseComObject(link); }
    }

    private static (string Path, int Index) SplitIconLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location)) return (string.Empty, 0);
        var match = Regex.Match(location, @"^(.*),(-?\d+)$");
        return match.Success && int.TryParse(match.Groups[2].Value, out var index)
            ? (match.Groups[1].Value, index) : (location, 0);
    }

    private static void ClearProtectedAttributes(string path)
    {
        if (!File.Exists(path)) return;
        var attributes = File.GetAttributes(path) & ~(FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);
        File.SetAttributes(path, attributes);
    }

    private static void Refresh(string targetPath)
    {
        NativeMethods.SHChangeNotify(0x00002000, 0x0005, targetPath, null);
        NativeMethods.SHChangeNotify(0x08000000, 0, null, null);
        try { File.SetLastWriteTime(targetPath, DateTime.Now); } catch { }
    }

    [GeneratedRegex(@"(?im)^\s*IconResource\s*=\s*(.+?)\s*$")]
    private static partial Regex IconResourceLine();
    [GeneratedRegex(@"(?im)^\s*IconFile\s*=\s*(.+?)\s*$")]
    private static partial Regex IconFileLine();
    [GeneratedRegex(@"(?im)^\s*IconIndex\s*=\s*(.+?)\s*$")]
    private static partial Regex IconIndexLine();
}
