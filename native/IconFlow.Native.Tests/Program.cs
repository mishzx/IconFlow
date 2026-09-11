using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using IconFlow.Core;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 3 && args[0] == "--prepare-builtin")
        {
            ImageService.PrepareSpriteSheet(args[1], args[2], 3, 3,
                ["literature", "data", "code", "laboratory", "pictures", "clinical", "todo", "completed", "archive"]);
            Console.WriteLine("PASS: prepared nine built-in Fluent folder icons");
            return 0;
        }
        if (args.Length == 3 && args[0] == "--seed")
        {
            var store = new DataStore(args[1]);
            var icon = new ImageService(store).Import(args[2]);
            Console.WriteLine(icon.Path);
            return 0;
        }
        if (args.Length == 2 && args[0] == "--integration-roundtrip")
        {
            var services = new AppServices(args[1], false);
            try
            {
                services.Integration.SetContextMenu(true);
                services.Integration.SetLaunchAtLogin(true);
                Assert(services.Integration.IsContextMenuRegistered(), "Context menu registration failed.");
                Assert(services.Integration.IsLaunchAtLoginRegistered(), "Startup registration failed.");
            }
            finally { services.Integration.RemoveAllIntegrations(); }
            Assert(!services.Integration.IsContextMenuRegistered(), "Context menu cleanup failed.");
            Assert(!services.Integration.IsLaunchAtLoginRegistered(), "Startup cleanup failed.");
            Console.WriteLine("PASS: context menu and startup registration roundtrip, no residue");
            return 0;
        }
        if (args.Length == 4 && args[0] == "--seed-many" && int.TryParse(args[3], out var count))
        {
            var store = new DataStore(args[1]);
            var source = new ImageService(store).Import(args[2]);
            store.Data.Icons.Clear();
            for (var i = 0; i < count; i++)
            {
                store.Data.Icons.Add(new IconRecord
                {
                    Name = $"测试图标 {i + 1:00000}", Path = source.Path, OriginalPath = args[2],
                    Hash = source.Hash + i.ToString("X8"), PreviewPath = source.PreviewPath,
                    Tags = [i % 2 == 0 ? "蓝色" : "科研"]
                });
            }
            store.Save();
            Console.WriteLine($"PASS: seeded {count} icon records");
            return 0;
        }
        var root = Path.Combine(Path.GetTempPath(), "IconFlowCoreTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            TestStore(root);
            var (store, icon) = TestIcoImport(root);
            TestDuplicateImport(store, icon);
            TestRename(store, icon);
            TestFolders(store, icon);
            TestBackgroundRemoval(root);
            TestBuiltInPack(root);
            TestEdit(store, icon, root);
            TestFolderApplyUndo(store, icon, root);
            TestShortcutApplyUndo(store, icon, root);
            TestHistoryPersistence(store);
            TestMissingHistoryCleanup(store, root);
            TestStorageMigration(store, icon, root);
            TestDelete(store, icon);
            Console.WriteLine("PASS: atomic store, sharp v2 preview, 7-size ICO, duplicate detection, folders, connected background removal, built-in pack, edit pipeline, resilient undo, grouped history, storage migration and safe delete");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                Directory.Delete(root, true);
            }
            catch { }
        }
    }

    private static void TestStore(string root)
    {
        var path = Path.Combine(root, "store-test");
        var store = new DataStore(path);
        store.Data.Settings.Theme = "light";
        store.Save();
        var reloaded = new DataStore(path);
        Assert(reloaded.Data.Settings.Theme == "light", "Atomic store reload failed.");
    }

    private static (DataStore Store, IconRecord Icon) TestIcoImport(string root)
    {
        var store = new DataStore(Path.Combine(root, "app-data"));
        var png = Path.Combine(root, "source.png");
        using (var bitmap = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(Color.FromArgb(255, 92, 112, 238)))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.FillRectangle(brush, 8, 8, 240, 240);
            graphics.FillEllipse(Brushes.White, 76, 76, 104, 104);
            bitmap.Save(png, ImageFormat.Png);
        }
        var service = new ImageService(store);
        var icon = service.Import(png);
        Assert(File.Exists(icon.Path), "ICO was not created.");
        Assert(File.Exists(icon.PreviewPath), "Preview PNG was not created.");
        using (var preview = Image.FromFile(icon.PreviewPath!))
            Assert(preview.Width == 256 && preview.Height == 256, "Preview PNG is not 256 by 256 pixels.");
        using var reader = new BinaryReader(File.OpenRead(icon.Path));
        Assert(reader.ReadUInt16() == 0 && reader.ReadUInt16() == 1 && reader.ReadUInt16() == 7, "ICO does not contain seven directory entries.");
        return (store, icon);
    }

    private static void TestRename(DataStore store, IconRecord icon)
    {
        store.RenameIcon(icon.Id, "清晰图标");
        Assert(icon.Name == "清晰图标", "Icon rename failed.");
    }

    private static void TestDuplicateImport(DataStore store, IconRecord expected)
    {
        var duplicate = new ImageService(store).Import(expected.OriginalPath!);
        Assert(duplicate.Id == expected.Id && store.Data.Icons.Count == 1, "Duplicate detection failed.");
    }

    private static void TestFolders(DataStore store, IconRecord icon)
    {
        var folder = store.AddIconFolder("科研图标");
        store.MoveIconToFolder(icon.Id, folder.Id);
        store.RenameIconFolder(folder.Id, "科研与数据");
        Assert(icon.FolderId == folder.Id && store.Data.IconFolders.Single().Name == "科研与数据", "Icon folder create, move or rename failed.");
    }

    private static void TestBackgroundRemoval(string root)
    {
        using var source = new Bitmap(128, 128, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(source))
        {
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.RoyalBlue, 24, 24, 80, 80);
            graphics.FillEllipse(Brushes.White, 50, 50, 28, 28);
        }
        using var cleaned = ImageService.RemoveConnectedBackground(source, Color.White, 24);
        Assert(cleaned.GetPixel(4, 4).A == 0, "Connected white background was not removed.");
        Assert(cleaned.GetPixel(64, 64).A == 255, "Disconnected white foreground motif was removed.");
    }

    private static void TestBuiltInPack(string root)
    {
        var sheetPath = Path.Combine(root, "built-in-sheet.png");
        using (var sheet = new Bitmap(300, 300, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(sheet))
        {
            graphics.Clear(Color.White);
            for (var row = 0; row < 3; row++)
            for (var column = 0; column < 3; column++)
            {
                using var brush = new SolidBrush(Color.FromArgb(255, 50 + column * 55, 70 + row * 50, 150));
                graphics.FillRectangle(brush, column * 100 + 12, row * 100 + 16, 76, 68);
            }
            sheet.Save(sheetPath, ImageFormat.Png);
        }
        var assets = Path.Combine(root, "built-in-assets");
        ImageService.PrepareSpriteSheet(sheetPath, assets, 3, 3,
            ["literature", "data", "code", "laboratory", "pictures", "clinical", "todo", "completed", "archive"]);
        using (var preview = new Bitmap(Path.Combine(assets, "literature.png")))
            Assert(preview.GetPixel(0, 0).A == 0, "Built-in preview does not have a transparent corner.");
        var store = new DataStore(Path.Combine(root, "built-in-store"));
        var images = new ImageService(store);
        images.EnsureBuiltInIcons(assets);
        Assert(store.Data.Icons.Count == 9 && store.Data.IconFolders.Any(x => x.Name == "内置图标"), "Built-in icon pack was not seeded.");
        images.EnsureBuiltInIcons(assets);
        Assert(store.Data.Icons.Count == 9, "Built-in icon pack seeded duplicates.");
    }

    private static void TestEdit(DataStore store, IconRecord icon, string root)
    {
        var service = new ImageService(store);
        var preview = Path.Combine(root, "edit-preview.png");
        var options = new IconEditOptions { AspectRatio = "1:1", Zoom = 1.2, OffsetX = .2, Padding = .08, CornerRadius = .18, BottomShape = "squircle" };
        service.RenderEditPreview(icon, options, preview);
        Assert(File.Exists(preview), "Edited preview was not created.");
        service.ApplyEdit(icon, options);
        using var reader = new BinaryReader(File.OpenRead(icon.Path));
        Assert(reader.ReadUInt16() == 0 && reader.ReadUInt16() == 1 && reader.ReadUInt16() == 7, "Edited ICO does not contain seven sizes.");
    }

    private static void TestFolderApplyUndo(DataStore store, IconRecord icon, string root)
    {
        var folder = Path.Combine(root, "测试 文件夹 & symbols");
        Directory.CreateDirectory(folder);
        var service = new WindowsIconService(store);
        var first = service.Apply(folder, icon);
        var ini = Path.Combine(folder, "desktop.ini");
        Assert(File.Exists(ini), "Folder desktop.ini was not created.");
        Assert(Encoding.Unicode.GetString(File.ReadAllBytes(ini)).Contains("IconResource="), "Folder icon entry missing.");
        var second = service.Apply(folder, icon);
        Assert(File.Exists(second.Before.IconBackupPath), "Previous folder icon was not backed up.");
        var originalManaged = second.Before.IconLocation?.Split(',')[0];
        if (File.Exists(originalManaged)) File.Delete(originalManaged);
        service.Undo(second.Id);
        var restored = Encoding.Unicode.GetString(File.ReadAllBytes(ini));
        Assert(restored.Contains(second.Before.IconBackupPath!, StringComparison.OrdinalIgnoreCase), "Folder undo did not fall back to its backup icon.");
        service.Undo(first.Id);
        Assert(!File.Exists(ini), "Folder undo did not restore the original state.");
    }

    private static void TestShortcutApplyUndo(DataStore store, IconRecord icon, string root)
    {
        var shortcut = Path.Combine(root, "测试 shortcut.lnk");
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe"));
            ((IPersistFile)link).Save(shortcut, true);
        }
        finally { Marshal.FinalReleaseComObject(link); }
        var service = new WindowsIconService(store);
        var record = service.Apply(shortcut, icon);
        Assert(service.Inspect(shortcut).CurrentIcon?.Contains("managed-icons", StringComparison.OrdinalIgnoreCase) == true, "Shortcut icon was not updated.");
        service.Undo(record.Id);
        Assert(string.IsNullOrWhiteSpace(service.Inspect(shortcut).CurrentIcon), "Shortcut undo did not restore the default icon.");
    }

    private static void TestHistoryPersistence(DataStore store)
    {
        var reloaded = new DataStore(store.RootPath);
        Assert(reloaded.Data.History.Count >= 2 && reloaded.Data.History.All(x => x.Undone), "History persistence failed.");
    }

    private static void TestMissingHistoryCleanup(DataStore store, string root)
    {
        var managed = Path.Combine(store.ManagedPath, "orphan.ico");
        File.WriteAllBytes(managed, [0, 0, 1, 0]);
        var ghost = new HistoryRecord
        {
            TargetPath = Path.Combine(root, "deleted-target"), TargetName = "deleted-target", TargetType = "folder",
            After = new TargetState { IconPath = managed }
        };
        store.AddHistory(ghost);
        var removed = store.PruneMissingHistory();
        Assert(removed == 1 && store.Data.History.All(x => x.Id != ghost.Id), "Missing target history was not removed.");
        Assert(!File.Exists(managed), "Orphaned managed icon was not cleaned up.");
    }

    private static void TestStorageMigration(DataStore store, IconRecord icon, string root)
    {
        var beforeHash = DataStore.Sha256(icon.Path);
        var destination = Path.Combine(root, "migrated-library");
        store.MigrateIconStorage(destination);
        Assert(icon.Path.StartsWith(Path.Combine(destination, "icon-library"), StringComparison.OrdinalIgnoreCase), "Icon path was not migrated.");
        Assert(DataStore.Sha256(icon.Path) == beforeHash && File.Exists(icon.PreviewPath), "Migrated icon or preview did not pass verification.");
        var reloaded = new DataStore(store.RootPath);
        Assert(reloaded.LibraryPath.StartsWith(destination, StringComparison.OrdinalIgnoreCase), "Migrated storage setting did not persist.");
    }

    private static void TestDelete(DataStore store, IconRecord icon)
    {
        Assert(store.GetActiveIconUsageCount(icon.Id) == 0, "Undone history was counted as active usage.");
        new ImageService(store).DeleteIcon(icon.Id);
        Assert(store.Data.Icons.All(x => x.Id != icon.Id), "Icon delete failed.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
