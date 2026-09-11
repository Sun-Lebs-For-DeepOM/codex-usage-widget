using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodexUsageWidget;
using CodexUsageWidget.Models;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var app = new App();
        app.InitializeComponent();
        var previewDirectory = Path.Combine(AppContext.BaseDirectory, "previews");
        Directory.CreateDirectory(previewDirectory);
        Directory.SetCurrentDirectory(previewDirectory);
        if (args.Contains("--native"))
        {
            var nativeWindow = new MainWindow();
            new System.Windows.Interop.WindowInteropHelper(nativeWindow).EnsureHandle();
            Console.WriteLine($"NativeBackdropActive={nativeWindow.NativeBackdropActive}; Layered={nativeWindow.AllowsTransparency}");
            if (nativeWindow.NativeBackdropActive || !nativeWindow.AllowsTransparency) throw new Exception("Independent alpha needs a layered window");
            typeof(MainWindow).GetMethod("ShowUsageDetails", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(nativeWindow, null);
            var details = app.Windows.OfType<Window>().Single(w => w.Title == "Codex · 详细统计");
            if (!details.IsVisible) throw new Exception("Details window did not open");
            details.Close();
            Console.WriteLine("PASS details window opens and closes");
            nativeWindow.Close();
            return;
        }
        var window = new MainWindow();
        var normalizationTime = DateTimeOffset.Now;
        var weeklyOnly = new CodexQuotaSnapshot("codex", null, null, new RateLimitWindowSnapshot(15, 10080, normalizationTime.AddDays(5)), null, null, null, 0, "test", normalizationTime);
        if (weeklyOnly.NormalizeWindowOrder().Primary is not null || weeklyOnly.NormalizeWindowOrder().Secondary?.WindowDurationMinutes != 10080)
            throw new Exception("Weekly-only primary slot must map to weekly display");
        var reversed = weeklyOnly with { Secondary = new RateLimitWindowSnapshot(1, 300, normalizationTime.AddHours(3)) };
        if (reversed.NormalizeWindowOrder().Primary?.WindowDurationMinutes != 300 || reversed.NormalizeWindowOrder().Secondary?.WindowDurationMinutes != 10080)
            throw new Exception("Reversed API slots not normalized");
        var cycleNow = DateTimeOffset.Now;
        if (MainWindow.ResetCycleRemainingPercent(cycleNow, cycleNow.AddHours(3), 300) != 60 ||
            MainWindow.ResetCycleRemainingPercent(cycleNow, cycleNow.AddHours(5), 300) != 100 ||
            MainWindow.ResetCycleRemainingPercent(cycleNow, cycleNow.AddMinutes(-1), 300) != 0 ||
            MainWindow.ResetCycleRemainingPercent(cycleNow, null, 300) is not null ||
            MainWindow.ResetCycleRemainingPercent(cycleNow, cycleNow, 0) is not null)
            throw new Exception("Reset-cycle boundary calculation failed");
        typeof(MainWindow).GetField("_snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window,
            new CodexQuotaSnapshot("codex", null, "test", new RateLimitWindowSnapshot(1, 300, cycleNow.AddHours(3)),
                new RateLimitWindowSnapshot(15, 10080, cycleNow.AddDays(5)), null, null, 0, "test", cycleNow, null));
        window.ApplyAppearance(300, 240, 1, "#8B5CF6", "#221933");
        window.UpdateResponsiveLayout();
        new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
        var appearance = new AppearanceWindow(window);
        ((Slider)appearance.FindName("OpacitySlider")).Value = 25;
        ((Slider)appearance.FindName("TextOpacitySlider")).Value = 35;
        var background = (SolidColorBrush)((Border)window.FindName("WindowChrome")).Background;
        if (window.BackgroundOpacity != 0.25 || window.TextOpacity != 0.35 || window.Opacity != 1 || background.Color.A > 64)
            throw new Exception("Slider does not change independent alpha");
        if (((TextBlock)window.FindName("EstimatedCostText")).Opacity != 0.35)
            throw new Exception("Text opacity not applied");
        ((Slider)appearance.FindName("OpacitySlider")).Value = 0;
        if (((SolidColorBrush)((Border)window.FindName("WindowChrome")).Background).Color.A != 0 || window.TextOpacity != 0.35)
            throw new Exception("Zero background opacity is not independent");
        var settingsFile = Path.Combine(Path.GetTempPath(), $"widget-alpha-{Guid.NewGuid():N}.json");
        try
        {
            CodexUsageWidget.Services.WidgetSettingsStore.Save(new CodexUsageWidget.Services.WidgetSettings { Opacity = 0.25, TextOpacity = 0.35, ShowPrimaryQuota = false }, settingsFile);
            var saved = CodexUsageWidget.Services.WidgetSettingsStore.Load(settingsFile);
            if (saved.Opacity != 0.25 || saved.TextOpacity != 0.35) throw new Exception("Alpha settings did not round trip");
            if (saved.ShowPrimaryQuota) throw new Exception("Primary quota preference not persisted");
        }
        finally { File.Delete(settingsFile); }
        ((CheckBox)appearance.FindName("ShowPrimaryQuotaCheckBox")).IsChecked = false;
        if (window.ShowPrimaryQuota) throw new Exception("Primary toggle callback failed");
        ((CheckBox)appearance.FindName("ShowPrimaryQuotaCheckBox")).IsChecked = true;
        if (!window.ShowPrimaryQuota) throw new Exception("Primary toggle restore failed");
        var snapshotField = typeof(MainWindow).GetField("_snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var applySnapshot = typeof(MainWindow).GetMethod("ApplySnapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        applySnapshot.Invoke(window, new[] { snapshotField.GetValue(window) });
        if (((TextBlock)window.FindName("BadgeText")).Text != "重置卡 0") throw new Exception("Zero reset cards must remain visible");
        appearance.Close(); window.ApplyTextOpacity(1);
        Console.WriteLine("PASS real slider callbacks, background 0/25%, text 35%, independent alpha and settings persistence");
        foreach (var (width, height, light) in new[] { (460, 570, true), (720, 240, true), (300, 240, false), (720, 470, true), (320, 900, true), (400, 740, true), (720, 470, false), (300, 240, true), (240, 192, true), (150, 120, true), (150, 300, false), (500, 120, false), (300, 240, true) })
        {
            window.ApplyAppearance(width, height, 1, "#9B75FF", light ? "#DDE3ED" : "#221933");
            window.ApplyDisplayOptions(height == 470 || height == 740 ? WidgetViewStyle.Card : WidgetViewStyle.Ring, true, TokenUsageScope.Today, TokenUsagePeriod.All, null, null, false);
            ((TextBlock)window.FindName("EstimatedCostText")).Text = "≈ US$15,212.53*";
            ((TextBlock)window.FindName("ServiceTierEstimatedCostText")).Text = "Fast ≈ US$30,425.06*";
            ((TextBlock)window.FindName("TokenTotalText")).Text = "17.3B";
            ((TextBlock)window.FindName("PrimaryResetText")).Text = "重置：2小时 35分";
            ((TextBlock)window.FindName("SecondaryResetText")).Text = "重置：5天 12小时";
            ((System.Windows.Documents.Run)window.FindName("PrimaryPercentRun")).Text = "99";
            ((System.Windows.Documents.Run)window.FindName("SecondaryPercentRun")).Text = "85";
            ((CodexUsageWidget.Controls.CircularProgressRing)window.FindName("PrimaryRing")).Value = 99;
            ((CodexUsageWidget.Controls.CircularProgressRing)window.FindName("SecondaryRing")).Value = 85;
            ((CodexUsageWidget.Controls.CircularProgressRing)window.FindName("PrimaryRing")).ProgressBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246));
            ((CodexUsageWidget.Controls.CircularProgressRing)window.FindName("SecondaryRing")).ProgressBrush = new SolidColorBrush(Color.FromRgb(69, 190, 208));
            ((System.Windows.Documents.Run)window.FindName("PrimaryRingPercentRun")).Text = "99";
            ((System.Windows.Documents.Run)window.FindName("SecondaryRingPercentRun")).Text = "85";
            window.UpdateResponsiveLayout();
            if (args.Contains("--readme"))
            {
                foreach (var (name, value) in new[] {
                    ("RegularInputTokenText", "888.4M"), ("CachedInputTokenText", "16.31B"),
                    ("VisibleOutputTokenText", "68.8M"), ("ReasoningOutputTokenText", "32.8M"),
                    ("StatusText", "合成数据 · 功能示意"), ("PlanText", "DEMO"),
                    ("PrimaryResetText", "重置：2小时 59分"), ("SecondaryResetText", "重置：4天 23小时"),
                    ("TokenEstimateNoteText", "示例金额用于展示界面，不代表实际账单") })
                    ((TextBlock)window.FindName(name)).Text = value;
                var distribution = ((Grid)window.FindName("TokenDistributionGrid")).ColumnDefinitions;
                var counts = new[] { 888.4, 16310, 68.8, 32.8 };
                for (var i = 0; i < counts.Length; i++) distribution[i].Width = new GridLength(counts[i], GridUnitType.Star);
            }
            var root = (FrameworkElement)window.Content;
            root.Measure(new Size(width, height));
            root.Arrange(new Rect(0, 0, width, height));
            root.UpdateLayout();
            var chrome = (FrameworkElement)window.FindName("WindowChrome");
            var grip = Descendants(root).OfType<Thumb>().Single(t => Equals(t.Tag, "BottomRight"));
            var chromeCorner = chrome.TranslatePoint(new Point(chrome.ActualWidth, chrome.ActualHeight), root);
            var gripCorner = grip.TranslatePoint(new Point(grip.ActualWidth, grip.ActualHeight), root);
            if ((chromeCorner - gripCorner).Length > 1) throw new Exception("Resize corner mismatch");
            if (Descendants(root).OfType<Viewbox>().Any()) throw new Exception("Unexpected text scaling");
            if (Descendants(root).OfType<TextBlock>().Any(t => t.FontSize < 11)) throw new Exception("Unreadable font floor");
            var compactSummary = Descendants(root).OfType<TextBlock>().Where(t => (t.Name == "CompactTokenTotal" || t.Name == "CompactMainAmount") && VisibleInTree(t, root)).ToArray();
            foreach (var text in compactSummary)
            {
                if (text.Text != (text.Name == "CompactTokenTotal" ? "17.3B" : "≈ US$15,212.53*")) throw new Exception("Compact summary data missing");
                var origin = text.TranslatePoint(new Point(0, 0), root);
                var end = text.TranslatePoint(new Point(text.ActualWidth, text.ActualHeight), root);
                if (origin.X < 0 || origin.Y < 0 || end.X > width || end.Y > height) throw new Exception("Compact summary outside window");
                if (text.ActualHeight > 35) throw new Exception("Compact amount wraps unexpectedly");
            }
            if (window.ViewStyle == WidgetViewStyle.Ring)
            {
                var rings = Descendants(root).OfType<CodexUsageWidget.Controls.CircularProgressRing>().Where(r => VisibleInTree(r, root)).ToArray();
                if (rings.Length != 4) throw new Exception("Both quota circles must have a time ring");
                foreach (var timeRing in rings.Where(r => r.Name == "ResetCycleTimeRing"))
                {
                    var container = VisualTreeHelper.GetParent(timeRing);
                    var quota = Descendants(container).OfType<CodexUsageWidget.Controls.CircularProgressRing>().Single(r => r.Name != "ResetCycleTimeRing");
                    if (timeRing.ActualWidth >= quota.ActualWidth || timeRing.Value <= 0 || timeRing.Value >= 100) throw new Exception("Time ring must be inside quota ring with real progress");
                }
            }
            var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            image.Render(root);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using var stream = File.Create($"preview-{width}x{height}.png");
            encoder.Save(stream);
            if (args.Contains("--readme"))
                SavePreview(root, width, height, $"readme-{width}x{height}-{(light ? "light" : "dark")}.png");
            if (window.Opacity != 1) throw new Exception("Text opacity changed");
            if (!Descendants(root).OfType<TextBlock>().Any(t => t.Text == "重置卡 0" && VisibleInTree(t, root))) throw new Exception("Reset-card count disappeared");
            if (window.ViewStyle == WidgetViewStyle.Ring)
            {
                window.SetPrimaryQuotaVisible(false);
                root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
                if (Descendants(root).OfType<CodexUsageWidget.Controls.CircularProgressRing>().Count(r => VisibleInTree(r, root)) != 2) throw new Exception("Primary hidden must leave one dual-ring component");
                var visibleLabels = Descendants(root).OfType<TextBlock>().Where(t => VisibleInTree(t, root)).Select(t => t.Text).ToArray();
                if (!visibleLabels.Contains("7 天") || visibleLabels.Contains("5 小时")) throw new Exception("Hidden five-hour option must retain the seven-day label");
                if (!Descendants(root).OfType<TextBlock>().Any(t => t.Text == "重置卡 0" && VisibleInTree(t, root))) throw new Exception("Reset-card count disappeared in single quota mode");
                if (width == 300 && height == 240)
                {
                    if (args.Contains("--readme") && light) SavePreview(root, width, height, "readme-single-quota.png");
                    var singleImage = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); singleImage.Render(root);
                    var singleEncoder = new PngBitmapEncoder(); singleEncoder.Frames.Add(BitmapFrame.Create(singleImage));
                    using var singleStream = File.Create("preview-single-quota.png"); singleEncoder.Save(singleStream);
                }
                window.SetPrimaryQuotaVisible(true);
                root.Measure(new Size(width, height)); root.Arrange(new Rect(0, 0, width, height)); root.UpdateLayout();
                if (Descendants(root).OfType<CodexUsageWidget.Controls.CircularProgressRing>().Count(r => VisibleInTree(r, root)) != 4) throw new Exception("Primary restoration failed");
            }
            if ((width < 300 || height < 240) != (window.SmallWindowScale < 1)) throw new Exception("Small-window threshold regression");
            Console.WriteLine($"PASS {width}x{height}: corner alignment, bounded fonts >=11, opaque text, rendered");
        }
        if (args.Contains("--readme"))
        {
            var settingsPreview = new AppearanceWindow(window);
            if (settingsPreview.Content is Panel settingsPanel) settingsPanel.Background = settingsPreview.Background;
            SavePreview((FrameworkElement)settingsPreview.Content, 430, 660, "readme-settings.png", settingsPreview.Background);
            settingsPreview.Close();
        }
    }

    static void SavePreview(FrameworkElement root, int width, int height, string filename, Brush? background = null)
    {
        root.Measure(new Size(width, height));
        root.Arrange(new Rect(0, 0, width, height));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width * 2, height * 2, 192, 192, PixelFormats.Pbgra32);
        if (background is not null)
        {
            var backdrop = new DrawingVisual();
            using (var drawing = backdrop.RenderOpen()) drawing.DrawRectangle(background, null, new Rect(0, 0, width, height));
            bitmap.Render(backdrop);
        }
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(filename);
        encoder.Save(output);
    }

    static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    static bool VisibleInTree(DependencyObject item, DependencyObject root)
    {
        for (DependencyObject? current = item; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement ui && ui.Visibility != Visibility.Visible) return false;
            if (current == root) return true;
        }
        return false;
    }
}
