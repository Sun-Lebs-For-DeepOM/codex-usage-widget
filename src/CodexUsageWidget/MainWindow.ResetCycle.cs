using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodexUsageWidget.Controls;

namespace CodexUsageWidget;

public partial class MainWindow
{
    private readonly CircularProgressRing?[] _resetCycleRings = new CircularProgressRing?[2];
    private readonly CircularProgressRing?[] _compactResetCycleRings = new CircularProgressRing?[2];
    private readonly TextBlock[] _resetCycleCaptions = [new() { Text = "周期待同步" }, new() { Text = "周期待同步" }];
    private readonly TextBlock[] _shortCycleCaptions = [new() { Text = "待同步" }, new() { Text = "待同步" }];

    private void InitializeResetCycle()
    {
        var quotaRings = new[] { PrimaryRing, SecondaryRing };
        var captions = new[] { PrimaryRingUsedText, SecondaryRingUsedText };
        for (var index = 0; index < 2; index++)
        {
            var ring = CreateResetCycleRing();
            _resetCycleRings[index] = ring;
            ((Grid)quotaRings[index].Parent).Children.Insert(0, ring);
            ((Grid)captions[index].Parent).RowDefinitions[1].Height = new GridLength(32);
            captions[index].TextWrapping = TextWrapping.Wrap;
            captions[index].TextAlignment = TextAlignment.Center;
        }
    }

    private CircularProgressRing CreateResetCycleRing() => new()
    {
        Name = "ResetCycleTimeRing",
        StrokeThickness = 3,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TrackBrush = new SolidColorBrush(Color.FromArgb(30, 69, 190, 208)),
        ProgressBrush = new SolidColorBrush(Color.FromRgb(69, 190, 208)),
        ToolTip = "外圈：剩余额度；内圈：周期剩余时间",
        Visibility = Visibility.Collapsed
    };

    public static double? ResetCycleRemainingPercent(DateTimeOffset now, DateTimeOffset? reset, long? durationMinutes)
    {
        if (reset is null || durationMinutes is null or <= 0) return null;
        return Math.Clamp((reset.Value - now).TotalMinutes / durationMinutes.Value * 100, 0, 100);
    }

    private static string CycleDuration(double minutes)
    {
        minutes = Math.Max(0, minutes);
        if (minutes >= 1440) return $"{Math.Floor(minutes / 1440)}天{Math.Floor(minutes % 1440 / 60)}时";
        if (minutes >= 60) return $"{Math.Floor(minutes / 60)}时{Math.Floor(minutes % 60)}分";
        return $"{Math.Ceiling(minutes)}分";
    }

    private void UpdateResetCycle()
    {
        var now = DateTimeOffset.Now;
        var windows = new[] { _snapshot?.Primary, _snapshot?.Secondary };
        var visibleCaptions = new[] { PrimaryRingUsedText, SecondaryRingUsedText };
        var quotaRings = new[] { PrimaryRing, SecondaryRing };
        for (var index = 0; index < 2; index++)
        {
            var window = windows[index];
            var percent = ResetCycleRemainingPercent(now, window?.ResetsAt, window?.WindowDurationMinutes);
            var caption = "周期待同步";
            var shortCaption = "待同步";
            var description = "缺少周期长度或重置时间，暂不推算时间进度；外圈仍表示剩余额度。";
            if (percent.HasValue)
            {
                var total = (double)window!.WindowDurationMinutes!.Value;
                var remaining = Math.Clamp((window.ResetsAt!.Value - now).TotalMinutes, 0, total);
                var elapsed = total - remaining;
                caption = remaining <= 0 ? "周期结束 · 待刷新" : $"已过{CycleDuration(elapsed)}\n余{CycleDuration(remaining)}";
                shortCaption = remaining <= 0 ? "待刷新" : $"余{CycleDuration(remaining)}";
                description = $"外圈：剩余额度；内圈：周期剩余 {percent:0.0}%\n周期长度：{CycleDuration(total)}\n已过：{CycleDuration(elapsed)}\n剩余：{CycleDuration(remaining)}\n重置时间：{window.ResetsAt.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}\n彩色表示剩余时间，灰色表示已过时间。";
            }
            _resetCycleCaptions[index].Text = caption;
            _shortCycleCaptions[index].Text = shortCaption;
            _resetCycleCaptions[index].ToolTip = _shortCycleCaptions[index].ToolTip = description;
            visibleCaptions[index].Text = caption;
            visibleCaptions[index].ToolTip = description;
            ((Grid)quotaRings[index].Parent).ToolTip = description;
            foreach (var ring in new[] { _resetCycleRings[index], _compactResetCycleRings[index] })
            {
                if (ring is null) continue;
                ring.Value = percent ?? 0;
                ring.Visibility = percent.HasValue ? Visibility.Visible : Visibility.Collapsed;
                ring.ToolTip = description;
            }
        }
    }
}
