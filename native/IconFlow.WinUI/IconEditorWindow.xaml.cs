using IconFlow.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace IconFlow;

public sealed partial class IconEditorWindow : Window
{
    private readonly AppServices _services = App.Current.Services;
    private readonly IconRecord _icon;
    private CancellationTokenSource? _previewDebounce;
    private string? _previewPath;
    private bool _ready;
    public event Action<IconRecord>? Saved;

    public IconEditorWindow(IconRecord icon)
    {
        _icon = icon;
        InitializeComponent();
        Root.FlowDirection = Loc.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.Title = $"{Loc.Get("Edit", "编辑")} - {icon.Name}";
        Root.RequestedTheme = _services.Store.Data.Settings.Theme switch { "light" => ElementTheme.Light, "dark" => ElementTheme.Dark, _ => ElementTheme.Default };
        AspectCombo.SelectedIndex = 0;
        ShapeCombo.SelectedIndex = 0;
        ColorCombo.SelectedIndex = 0;
        RemoveColorCombo.SelectedIndex = 0;
        WindowSizing.ResizeAndCenter(this, 780, 590);
        Closed += (_, _) => CleanupPreview();
        _ready = true;
        QueuePreview();
    }

    private IconEditOptions Options()
    {
        var colorText = (ColorCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "FF5B6EF5";
        var removalColorText = (RemoveColorCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "FFFFFFFF";
        return new IconEditOptions
        {
            AspectRatio = (AspectCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "original",
            Zoom = ZoomSlider.Value, OffsetX = OffsetXSlider.Value, OffsetY = OffsetYSlider.Value,
            Padding = PaddingSlider.Value, CornerRadius = CornerSlider.Value,
            BottomShape = (ShapeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "none",
            BackgroundArgb = unchecked((int)Convert.ToUInt32(colorText, 16)),
            RemoveBackgroundColor = RemoveBackgroundToggle.IsOn,
            BackgroundRemovalArgb = unchecked((int)Convert.ToUInt32(removalColorText, 16)),
            BackgroundTolerance = (int)Math.Round(RemoveToleranceSlider.Value)
        };
    }

    private void RemoveBackground_Changed(object sender, RoutedEventArgs e)
    {
        RemoveColorCombo.IsEnabled = RemoveBackgroundToggle.IsOn;
        RemoveToleranceSlider.IsEnabled = RemoveBackgroundToggle.IsOn;
        if (_ready) QueuePreview();
    }

    private void Control_Changed(object sender, object e)
    {
        if (_ready) QueuePreview();
    }

    private void QueuePreview()
    {
        _previewDebounce?.Cancel();
        _previewDebounce = new CancellationTokenSource();
        var token = _previewDebounce.Token;
        var options = Options();
        _ = RenderPreviewAsync(options, token);
    }

    private async Task RenderPreviewAsync(IconEditOptions options, CancellationToken token)
    {
        try
        {
            await Task.Delay(70, token);
            PreviewProgress.IsActive = true;
            var path = Path.Combine(_services.Store.PreviewPath, "editor-" + Guid.NewGuid().ToString("N") + ".png");
            await Task.Run(() => _services.Images.RenderEditPreview(_icon, options, path), token);
            if (token.IsCancellationRequested) { TryDelete(path); return; }
            var old = _previewPath;
            _previewPath = path;
            PreviewImage.Source = new BitmapImage(new Uri(path)) { DecodePixelWidth = 512, DecodePixelHeight = 512 };
            if (old is not null) TryDelete(old);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { StatusText.Text = error.Message; }
        finally { if (!token.IsCancellationRequested) PreviewProgress.IsActive = false; }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = Loc.Get("GeneratingSizes", "正在生成 7 种尺寸…");
            var saved = await Task.Run(() => _services.Images.ApplyEdit(_icon, Options()));
            Saved?.Invoke(saved);
            Close();
        }
        catch (Exception error) { StatusText.Text = error.Message; }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void CleanupPreview() { _previewDebounce?.Cancel(); if (_previewPath is not null) TryDelete(_previewPath); }
    private static void TryDelete(string path) { try { File.Delete(path); } catch { } }
}
