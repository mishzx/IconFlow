using System.Security.Cryptography;
using System.Text.Json;

namespace IconFlow.Core;

public sealed class DataStore
{
    private readonly object _gate = new();
    private readonly string _filePath;
    public string RootPath { get; }
    public string LibraryPath { get; private set; }
    public string ManagedPath { get; }
    public string BackupPath { get; }
    public string PreviewPath { get; private set; }
    public AppData Data { get; private set; }

    public DataStore(string? rootOverride = null)
    {
        RootPath = rootOverride
            ?? Environment.GetEnvironmentVariable("ICONFLOW_DATA_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IconFlow");
        LibraryPath = Path.Combine(RootPath, "icon-library");
        ManagedPath = Path.Combine(RootPath, "managed-icons");
        BackupPath = Path.Combine(RootPath, "backups");
        PreviewPath = Path.Combine(RootPath, "preview-cache");
        _filePath = Path.Combine(RootPath, "data.json");
        Directory.CreateDirectory(LibraryPath);
        Directory.CreateDirectory(ManagedPath);
        Directory.CreateDirectory(BackupPath);
        Directory.CreateDirectory(PreviewPath);
        Data = Load();
        ConfigureStoragePaths(Data.Settings.IconStoragePath);
        Directory.CreateDirectory(LibraryPath);
        Directory.CreateDirectory(PreviewPath);
        PruneHistory(false);
        if (PruneMissingHistory(false) > 0) Save();
    }

    private AppData Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new AppData();
            var parsed = JsonSerializer.Deserialize(File.ReadAllText(_filePath), AppJsonContext.Default.AppData) ?? new AppData();
            parsed.Version = 8;
            parsed.Icons ??= [];
            parsed.IconFolders ??= [];
            parsed.History ??= [];
            parsed.Settings ??= new AppSettings();
            return parsed;
        }
        catch
        {
            try { File.Copy(_filePath, _filePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMddHHmmss"), true); } catch { }
            return new AppData();
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            Directory.CreateDirectory(RootPath);
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Data, AppJsonContext.Default.AppData));
            File.Move(temp, _filePath, true);
        }
    }

    public IconRecord AddIcon(IconRecord icon)
    {
        lock (_gate) { Data.Icons.Insert(0, icon); Save(); return icon; }
    }

    public void AddHistory(HistoryRecord record)
    {
        lock (_gate) { Data.History.Insert(0, record); PruneHistory(false); Save(); }
    }

    public void MarkUsed(string iconId)
    {
        lock (_gate)
        {
            var icon = Data.Icons.FirstOrDefault(x => x.Id == iconId);
            if (icon is null) return;
            icon.UseCount++;
            icon.LastUsedAt = DateTimeOffset.Now;
            Save();
        }
    }

    public void ToggleFavorite(string iconId)
    {
        lock (_gate)
        {
            var icon = Data.Icons.FirstOrDefault(x => x.Id == iconId);
            if (icon is null) return;
            icon.Favorite = !icon.Favorite;
            Save();
        }
    }

    public void RenameIcon(string iconId, string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new InvalidOperationException("图标名称不能为空。");
        lock (_gate)
        {
            var icon = Data.Icons.FirstOrDefault(x => x.Id == iconId)
                ?? throw new InvalidOperationException("图标已不存在。");
            icon.Name = normalized;
            Save();
        }
    }

    public int GetActiveIconUsageCount(string iconId)
        => Data.History.Count(x => !x.Undone && x.After.IconId == iconId
            && (Directory.Exists(x.TargetPath) || File.Exists(x.TargetPath)));

    public void RemoveIcon(string iconId)
    {
        lock (_gate)
        {
            var icon = Data.Icons.FirstOrDefault(x => x.Id == iconId)
                ?? throw new InvalidOperationException("图标已不存在。");
            Data.Icons.Remove(icon);
            Save();
        }
    }

    public IconFolder AddIconFolder(string name)
    {
        var normalized = NormalizeFolderName(name);
        lock (_gate)
        {
            if (Data.IconFolders.Any(x => x.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("已经存在同名文件夹。");
            var folder = new IconFolder { Name = normalized };
            Data.IconFolders.Add(folder);
            Save();
            return folder;
        }
    }

    public void RenameIconFolder(string id, string name)
    {
        var normalized = NormalizeFolderName(name);
        lock (_gate)
        {
            var folder = Data.IconFolders.FirstOrDefault(x => x.Id == id) ?? throw new InvalidOperationException("文件夹已不存在。");
            if (Data.IconFolders.Any(x => x.Id != id && x.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("已经存在同名文件夹。");
            folder.Name = normalized;
            Save();
        }
    }

    public void MoveIconToFolder(string iconId, string? folderId)
    {
        lock (_gate)
        {
            var icon = Data.Icons.FirstOrDefault(x => x.Id == iconId) ?? throw new InvalidOperationException("图标已不存在。");
            if (folderId is not null && Data.IconFolders.All(x => x.Id != folderId)) throw new InvalidOperationException("目标文件夹已不存在。");
            icon.FolderId = folderId;
            Save();
        }
    }

    private static string NormalizeFolderName(string name)
    {
        var value = name.Trim();
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("文件夹名称不能为空。");
        return value;
    }

    public void MigrateIconStorage(string destinationRoot)
    {
        if (string.IsNullOrWhiteSpace(destinationRoot)) throw new InvalidOperationException("请选择有效的保存位置。");
        var root = Path.GetFullPath(destinationRoot.Trim());
        var targetLibrary = Path.Combine(root, "icon-library");
        var targetPreview = Path.Combine(root, "preview-cache");
        if (Path.GetFullPath(LibraryPath).Equals(Path.GetFullPath(targetLibrary), StringComparison.OrdinalIgnoreCase)) return;

        lock (_gate)
        {
            var oldLibrary = LibraryPath;
            var oldPreview = PreviewPath;
            Directory.CreateDirectory(targetLibrary);
            Directory.CreateDirectory(targetPreview);
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CopyAndVerifyReferencedFiles(Data.Icons.Select(x => x.Path), LibraryPath, targetLibrary, mappings);
            CopyAndVerifyReferencedFiles(Data.Icons.Select(x => x.PreviewPath).Where(x => !string.IsNullOrWhiteSpace(x))!, PreviewPath, targetPreview, mappings);

            foreach (var icon in Data.Icons)
            {
                if (mappings.TryGetValue(icon.Path, out var path)) icon.Path = path;
                if (icon.PreviewPath is not null && mappings.TryGetValue(icon.PreviewPath, out var preview)) icon.PreviewPath = preview;
            }
            Data.Settings.IconStoragePath = root;
            LibraryPath = targetLibrary;
            PreviewPath = targetPreview;
            Save();
            foreach (var oldPath in mappings.Keys)
            {
                if (!IsInside(oldPath, oldLibrary) && !IsInside(oldPath, oldPreview)) continue;
                if (mappings.TryGetValue(oldPath, out var newPath) && Path.GetFullPath(oldPath).Equals(Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase)) continue;
                try { File.Delete(oldPath); } catch { }
            }
        }
    }

    private static void CopyAndVerifyReferencedFiles(IEnumerable<string> paths, string sourceRoot, string destination, IDictionary<string, string> mappings)
    {
        foreach (var source in paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!IsInside(source, sourceRoot)) continue;
            var target = Path.Combine(destination, Path.GetFileName(source));
            if (!Path.GetFullPath(source).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) File.Copy(source, target, true);
            if (!DataStore.Sha256(source).Equals(DataStore.Sha256(target), StringComparison.OrdinalIgnoreCase))
                throw new IOException($"迁移校验失败：{Path.GetFileName(source)}");
            mappings[source] = target;
        }
    }

    private void ConfigureStoragePaths(string? storageRoot)
    {
        var root = string.IsNullOrWhiteSpace(storageRoot) ? RootPath : Path.GetFullPath(storageRoot);
        LibraryPath = Path.Combine(root, "icon-library");
        PreviewPath = Path.Combine(root, "preview-cache");
    }

    public void PruneHistory(bool save = true)
    {
        lock (_gate)
        {
            var cutoff = DateTimeOffset.Now.AddDays(-Math.Clamp(Data.Settings.HistoryDays, 1, 3650));
            Data.History = Data.History.Where(x => x.Timestamp >= cutoff)
                .Take(Math.Clamp(Data.Settings.HistoryLimit, 10, 5000)).ToList();
            if (save) Save();
        }
    }

    public int PruneMissingHistory(bool save = true)
    {
        lock (_gate)
        {
            var missing = Data.History.Where(x => !Directory.Exists(x.TargetPath) && !File.Exists(x.TargetPath)).ToList();
            if (missing.Count == 0) return 0;
            var retained = Data.History.Except(missing).ToList();
            var retainedAssets = retained.SelectMany(HistoryAssets).Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in missing.SelectMany(HistoryAssets).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(asset) || retainedAssets.Contains(asset)) continue;
                if (!IsInside(asset, ManagedPath) && !IsInside(asset, BackupPath)) continue;
                try { File.Delete(asset); } catch { }
            }
            Data.History = retained;
            if (save) Save();
            return missing.Count;
        }
    }

    private static IEnumerable<string> HistoryAssets(HistoryRecord record)
    {
        if (!string.IsNullOrWhiteSpace(record.Before.IconBackupPath)) yield return record.Before.IconBackupPath;
        if (!string.IsNullOrWhiteSpace(record.After.IconPath)) yield return record.After.IconPath;
    }

    private static bool IsInside(string candidate, string root)
    {
        try
        {
            var path = Path.GetFullPath(candidate);
            var basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static string Sha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
