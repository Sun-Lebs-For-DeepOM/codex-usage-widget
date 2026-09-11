using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CodexUsageWidget;

public partial class MainWindow
{
    public bool ShowPrimaryQuota { get; private set; } = true;
    private StackPanel? _fullLegend;
    private StackPanel? _compactLegend;
    private Grid? _compactQuotaGrid;
    private FrameworkElement? _compactPrimaryHolder;

    public void SetPrimaryQuotaVisible(bool visible)
    {
        ShowPrimaryQuota = visible;
        ShowPrimaryMenuItem.IsChecked = visible;
        UpdateResponsiveLayout();
        _appearanceWindow?.SynchronizePrimaryVisibility();
        ScheduleSettingsSave();
    }

    private void ShowPrimaryMenuItem_Click(object sender, RoutedEventArgs e) =>
        SetPrimaryQuotaVisible(ShowPrimaryMenuItem.IsChecked);

    private StackPanel CreateRingLegend()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "额度剩余：外圈，低额度时变为红色。时间剩余：青色内圈。" };
        foreach (var (label, resource) in new[] { ("额度剩余", "QuotaLegendBrush"), ("时间剩余", "TimeLegendBrush") })
        {
            var dot = new Border { Width = 6, Height = 6, CornerRadius = new CornerRadius(3), VerticalAlignment = VerticalAlignment.Center };
            dot.SetResourceReference(Border.BackgroundProperty, resource);
            panel.Children.Add(dot);
            var text = new TextBlock { Text = label, FontSize = 11, Margin = new Thickness(4, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
            panel.Children.Add(text);
        }
        return panel;
    }

    private void ApplyQuotaVisibility(bool narrow, bool ringMode)
    {
        RingNextResetText.Visibility = narrow || !ShowPrimaryQuota ? Visibility.Collapsed : Visibility.Visible;
        StatusText.TextTrimming = TextTrimming.CharacterEllipsis;
        foreach (var (grid, first) in new[] { (CardQuotaView, (FrameworkElement)PrimaryCard), (RingQuotaView, (FrameworkElement)RingQuotaView.Children[0]) })
        {
            first.Visibility = ShowPrimaryQuota ? Visibility.Visible : Visibility.Collapsed;
            if (!ShowPrimaryQuota)
            {
                if (narrow)
                {
                    grid.RowDefinitions[0].Height = new GridLength(0);
                    grid.RowDefinitions[1].Height = new GridLength(0);
                }
                else
                {
                    grid.ColumnDefinitions[0].Width = new GridLength(0);
                    grid.ColumnDefinitions[1].Width = new GridLength(0);
                }
            }
        }
        if (_compactPrimaryHolder is not null && _compactQuotaGrid is not null)
        {
            _compactPrimaryHolder.Visibility = ShowPrimaryQuota ? Visibility.Visible : Visibility.Collapsed;
            _compactQuotaGrid.ColumnDefinitions[0].Width = ShowPrimaryQuota ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }
        if (_fullLegend is null)
        {
            _fullLegend = CreateRingLegend();
            _fullLegend.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetRow(_fullLegend, 3);
            DesignSurface.Children.Add(_fullLegend);
        }
        _fullLegend.Visibility = ringMode ? Visibility.Visible : Visibility.Collapsed;
        _fullLegend.Opacity = TextOpacity;
        if (_compactLegend is not null) _compactLegend.Visibility = ringMode ? Visibility.Visible : Visibility.Collapsed;
        DesignSurface.RowDefinitions[1].Height = new GridLength(8);
        DesignSurface.RowDefinitions[3].Height = new GridLength(ringMode ? 22 : (_showTokenUsage ? 8 : 0));
    }
}
