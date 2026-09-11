using IconFlow.Core;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.Globalization;

namespace IconFlow;

public sealed partial class MainWindow : Window
{
    private readonly AppServices _services = App.Current.Services;
    private string? _selectedTarget;
    private bool _settingsReady;
    private bool _selectionBusy;
    private CancellationTokenSource? _searchDebounce;
    private string? _lastLibraryClickId;
    private DateTimeOffset _lastLibraryClickAt;
    private readonly List<Window> _childWindows = [];
    private string? _selectedFolderId;

    public BulkObservableCollection<IconCard> RecentIcons { get; } = [];
    public BulkObservableCollection<IconCard> FilteredIcons { get; } = [];
    public BulkObservableCollection<IconFolderItem> IconFolders { get; } = [];
    public BulkObservableCollection<HistoryGroup> HistoryGroups { get; } = [];
    public IReadOnlyList<LanguageOption> LanguageOptions => Loc.Languages;

    public MainWindow()
    {
        InitializeComponent();
        if (NavView.SettingsItem is NavigationViewItem settingsItem)
            settingsItem.Content = Loc.Get("SettingsTitle", "设置");
        Root.FlowDirection = Loc.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.Title = "IconFlow";
        WindowSizing.ResizeAndCenter(this, 1080, 730);
        ApplyTheme(_services.Store.Data.Settings.Theme);
        InitializeSettings();
        RefreshAll();
        FolderList.SelectedIndex = 0;
        NavView.SelectedItem = HomeNav;
        Activated += MainWindow_Activated;
    }

    private void RefreshAll()
    {
        RefreshFolders();
        RefreshIcons();
        RefreshHistory();
        var last = _services.Store.Data.History.FirstOrDefault();
        LastActionName.Text = last?.TargetName ?? Loc.Get("NoHistoryValue", "还没有修改记录");
        LastActionTime.Text = last is null ? Loc.Get("AutoBackupValue", "修改前会自动备份") : last.Timestamp.LocalDateTime.ToString("g");
        DataPathText.Text = _services.Store.RootPath;
        IconStoragePathText.Text = _services.Store.Data.Settings.IconStoragePath ?? _services.Store.RootPath;
        ModernMenuStatusText.Text = _services.Integration.IsModernMenuInstalled
            ? Loc.Get("ModernMenuEnabled", "Windows 11 新版一级菜单组件已启用。")
            : Loc.Get("ModernMenuPortable", "当前为便携兼容模式；安装新版菜单组件后可进入 Windows 11 一级菜单。");
    }

    private void RefreshIcons(string? query = null)
    {
        var icons = _services.Store.Data.Icons.ToList();
        var recent = icons.OrderByDescending(x => x.LastUsedAt ?? x.ImportedAt).Take(8).Select(x => new IconCard(x, _services.Images));
        Replace(RecentIcons, recent);
        if (!string.IsNullOrWhiteSpace(query))
        {
            icons = icons.Where(x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || x.Source.Contains(query, StringComparison.OrdinalIgnoreCase)
                || x.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        if (_selectedFolderId is not null) icons = icons.Where(x => x.FolderId == _selectedFolderId).ToList();
        Replace(FilteredIcons, icons.OrderByDescending(x => x.Favorite).ThenByDescending(x => x.LastUsedAt ?? x.ImportedAt).Select(x => new IconCard(x, _services.Images)));
    }

    private void RefreshFolders()
    {
        var values = new List<IconFolderItem> { new() { Name = Loc.Get("AllIcons", "全部图标"), Count = _services.Store.Data.Icons.Count } };
        values.AddRange(_services.Store.Data.IconFolders.OrderBy(x => x.Name).Select(x => new IconFolderItem
        {
            Id = x.Id, Name = x.Id == "iconflow-built-in-fluent" ? Loc.Get("BuiltInFolder", "内置图标 · Fluent 文件夹") : x.Name,
            Count = _services.Store.Data.Icons.Count(icon => icon.FolderId == x.Id)
        }));
        IconFolders.ReplaceAll(values);
    }

    private void RefreshHistory()
    {
        _services.Store.PruneMissingHistory();
        var groups = _services.Store.Data.History
            .GroupBy(x => x.TargetPath, StringComparer.OrdinalIgnoreCase)
            .Select(group => new HistoryGroup(group.Key, group.Select(x => new HistoryItem(x, _services.Images))))
            .OrderByDescending(group => group.Items[0].Record.Timestamp);
        Replace(HistoryGroups, groups);
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState != WindowActivationState.Deactivated && HistoryPage.Visibility == Visibility.Visible)
            RefreshHistory();
    }

    private static void Replace<T>(BulkObservableCollection<T> target, IEnumerable<T> values) => target.ReplaceAll(values);

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        HomePage.Visibility = LibraryPage.Visibility = HistoryPage.Visibility = SettingsPage.Visibility = Visibility.Collapsed;
        if (args.IsSettingsSelected)
        {
            RefreshIntegrationSettings();
            SettingsPage.Visibility = Visibility.Visible;
        }
        else
        {
            var tag = args.SelectedItemContainer?.Tag?.ToString();
            if (tag == "library") LibraryPage.Visibility = Visibility.Visible;
            else if (tag == "history") { RefreshHistory(); HistoryPage.Visibility = Visibility.Visible; }
            else HomePage.Visibility = Visibility.Visible;
        }
    }

    private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializePicker(picker);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null) SelectTarget(folder.Path);
    }

    private async void ChooseShortcut_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".lnk");
        InitializePicker(picker);
        var file = await picker.PickSingleFileAsync();
        if (file is not null) SelectTarget(file.Path);
    }

    private void SelectTarget(string path)
    {
        try
        {
            var target = _services.Windows.Inspect(path);
            _selectedTarget = target.Path;
            SelectedTargetText.Text = target.Path;
            ShowInfo("已选择", target.Name, InfoBarSeverity.Informational);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg", ".bmp", ".ico", ".exe" }) picker.FileTypeFilter.Add(extension);
        InitializePicker(picker);
        var files = await picker.PickMultipleFilesAsync();
        if (files.Count == 0) return;
        await ImportFiles(files.Select(x => x.Path));
    }

    private async Task ImportFiles(IEnumerable<string> paths)
    {
        try
        {
            StatusBar.IsOpen = false;
            var count = 0;
            foreach (var path in paths) { await _services.Images.ImportAsync(path); count++; }
            RefreshIcons(SearchBox.Text);
            ShowInfo("导入完成", $"已处理 {count} 个文件", InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private async void IconGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectionBusy || ((GridView)sender).SelectedItem is not IconCard card) return;
        _selectionBusy = true;
        try
        {
            if (string.IsNullOrWhiteSpace(_selectedTarget))
            {
                ShowInfo("先选择对象", "拖入或选择一个文件夹/快捷方式。", InfoBarSeverity.Informational);
                return;
            }
            await Task.Run(() => _services.Windows.Apply(_selectedTarget, card.Record));
            RefreshAll();
            ShowInfo("已更换图标", Path.GetFileName(_selectedTarget.TrimEnd(Path.DirectorySeparatorChar)), InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
        finally
        {
            ((GridView)sender).SelectedItem = null;
            _selectionBusy = false;
        }
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var record = await Task.Run(() => _services.Windows.Undo());
            RefreshAll();
            ShowInfo("已撤销", record.TargetName, InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _searchDebounce?.Cancel();
        _searchDebounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(120, _searchDebounce.Token);
            RefreshIcons(sender.Text);
        }
        catch (OperationCanceledException) { }
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id })
        {
            _services.Store.ToggleFavorite(id);
            RefreshIcons(SearchBox.Text);
        }
    }

    private IconCard? FindIconCard(string id) => FilteredIcons.FirstOrDefault(x => x.Id == id);

    private void IconRenameMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string id } && FindIconCard(id) is { } card) BeginRename(card);
    }

    private void IconFavoriteMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id }) return;
        _services.Store.ToggleFavorite(id);
        RefreshIcons(SearchBox.Text);
    }

    private void IconEditMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id } || FindIconCard(id) is not { } card) return;
        var editor = new IconEditorWindow(card.Record);
        _childWindows.Add(editor);
        editor.Saved += icon =>
        {
            RefreshIcons(SearchBox.Text);
            ShowInfo("已保存编辑", icon.Name, InfoBarSeverity.Success);
        };
        editor.Closed += (_, _) => _childWindows.Remove(editor);
        editor.Activate();
    }

    private async void IconMoveMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id }) return;
        await MoveIconWithDialog(id);
    }

    private async Task MoveIconWithDialog(string iconId)
    {
        var choices = new List<IconFolderItem> { new() { Name = "不放入文件夹" } };
        choices.AddRange(_services.Store.Data.IconFolders.OrderBy(x => x.Name).Select(x => new IconFolderItem { Id = x.Id, Name = x.Name }));
        var combo = new ComboBox { ItemsSource = choices, DisplayMemberPath = nameof(IconFolderItem.Name), SelectedIndex = 0, MinWidth = 260 };
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "移动到文件夹", Content = combo, PrimaryButtonText = "移动", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary || combo.SelectedItem is not IconFolderItem folder) return;
        _services.Store.MoveIconToFolder(iconId, folder.Id);
        RefreshFolders();
        RefreshIcons(SearchBox.Text);
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var input = new TextBox { PlaceholderText = "文件夹名称", MinWidth = 280 };
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "新建图标文件夹", Content = input, PrimaryButtonText = "创建", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            var folder = _services.Store.AddIconFolder(input.Text);
            RefreshFolders();
            FolderList.SelectedItem = IconFolders.FirstOrDefault(x => x.Id == folder.Id);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private async void RenameFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id }) return;
        var folder = _services.Store.Data.IconFolders.FirstOrDefault(x => x.Id == id);
        if (folder is null) return;
        var input = new TextBox { Text = folder.Name, MinWidth = 280, SelectionStart = 0, SelectionLength = folder.Name.Length };
        var dialog = new ContentDialog { XamlRoot = Root.XamlRoot, Title = "重命名文件夹", Content = input, PrimaryButtonText = "保存", CloseButtonText = "取消", DefaultButton = ContentDialogButton.Primary };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try { _services.Store.RenameIconFolder(id, input.Text); RefreshFolders(); }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void FolderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FolderList.SelectedItem is not IconFolderItem folder) return;
        _selectedFolderId = folder.Id;
        RefreshIcons(SearchBox.Text);
    }

    private void IconCard_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is not FrameworkElement { DataContext: IconCard card }) return;
        args.Data.SetText(card.Id);
        args.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private void Folder_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.Text)) e.AcceptedOperation = DataPackageOperation.Move;
    }

    private async void Folder_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.Text) || sender is not FrameworkElement { DataContext: IconFolderItem folder }) return;
        var iconId = await e.DataView.GetTextAsync();
        try
        {
            _services.Store.MoveIconToFolder(iconId, folder.Id);
            RefreshFolders();
            RefreshIcons(SearchBox.Text);
            ShowInfo("已整理", folder.IsAll ? "已移出文件夹" : $"已移动到“{folder.Name}”", InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private async void IconDeleteMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string id } || FindIconCard(id) is not { } card) return;
        var usage = _services.Store.GetActiveIconUsageCount(id);
        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot, Title = $"删除“{card.Name}”？", PrimaryButtonText = "删除",
            CloseButtonText = "取消", DefaultButton = ContentDialogButton.Close,
            Content = usage > 0 ? $"该图标当前被 {usage} 个对象使用。已应用对象会继续使用安全副本，但图标会从库中移除。" : "图标会从本地图标库中移除。"
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            _services.Images.DeleteIcon(id);
            RefreshIcons(SearchBox.Text);
            ShowInfo("已删除", card.Name, InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void LibraryGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not IconCard card || card.IsRenaming) return;
        var now = DateTimeOffset.Now;
        var elapsed = now - _lastLibraryClickAt;
        if (_lastLibraryClickId == card.Id && elapsed >= TimeSpan.FromMilliseconds(380) && elapsed <= TimeSpan.FromSeconds(8))
        {
            BeginRename(card);
            _lastLibraryClickId = null;
            return;
        }
        _lastLibraryClickId = card.Id;
        _lastLibraryClickAt = now;
    }

    private void BeginRename(IconCard card)
    {
        foreach (var item in FilteredIcons.Where(x => x.IsRenaming)) item.EndRename();
        card.BeginRename();
        DispatcherQueue.TryEnqueue(() =>
        {
            if (LibraryGrid.ContainerFromItem(card) is not DependencyObject container) return;
            var editor = FindDescendant<TextBox>(container, box => ReferenceEquals(box.DataContext, card));
            editor?.Focus(FocusState.Programmatic);
            editor?.SelectAll();
        });
    }

    private void RenameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: IconCard card } box && card.IsRenaming) CommitRename(box, card);
    }

    private void RenameBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not TextBox { DataContext: IconCard card } box) return;
        if (e.Key == VirtualKey.Enter)
        {
            CommitRename(box, card);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Escape)
        {
            card.EndRename();
            LibraryGrid.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
    }

    private void CommitRename(TextBox box, IconCard card)
    {
        var name = box.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("图标名称不能为空。");
            box.Focus(FocusState.Programmatic);
            return;
        }
        try
        {
            _services.Store.RenameIcon(card.Id, name);
            card.EndRename(name);
            ShowInfo("已重命名", name, InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match && predicate(match)) return match;
            var nested = FindDescendant(child, predicate);
            if (nested is not null) return nested;
        }
        return null;
    }

    private async void HistoryUndo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string id } || string.IsNullOrWhiteSpace(id)) return;
        try
        {
            var record = await Task.Run(() => _services.Windows.Undo(id));
            RefreshAll();
            ShowInfo("已撤销更改", record.TargetName, InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void HistoryOpen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string path } || string.IsNullOrWhiteSpace(path)) return;
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            RefreshHistory();
            ShowError("对象已被删除，对应历史记录已清理。");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void InitializeSettings()
    {
        var settings = _services.Store.Data.Settings;
        ThemeCombo.SelectedIndex = settings.Theme switch { "light" => 1, "dark" => 2, _ => 0 };
        LanguageCombo.SelectedItem = LanguageOptions.FirstOrDefault(x => x.Code.Equals(settings.Language, StringComparison.OrdinalIgnoreCase)) ?? LanguageOptions[0];
        ContextMenuToggle.IsOn = settings.ContextMenu;
        StartToggle.IsOn = settings.LaunchAtLogin;
        _settingsReady = true;
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_settingsReady || LanguageCombo.SelectedItem is not LanguageOption option) return;
        if (_services.Store.Data.Settings.Language.Equals(option.Code, StringComparison.OrdinalIgnoreCase)) return;
        _services.Store.Data.Settings.Language = option.Code;
        _services.Store.Save();
        Loc.ApplyLanguage(option.Code);
        ShowInfo(Loc.Get("LanguageSavedTitle", "语言已保存"), Loc.Get("LanguageRestartMessage", "重新打开 IconFlow 后应用新语言。"), InfoBarSeverity.Success);
    }

    private void RefreshIntegrationSettings()
    {
        _settingsReady = false;
        _services.Integration.ReconcileSafeDefaults();
        ContextMenuToggle.IsOn = _services.Store.Data.Settings.ContextMenu;
        StartToggle.IsOn = _services.Store.Data.Settings.LaunchAtLogin;
        _settingsReady = true;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_settingsReady || ThemeCombo.SelectedItem is not ComboBoxItem item) return;
        var theme = item.Tag?.ToString() ?? "system";
        _services.Store.Data.Settings.Theme = theme;
        _services.Store.Save();
        ApplyTheme(theme);
    }

    private void ApplyTheme(string theme)
        => Root.RequestedTheme = theme switch { "light" => ElementTheme.Light, "dark" => ElementTheme.Dark, _ => ElementTheme.Default };

    private void ContextMenuToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_settingsReady) return;
        try { _services.Integration.SetContextMenu(ContextMenuToggle.IsOn); }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void InstallModernMenu_Click(object sender, RoutedEventArgs e)
    {
        var script = Path.Combine(AppContext.BaseDirectory, "Install-Win11Menu.ps1");
        if (!File.Exists(script)) { ShowError("当前开发目录尚未生成新版菜单安装组件，请使用发布版。"); return; }
        try
        {
            Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"") { UseShellExecute = true });
            ShowInfo("正在安装", "完成后重新打开 IconFlow 即可显示启用状态。", InfoBarSeverity.Informational);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void StartToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_settingsReady) return;
        try { _services.Integration.SetLaunchAtLogin(StartToggle.IsOn); }
        catch (Exception error) { ShowError(error.Message); }
    }

    private async void ChangeStorage_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        InitializePicker(picker);
        var folder = await picker.PickSingleFolderAsync();
        if (folder is null) return;
        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot, Title = "迁移图标库", PrimaryButtonText = "确认迁移", CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            Content = $"IconFlow 将复制并校验图标到：\n{folder.Path}\n\n完成后自动切换保存位置。"
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        try
        {
            ShowInfo("正在迁移", "请稍候…", InfoBarSeverity.Informational);
            await Task.Run(() => _services.Store.MigrateIconStorage(folder.Path));
            RefreshAll();
            ShowInfo("迁移完成", folder.Path, InfoBarSeverity.Success);
        }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void Root_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "拖入 IconFlow";
    }

    private async void Root_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var items = await e.DataView.GetStorageItemsAsync();
        var imports = new List<string>();
        foreach (var item in items)
        {
            if (item is StorageFolder folder) SelectTarget(folder.Path);
            else if (item is StorageFile file && file.FileType.Equals(".lnk", StringComparison.OrdinalIgnoreCase)) SelectTarget(file.Path);
            else if (item is StorageFile import && _services.Images.IsSupported(import.Path)) imports.Add(import.Path);
        }
        if (imports.Count > 0) await ImportFiles(imports);
    }

    private void InitializePicker(object picker)
        => WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

    private void ShowError(string message) => ShowInfo("操作未完成", message, InfoBarSeverity.Error);

    private void ShowInfo(string title, string message, InfoBarSeverity severity)
    {
        StatusBar.Title = title;
        StatusBar.Message = message;
        StatusBar.Severity = severity;
        StatusBar.IsOpen = true;
    }
}
