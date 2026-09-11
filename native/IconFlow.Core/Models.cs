using System.Text.Json.Serialization;

namespace IconFlow.Core;

public sealed class AppData
{
    public int Version { get; set; } = 8;
    public List<IconRecord> Icons { get; set; } = [];
    public List<IconFolder> IconFolders { get; set; } = [];
    public List<HistoryRecord> History { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}

public sealed class AppSettings
{
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "system";
    public bool InstantApply { get; set; } = true;
    public bool ContextMenu { get; set; }
    public bool LaunchAtLogin { get; set; }
    public bool KeepInTray { get; set; }
    public int HistoryLimit { get; set; } = 500;
    public int HistoryDays { get; set; } = 90;
    public string? IconStoragePath { get; set; }
    public int BuiltInIconPackVersion { get; set; }
}

public sealed class IconEditOptions
{
    public string AspectRatio { get; set; } = "original";
    public double Zoom { get; set; } = 1;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Padding { get; set; } = .06;
    public double CornerRadius { get; set; }
    public string BottomShape { get; set; } = "none";
    public int BackgroundArgb { get; set; } = unchecked((int)0xFF5B6EF5);
    public bool RemoveBackgroundColor { get; set; }
    public int BackgroundRemovalArgb { get; set; } = unchecked((int)0xFFFFFFFF);
    public int BackgroundTolerance { get; set; } = 24;
}

public sealed class IconRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "未命名图标";
    public string Path { get; set; } = string.Empty;
    public string? OriginalPath { get; set; }
    public string Source { get; set; } = "本地导入";
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? LastUsedAt { get; set; }
    public int UseCount { get; set; }
    public bool Favorite { get; set; }
    public List<string> Tags { get; set; } = [];
    public string Category { get; set; } = "本地导入";
    public string Hash { get; set; } = string.Empty;
    public string? PreviewPath { get; set; }
    public string? FolderId { get; set; }
    public string? BuiltInKey { get; set; }
}

public sealed class IconFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新建文件夹";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}

public sealed class HistoryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string TargetPath { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public TargetState Before { get; set; } = new();
    public TargetState After { get; set; } = new();
    public string Method { get; set; } = "single";
    public string Status { get; set; } = "success";
    public bool Undone { get; set; }
    public DateTimeOffset? UndoneAt { get; set; }
}

public sealed class TargetState
{
    public bool DesktopIniExisted { get; set; }
    public string? DesktopIniBase64 { get; set; }
    public string? IconLocation { get; set; }
    public string? IconBackupPath { get; set; }
    public string? IconPath { get; set; }
    public string? IconId { get; set; }
    public bool Default { get; set; }
}

public sealed class TargetInfo
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? CurrentIcon { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(AppData))]
internal partial class AppJsonContext : JsonSerializerContext;
