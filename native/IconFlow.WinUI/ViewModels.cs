using IconFlow.Core;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IconFlow;

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void ReplaceAll(IEnumerable<T> values)
    {
        CheckReentrancy();
        Items.Clear();
        foreach (var value in values) Items.Add(value);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

public sealed class IconCard : INotifyPropertyChanged
{
    private readonly ImageService _images;
    private BitmapImage? _preview;
    private bool _isRenaming;
    public IconRecord Record { get; }
    public string Id => Record.Id;
    public string Name => string.IsNullOrWhiteSpace(Record.BuiltInKey) ? Record.Name : Loc.BuiltInIconName(Record.BuiltInKey, Record.Name);
    public string Source => string.IsNullOrWhiteSpace(Record.BuiltInKey) ? Record.Source : Loc.Get("BuiltInSource", "IconFlow 内置");
    public string Usage => Record.UseCount == 0 ? Loc.Get("Unused", "未使用") : Loc.Format("UsedCount", "已使用 {0} 次", Record.UseCount);
    public string FavoriteGlyph => Record.Favorite ? "\uE735" : "\uE734";
    public string FavoriteText => Record.Favorite ? Loc.Get("Unfavorite", "取消收藏") : Loc.Get("Favorite", "收藏");
    public BitmapImage Preview => _preview ??= CreateBitmap(_images.GetPreviewPath(Record), 256)!;
    public bool IsRenaming => _isRenaming;
    public Visibility NameVisibility => _isRenaming ? Visibility.Collapsed : Visibility.Visible;
    public Visibility EditorVisibility => _isRenaming ? Visibility.Visible : Visibility.Collapsed;
    public event PropertyChangedEventHandler? PropertyChanged;

    public IconCard(IconRecord record, ImageService images)
    {
        Record = record;
        _images = images;
    }

    public void BeginRename()
    {
        _isRenaming = true;
        Notify(nameof(IsRenaming));
        Notify(nameof(NameVisibility));
        Notify(nameof(EditorVisibility));
    }

    public void EndRename(string? newName = null)
    {
        if (!string.IsNullOrWhiteSpace(newName)) Record.Name = newName.Trim();
        _isRenaming = false;
        Notify(nameof(Name));
        Notify(nameof(IsRenaming));
        Notify(nameof(NameVisibility));
        Notify(nameof(EditorVisibility));
    }

    private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    internal static BitmapImage? CreateBitmap(string? path, int decodeSize)
        => string.IsNullOrWhiteSpace(path) || !File.Exists(path) ? null
            : new BitmapImage(new Uri(path, UriKind.Absolute)) { DecodePixelWidth = decodeSize, DecodePixelHeight = decodeSize };
}

public sealed class IconFolderItem
{
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public string CountText => Count.ToString();
    public bool IsAll => Id is null;
}

public sealed class HistoryItem
{
    public HistoryRecord Record { get; }
    public BitmapImage? BeforePreview { get; }
    public BitmapImage? AfterPreview { get; }
    public string Time => Record.Timestamp.LocalDateTime.ToString("MM-dd HH:mm");
    public string Name => Record.TargetName;
    public string Path => Record.TargetPath;
    public string Action => Record.Method == "restore-default" ? Loc.Get("RestoreDefault", "恢复默认") : Loc.Get("ChangeIcon", "更换图标");
    public string Status => Record.Undone ? Loc.Get("Undone", "已撤销") : Loc.Get("Undoable", "可撤销");
    public bool CanUndo => !Record.Undone;

    public HistoryItem(HistoryRecord record, ImageService images)
    {
        Record = record;
        BeforePreview = CreateHistoryBitmap(images.GetReferencePreview(record.Before.IconLocation, record.Before.IconBackupPath));
        AfterPreview = record.After.Default ? null : CreateHistoryBitmap(images.GetReferencePreview(record.After.IconPath));
    }

    private static BitmapImage? CreateHistoryBitmap(string? path) => IconCard.CreateBitmap(path, 192);
}

public sealed class HistoryGroup
{
    public string Name { get; }
    public string Path { get; }
    public IReadOnlyList<HistoryItem> Items { get; }
    public int ChangeCount => Items.Count;
    public string Summary => Loc.Format("HistorySummary", "{0} 次修改 · 最近 {1}", ChangeCount, Items[0].Time);
    public BitmapImage? LatestPreview => Items[0].AfterPreview ?? Items[0].BeforePreview;
    public string? LatestUndoableId => Items.FirstOrDefault(x => x.CanUndo)?.Record.Id;
    public bool CanUndo => LatestUndoableId is not null;

    public HistoryGroup(string path, IEnumerable<HistoryItem> items)
    {
        Path = path;
        Items = items.OrderByDescending(x => x.Record.Timestamp).ToList();
        Name = Items[0].Name;
    }
}
