using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace IconFlow.Core;

public sealed class ImageService(DataStore store)
{
    private const int BuiltInPackVersion = 1;
    private const string PreviewVersion = "v2";
    private static readonly int[] Sizes = [16, 24, 32, 48, 64, 128, 256];
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".webp", ".svg", ".bmp", ".ico", ".exe" };
    private static readonly (string Key, string Name, string[] Tags)[] BuiltInAssets =
    [
        ("literature", "文献资料", ["书籍", "论文", "PDF", "科研"]),
        ("data", "数据分析", ["数据库", "表格", "图表"]),
        ("code", "代码开发", ["开发", "终端", "编程"]),
        ("laboratory", "实验研究", ["试管", "实验室", "科研"]),
        ("pictures", "图片相册", ["相册", "照片", "图像"]),
        ("clinical", "临床医疗", ["医疗", "病历", "健康"]),
        ("todo", "待办事项", ["提醒", "时钟", "紧急"]),
        ("completed", "已完成", ["对勾", "完成", "绿色"]),
        ("archive", "归档资料", ["存档", "收纳", "灰色"])
    ];

    public bool IsSupported(string path) => Supported.Contains(Path.GetExtension(path));
    public Task<IconRecord> ImportAsync(string path) => Task.Run(() => Import(path));

    public void EnsureBuiltInIcons(string assetsPath)
    {
        if (store.Data.Settings.BuiltInIconPackVersion >= BuiltInPackVersion) return;
        if (BuiltInAssets.Any(asset => !File.Exists(Path.Combine(assetsPath, asset.Key + ".ico"))
            || !File.Exists(Path.Combine(assetsPath, asset.Key + ".png")))) return;

        const string folderId = "iconflow-built-in-fluent";
        if (store.Data.IconFolders.All(x => x.Id != folderId))
            store.Data.IconFolders.Add(new IconFolder { Id = folderId, Name = "内置图标" });

        foreach (var asset in BuiltInAssets)
        {
            var id = "builtin-fluent-" + asset.Key;
            if (store.Data.Icons.Any(x => x.Id == id)) continue;
            var sourceIco = Path.Combine(assetsPath, asset.Key + ".ico");
            var sourcePreview = Path.Combine(assetsPath, asset.Key + ".png");
            var destinationIco = Path.Combine(store.LibraryPath, id + ".ico");
            var destinationPreview = Path.Combine(store.PreviewPath, id + "-" + PreviewVersion + ".png");
            File.Copy(sourceIco, destinationIco, true);
            File.Copy(sourcePreview, destinationPreview, true);
            store.Data.Icons.Add(new IconRecord
            {
                Id = id,
                Name = asset.Name,
                Path = destinationIco,
                Source = "IconFlow 内置",
                Category = "内置图标",
                Hash = DataStore.Sha256(destinationIco),
                PreviewPath = destinationPreview,
                FolderId = folderId,
                BuiltInKey = asset.Key,
                Tags = [.. asset.Tags]
            });
        }
        store.Data.Settings.BuiltInIconPackVersion = BuiltInPackVersion;
        store.Save();
    }

    public static void PrepareSpriteSheet(string sourcePath, string outputDirectory, int columns, int rows,
        IReadOnlyList<string> assetNames, int backgroundArgb = unchecked((int)0xFFFFFFFF), int tolerance = 24)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("图标母图不存在。", sourcePath);
        if (columns < 1 || rows < 1 || assetNames.Count != columns * rows)
            throw new ArgumentException("图标名称数量必须与网格单元格数量一致。", nameof(assetNames));
        Directory.CreateDirectory(outputDirectory);
        using var sheet = new Bitmap(sourcePath);
        for (var index = 0; index < assetNames.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var left = column * sheet.Width / columns;
            var top = row * sheet.Height / rows;
            var right = (column + 1) * sheet.Width / columns;
            var bottom = (row + 1) * sheet.Height / rows;
            using var cell = new Bitmap(right - left, bottom - top, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(cell))
            {
                ConfigureGraphics(graphics);
                graphics.DrawImage(sheet, new Rectangle(0, 0, cell.Width, cell.Height),
                    new Rectangle(left, top, cell.Width, cell.Height), GraphicsUnit.Pixel);
            }
            using var transparent = RemoveConnectedBackground(cell, Color.FromArgb(backgroundArgb), tolerance);
            using var preview = RenderSquare(transparent, 256, 0);
            preview.Save(Path.Combine(outputDirectory, assetNames[index] + ".png"), ImageFormat.Png);
            WriteMultiSizeIco(transparent, Path.Combine(outputDirectory, assetNames[index] + ".ico"), 0);
        }
    }

    public IconRecord Import(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("导入文件不存在。", path);
        if (!IsSupported(path)) throw new InvalidOperationException("支持 PNG、JPG、WEBP、SVG、BMP、ICO 和 EXE。");
        using var bitmap = LoadSource(path, 512);
        var temp = Path.Combine(store.LibraryPath, Guid.NewGuid().ToString("N") + ".tmp.ico");
        WriteMultiSizeIco(bitmap, temp);
        var hash = DataStore.Sha256(temp);
        var duplicate = store.Data.Icons.FirstOrDefault(x => string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
        if (duplicate is not null)
        {
            File.Delete(temp);
            GetPreviewPath(duplicate);
            return duplicate;
        }
        var key = hash[..20].ToLowerInvariant();
        var destination = Path.Combine(store.LibraryPath, key + ".ico");
        File.Move(temp, destination, true);
        var preview = Path.Combine(store.PreviewPath, $"{key}-{PreviewVersion}.png");
        SavePreview(bitmap, preview);
        var executable = Path.GetExtension(path).Equals(".exe", StringComparison.OrdinalIgnoreCase);
        return store.AddIcon(new IconRecord
        {
            Name = Path.GetFileNameWithoutExtension(path), Path = destination, OriginalPath = path,
            Source = executable ? "软件提取" : "本地导入", Category = executable ? "软件" : "本地导入", Hash = hash,
            PreviewPath = preview
        });
    }

    public string GetPreviewPath(IconRecord icon)
    {
        if (!string.IsNullOrWhiteSpace(icon.PreviewPath) && File.Exists(icon.PreviewPath)
            && Path.GetFileNameWithoutExtension(icon.PreviewPath).EndsWith("-" + PreviewVersion, StringComparison.OrdinalIgnoreCase))
            return icon.PreviewPath;
        var key = !string.IsNullOrWhiteSpace(icon.Hash) ? icon.Hash[..Math.Min(20, icon.Hash.Length)].ToLowerInvariant() : StableKey(icon.Path);
        var preview = Path.Combine(store.PreviewPath, $"{key}-{PreviewVersion}.png");
        if (!File.Exists(preview))
        {
            var source = !string.IsNullOrWhiteSpace(icon.OriginalPath) && File.Exists(icon.OriginalPath) ? icon.OriginalPath : icon.Path;
            try
            {
                using var bitmap = LoadSource(source, 512);
                SavePreview(bitmap, preview);
            }
            catch { return icon.Path; }
        }
        icon.PreviewPath = preview;
        return preview;
    }

    public string? GetReferencePreview(string? iconLocation, string? backupPath = null)
    {
        var source = File.Exists(backupPath) ? backupPath : SplitIconLocation(iconLocation);
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source)) return null;
        var key = $"history-{PreviewVersion}-" + StableKey((iconLocation ?? string.Empty) + "|" + source);
        var preview = Path.Combine(store.PreviewPath, key + ".png");
        if (File.Exists(preview)) return preview;
        try
        {
            using var bitmap = IsSupported(source) ? LoadSource(source, 512) : NativeMethods.ShellImage(source, 512);
            SavePreview(bitmap, preview);
            return preview;
        }
        catch { return null; }
    }

    public string RenderEditPreview(IconRecord icon, IconEditOptions options, string destination, int size = 512)
    {
        var sourcePath = !string.IsNullOrWhiteSpace(icon.OriginalPath) && File.Exists(icon.OriginalPath) ? icon.OriginalPath : icon.Path;
        using var source = LoadSource(sourcePath, Math.Max(512, size));
        using var rendered = RenderEdited(source, Math.Clamp(size, 128, 1024), options);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        rendered.Save(destination, ImageFormat.Png);
        return destination;
    }

    public IconRecord ApplyEdit(IconRecord icon, IconEditOptions options)
    {
        var sourcePath = !string.IsNullOrWhiteSpace(icon.OriginalPath) && File.Exists(icon.OriginalPath) ? icon.OriginalPath : icon.Path;
        using var source = LoadSource(sourcePath, 1024);
        using var edited = RenderEdited(source, 768, options);
        var temp = Path.Combine(store.LibraryPath, Guid.NewGuid().ToString("N") + ".tmp.ico");
        WriteMultiSizeIco(edited, temp, 0);
        var hash = DataStore.Sha256(temp);
        var key = hash[..20].ToLowerInvariant();
        var destination = Path.Combine(store.LibraryPath, key + ".ico");
        File.Move(temp, destination, true);
        var preview = Path.Combine(store.PreviewPath, $"{key}-{PreviewVersion}.png");
        SavePreview(edited, preview);
        icon.Path = destination;
        icon.Hash = hash;
        icon.PreviewPath = preview;
        icon.Source = "本地编辑";
        store.Save();
        return icon;
    }

    public void DeleteIcon(string iconId)
    {
        var icon = store.Data.Icons.FirstOrDefault(x => x.Id == iconId)
            ?? throw new InvalidOperationException("图标已不存在。");
        var libraryFile = icon.Path;
        var previewFile = icon.PreviewPath;
        store.RemoveIcon(iconId);
        if (!store.Data.Icons.Any(x => x.Path.Equals(libraryFile, StringComparison.OrdinalIgnoreCase))) TryDelete(libraryFile, store.LibraryPath);
        if (previewFile is not null && !store.Data.Icons.Any(x => string.Equals(x.PreviewPath, previewFile, StringComparison.OrdinalIgnoreCase))) TryDelete(previewFile, store.PreviewPath);
    }

    private static void TryDelete(string path, string root)
    {
        try
        {
            var full = Path.GetFullPath(path);
            var basePath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (full.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) File.Delete(full);
        }
        catch { }
    }

    private static void SavePreview(Image source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var preview = RenderSquare(source, 256, .02);
        preview.Save(destination, ImageFormat.Png);
    }

    private static string? SplitIconLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location)) return null;
        var comma = location.LastIndexOf(',');
        return comma > 0 && int.TryParse(location[(comma + 1)..], out _) ? location[..comma] : location;
    }

    private static string StableKey(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..20];

    private static Bitmap LoadSource(string path, int size)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".exe") return NativeMethods.ShellImage(path, Math.Min(size, 256), true);
        if (extension is ".svg" or ".webp") return NativeMethods.ShellImage(path, size);
        if (extension == ".ico") return LoadLargestIcoFrame(path, size);
        using var source = Image.FromFile(path);
        return new Bitmap(source);
    }

    private static Bitmap LoadLargestIcoFrame(string path, int requestedSize)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            if (reader.ReadUInt16() != 0 || reader.ReadUInt16() != 1) throw new InvalidDataException();
            var count = reader.ReadUInt16();
            var entries = new List<(int Size, int Length, int Offset)>();
            for (var i = 0; i < count; i++)
            {
                var width = reader.ReadByte();
                var height = reader.ReadByte();
                reader.ReadBytes(6);
                entries.Add((Math.Max(width == 0 ? 256 : width, height == 0 ? 256 : height), reader.ReadInt32(), reader.ReadInt32()));
            }
            foreach (var entry in entries.OrderByDescending(x => x.Size))
            {
                stream.Position = entry.Offset;
                var data = reader.ReadBytes(entry.Length);
                if (data.Length < 8 || !data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) continue;
                using var memory = new MemoryStream(data);
                using var frame = Image.FromStream(memory);
                return new Bitmap(frame);
            }
        }
        catch { }
        using var ico = new Icon(path, Math.Min(requestedSize, 256), Math.Min(requestedSize, 256));
        return ico.ToBitmap();
    }

    internal static void WriteMultiSizeIco(Image source, string destination, double marginFraction = .07)
    {
        var images = Sizes.Select(size => (Size: size, Data: EncodePng(RenderSquare(source, size, marginFraction)))).ToArray();
        using var stream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)images.Length);
        var offset = 6 + images.Length * 16;
        foreach (var image in images)
        {
            writer.Write((byte)(image.Size == 256 ? 0 : image.Size));
            writer.Write((byte)(image.Size == 256 ? 0 : image.Size));
            writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
            writer.Write(image.Data.Length); writer.Write(offset); offset += image.Data.Length;
        }
        foreach (var image in images) writer.Write(image.Data);
    }

    private static Bitmap RenderEdited(Image source, int size, IconEditOptions options)
    {
        using var transparentSource = options.RemoveBackgroundColor
            ? RemoveConnectedBackground(source, Color.FromArgb(options.BackgroundRemovalArgb), options.BackgroundTolerance)
            : null;
        var drawSource = (Image?)transparentSource ?? source;
        var result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        ConfigureGraphics(graphics);
        graphics.Clear(Color.Transparent);
        var padding = Math.Clamp(options.Padding, 0, .4);
        var inset = (float)(size * padding);
        var destination = new RectangleF(inset, inset, size - inset * 2, size - inset * 2);
        var background = Color.FromArgb(options.BackgroundArgb);
        if (!options.BottomShape.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            using var brush = new SolidBrush(background);
            if (options.BottomShape.Equals("circle", StringComparison.OrdinalIgnoreCase)) graphics.FillEllipse(brush, destination);
            else
            {
                var radius = options.BottomShape.Equals("squircle", StringComparison.OrdinalIgnoreCase) ? destination.Width * .24f : destination.Width * .12f;
                using var shape = RoundedRectangle(destination, radius);
                graphics.FillPath(brush, shape);
            }
        }

        var ratio = ParseAspect(options.AspectRatio, drawSource.Width / (double)drawSource.Height);
        var crop = CalculateCrop(drawSource.Width, drawSource.Height, ratio, Math.Clamp(options.Zoom, 1, 4),
            Math.Clamp(options.OffsetX, -1, 1), Math.Clamp(options.OffsetY, -1, 1));
        var radiusPx = (float)(destination.Width * Math.Clamp(options.CornerRadius, 0, .5));
        var state = graphics.Save();
        if (radiusPx > 0)
        {
            using var clip = RoundedRectangle(destination, radiusPx);
            graphics.SetClip(clip);
        }
        graphics.DrawImage(drawSource, destination, crop, GraphicsUnit.Pixel);
        graphics.Restore(state);
        return result;
    }

    public static Bitmap RemoveConnectedBackground(Image source, Color backgroundColor, int tolerance)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(result))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        }

        var rectangle = new Rectangle(0, 0, result.Width, result.Height);
        var data = result.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var pixels = new byte[stride * result.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            var visited = new byte[result.Width * result.Height];
            var queue = new int[visited.Length];
            var head = 0;
            var tail = 0;
            var threshold = Math.Clamp(tolerance, 0, 220);
            var softness = Math.Clamp(18 + threshold / 2, 18, 64);
            var limit = Math.Min(255, threshold + softness);
            var limitSquared = limit * limit;

            bool IsBackgroundCandidate(int x, int y)
            {
                var offset = y * stride + x * 4;
                var blue = pixels[offset];
                var green = pixels[offset + 1];
                var red = pixels[offset + 2];
                var distanceSquared = ((red - backgroundColor.R) * (red - backgroundColor.R)
                    + (green - backgroundColor.G) * (green - backgroundColor.G)
                    + (blue - backgroundColor.B) * (blue - backgroundColor.B)) / 3;
                return distanceSquared <= limitSquared;
            }

            void Enqueue(int x, int y)
            {
                if ((uint)x >= (uint)result.Width || (uint)y >= (uint)result.Height) return;
                var index = y * result.Width + x;
                if (visited[index] != 0 || !IsBackgroundCandidate(x, y)) return;
                visited[index] = 1;
                queue[tail++] = index;
            }

            for (var x = 0; x < result.Width; x++) { Enqueue(x, 0); Enqueue(x, result.Height - 1); }
            for (var y = 1; y < result.Height - 1; y++) { Enqueue(0, y); Enqueue(result.Width - 1, y); }
            while (head < tail)
            {
                var index = queue[head++];
                var x = index % result.Width;
                var y = index / result.Width;
                Enqueue(x - 1, y); Enqueue(x + 1, y); Enqueue(x, y - 1); Enqueue(x, y + 1);
            }

            for (var index = 0; index < visited.Length; index++)
            {
                if (visited[index] == 0) continue;
                var x = index % result.Width;
                var y = index / result.Width;
                var offset = y * stride + x * 4;
                var blue = pixels[offset];
                var green = pixels[offset + 1];
                var red = pixels[offset + 2];
                var distance = Math.Sqrt(((red - backgroundColor.R) * (red - backgroundColor.R)
                    + (green - backgroundColor.G) * (green - backgroundColor.G)
                    + (blue - backgroundColor.B) * (blue - backgroundColor.B)) / 3d);
                var opacity = distance <= threshold ? 0 : Math.Clamp((distance - threshold) / softness, 0, 1);
                pixels[offset + 3] = (byte)Math.Round(pixels[offset + 3] * opacity);
            }
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
        }
        finally { result.UnlockBits(data); }
        return result;
    }

    private static double ParseAspect(string value, double original)
        => value switch { "1:1" => 1, "4:3" => 4d / 3, "3:4" => 3d / 4, "16:9" => 16d / 9, "9:16" => 9d / 16, _ => original };

    private static RectangleF CalculateCrop(int width, int height, double ratio, double zoom, double offsetX, double offsetY)
    {
        double cropWidth = width, cropHeight = cropWidth / ratio;
        if (cropHeight > height) { cropHeight = height; cropWidth = cropHeight * ratio; }
        cropWidth /= zoom;
        cropHeight /= zoom;
        var x = (width - cropWidth) / 2 + offsetX * (width - cropWidth) / 2;
        var y = (height - cropHeight) / 2 + offsetY * (height - cropHeight) / 2;
        return new RectangleF((float)x, (float)y, (float)cropWidth, (float)cropHeight);
    }

    private static GraphicsPath RoundedRectangle(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        radius = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        if (radius <= .1f) { path.AddRectangle(rect); return path; }
        var d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Bitmap RenderSquare(Image source, int size, double marginFraction)
    {
        var result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        graphics.Clear(Color.Transparent);
        ConfigureGraphics(graphics);
        var margin = Math.Max(0, (int)Math.Round(size * marginFraction));
        var available = Math.Max(1, size - margin * 2);
        var scale = Math.Min((double)available / source.Width, (double)available / source.Height);
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        graphics.DrawImage(source, new Rectangle((size - width) / 2, (size - height) / 2, width, height));
        return result;
    }

    private static void ConfigureGraphics(Graphics graphics)
    {
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }

    private static byte[] EncodePng(Bitmap bitmap)
    {
        using (bitmap)
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }
}
