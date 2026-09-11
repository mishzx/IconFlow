using System.Diagnostics;
using IconFlow.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace IconFlow;

public sealed partial class QuickChangeWindow : Window
{
    private readonly AppServices _services = App.Current.Services;
    private readonly string _target;
    private bool _busy;
    private bool _suppressSearchRefresh;
    private CancellationTokenSource? _searchDebounce;
    private readonly List<Window> _childWindows = [];
    private string? _selectedFolderId;
    private bool _suppressNextSelection;
    public BulkObservableCollection<IconCard> Icons { get; } = [];
    public BulkObservableCollection<IconFolderItem> FolderChoices { get; } = [];

    public QuickChangeWindow(string target)
    {
        _target = target;
        InitializeComponent();
        Root.FlowDirection = Loc.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.Title = $"IconFlow - {Loc.Get("ChangeIcon", "更换图标")}";
        WindowSizing.SetAlwaysOnTop(this, true);
        WindowSizing.ResizeNearCursor(this, 470, 520);
        Root.RequestedTheme = _services.Store.Data.Settings.Theme switch { "light" => ElementTheme.Light, "dark" => ElementTheme.Dark, _ => ElementTheme.Default };
        LoadTarget();
        RefreshFolders();
        RefreshIcons();
        FolderFilter.SelectedIndex = 0;
    }

    private void LoadTarget()
    {
        try
        {
            var info = _services.Windows.Inspect(_target);
            TargetName.Text = info.Name;
            TargetPath.Text = info.Path;
        }
        catch (Exception error)
        {
            TargetName.Text = "无法打开对象";
            TargetPath.Text = _target;
            Show("对象不可用", error.Message, InfoBarSeverity.Error);
            IconGrid.IsEnabled = false;
        }
    }

    private void RefreshIcons(string? query = null)
    {
        var icons = _services.Store.Data.Icons.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query))
            icons = icons.Where(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || x.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
                || x.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
        if (_selectedFolderId is not null) icons = icons.Where(x => x.FolderId == _selectedFolderId);
        Icons.ReplaceAll(icons.OrderByDescending(x => x.Favorite)
            .ThenByDescending(x => x.LastUsedAt ?? x.ImportedAt).Select(x => new IconCard(x, _services.Images)));
        EmptyState.Visibility = Icons.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshFolders()
    {
        var values = new List<IconFolderItem> { new() { Name = Loc.Get("AllIcons", "全部图标"), Count = _services.Store.Data.Icons.Count } };
        values.AddRange(_services.Store.Data.IconFolders.OrderBy(x => x.Name).Select(x => new IconFolderItem
        {
            Id = x.Id,
            Name = x.Id == "iconflow-built-in-fluent" ? Loc.Get("BuiltInFolder", "内置图标 · Fluent 文件夹") : x.Name,
            Count = _services.Store.Data.Icons.Count(icon => icon.FolderId == x.Id)
        }));
        FolderChoices.ReplaceAll(values);
    }

    private void FolderFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FolderFilter.SelectedItem is not IconFolderItem folder) return;
        _selectedFolderId = folder.Id;
        RefreshIcons(SearchBox.Text);
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var input = new TextBox { PlaceholderText = "文件夹名称", MinWidth = 250 };
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "新建图标文件夹", Content = input, PrimaryButtonText = "创建", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            var folder = _services.Store.AddIconFolder(input.Text);
            RefreshFolders();
            FolderFilter.SelectedItem = FolderChoices.First(x => x.Id == folder.Id);
        }
        catch (Exception error) { Show("无法创建文件夹", error.Message, InfoBarSeverity.Error); }
    }

    private async void RenameCurrentFolder_Click(object sender, RoutedEventArgs e)
    {
        if (FolderFilter.SelectedItem is not IconFolderItem { Id: string id } current)
        {
            Show("请选择文件夹", "“全部图标”不能重命名。", InfoBarSeverity.Informational);
            return;
        }
        var input = new TextBox { Text = current.Name, MinWidth = 250, SelectionStart = 0, SelectionLength = current.Name.Length };
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "重命名文件夹", Content = input, PrimaryButtonText = "保存", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            _services.Store.RenameIconFolder(id, input.Text);
            RefreshFolders();
            FolderFilter.SelectedItem = FolderChoices.First(x => x.Id == id);
        }
        catch (Exception error) { Show("无法重命名", error.Message, InfoBarSeverity.Error); }
    }

    private async void IconGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressNextSelection) { _suppressNextSelection = false; IconGrid.SelectedItem = null; return; }
        if (_busy || IconGrid.SelectedItem is not IconCard card) return;
        _busy = true;
        try
        {
            await Task.Run(() => _services.Windows.Apply(_target, card.Record, "context-menu"));
            Show("已更换图标", "需要时可以立即撤销。", InfoBarSeverity.Success, true);
        }
        catch (Exception error) { Show("操作未完成", error.Message, InfoBarSeverity.Error); }
        finally { _busy = false; }
    }

    private void IconCard_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint((UIElement)sender).Properties.IsRightButtonPressed) _suppressNextSelection = true;
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg", ".bmp", ".ico", ".exe" }) picker.FileTypeFilter.Add(extension);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        await ImportAndSelect([file.Path]);
    }

    private IconCard? FindCard(string id) => Icons.FirstOrDefault(x => x.Id == id)
        ?? (_services.Store.Data.Icons.FirstOrDefault(x => x.Id == id) is { } record ? new IconCard(record, _services.Images) : null);

    private async void IconRenameMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id } || FindCard(id) is not { } card) return;
        var input = new TextBox { Text = card.Name, MinWidth = 260, SelectionStart = 0, SelectionLength = card.Name.Length };
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "重命名图标", Content = input, PrimaryButtonText = "保存", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try { _services.Store.RenameIcon(id, input.Text); RefreshIcons(SearchBox.Text); }
        catch (Exception error) { Show("重命名失败", error.Message, InfoBarSeverity.Error); }
    }

    private void IconEditMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id } || FindCard(id) is not { } card) return;
        var editor = new IconEditorWindow(card.Record);
        _childWindows.Add(editor);
        editor.Saved += icon => { RefreshIcons(SearchBox.Text); Show("已保存编辑", icon.Name, InfoBarSeverity.Success); };
        editor.Closed += (_, _) => _childWindows.Remove(editor);
        editor.Activate();
    }

    private void IconFavoriteMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id }) return;
        _services.Store.ToggleFavorite(id);
        RefreshIcons(SearchBox.Text);
    }

    private async void IconMoveMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id }) return;
        var choices = new List<IconFolderItem> { new() { Name = "不放入文件夹" } };
        choices.AddRange(_services.Store.Data.IconFolders.OrderBy(x => x.Name).Select(x => new IconFolderItem { Id = x.Id, Name = x.Name }));
        var combo = new ComboBox { ItemsSource = choices, DisplayMemberPath = nameof(IconFolderItem.Name), SelectedIndex = 0, MinWidth = 250 };
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "移动到文件夹", Content = combo, PrimaryButtonText = "移动", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || combo.SelectedItem is not IconFolderItem folder) return;
        _services.Store.MoveIconToFolder(id, folder.Id);
        RefreshFolders();
        FolderFilter.SelectedIndex = 0;
        RefreshIcons(SearchBox.Text);
    }

    private async void IconDeleteMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id } || FindCard(id) is not { } card) return;
        var usage = _services.Store.GetActiveIconUsageCount(id);
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = $"删除“{card.Name}”？", PrimaryButtonText = "删除", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Close,
            Content = usage > 0 ? $"该图标当前被 {usage} 个对象使用。已应用对象会保留安全副本。" : "图标会从本地图标库中移除。" };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try { _services.Images.DeleteIcon(id); RefreshFolders(); RefreshIcons(SearchBox.Text); Show("已删除", card.Name, InfoBarSeverity.Success); }
        catch (Exception error) { Show("删除失败", error.Message, InfoBarSeverity.Error); }
    }

    private void PinToggle_Click(object sender, RoutedEventArgs e)
    {
        var enabled = PinToggle.IsChecked == true;
        WindowSizing.SetAlwaysOnTop(this, enabled);
        PinToggle.Opacity = enabled ? 1 : .55;
    }

    private async Task ImportAndSelect(IEnumerable<string> paths)
    {
        try
        {
            IconRecord? icon = null;
            var count = 0;
            foreach (var path in paths)
            {
                if (!_services.Images.IsSupported(path)) continue;
                icon = await _services.Images.ImportAsync(path);
                count++;
            }
            if (icon is null) throw new InvalidOperationException("没有可导入的图片或图标文件。");
            _searchDebounce?.Cancel();
            _suppressSearchRefresh = true;
            SearchBox.Text = string.Empty;
            _suppressSearchRefresh = false;
            RefreshIcons();
            var selected = Icons.First(x => x.Id == icon.Id);
            IconGrid.SelectedItem = selected;
            IconGrid.ScrollIntoView(selected);
            Show("已导入", count == 1 ? icon.Name : $"已导入 {count} 枚图标，自动选择最后一枚", InfoBarSeverity.Informational);
        }
        catch (Exception error) { Show("操作未完成", error.Message, InfoBarSeverity.Error); }
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_suppressSearchRefresh) return;
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(120, _searchDebounce.Token);
            RefreshIcons(sender.Text);
        }
        catch (OperationCanceledException) { }
    }

    private void Root_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "导入并自动选择";
    }

    private async void Root_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var items = await e.DataView.GetStorageItemsAsync();
        await ImportAndSelect(items.OfType<StorageFile>().Select(x => x.Path));
    }

    private async void RestoreDefault_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(() => _services.Windows.RestoreDefault(_target));
            Show("已恢复默认", TargetName.Text, InfoBarSeverity.Success, true);
        }
        catch (Exception error) { Show("恢复失败", error.Message, InfoBarSeverity.Error); }
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Run(() => _services.Windows.Undo());
            Show("已撤销", TargetName.Text, InfoBarSeverity.Success);
        }
        catch (Exception error) { Show("撤销失败", error.Message, InfoBarSeverity.Error); }
    }

    private void OpenMain_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true });
        Close();
    }

    private void Show(string title, string message, InfoBarSeverity severity, bool canUndo = false)
    {
        StatusBar.Title = title;
        StatusBar.Message = message;
        StatusBar.Severity = severity;
        UndoButton.Visibility = canUndo ? Visibility.Visible : Visibility.Collapsed;
        StatusBar.IsOpen = true;
    }
}
