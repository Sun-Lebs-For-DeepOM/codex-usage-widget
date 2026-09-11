using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Documents;

namespace CodexUsageWidget;

public partial class MainWindow
{
    private bool _narrowLayout;
    private bool _compactLayout;
    private bool _arrangingResponsive;
    private Grid? _compactPanel;
    private readonly List<FrameworkElement> _compactRingViews = new();
    private readonly List<FrameworkElement> _compactCardViews = new();
    private readonly List<Controls.CircularProgressRing> _compactRings = new();
    private readonly List<TextBlock> _compactPercentageTexts = new();
    private Window? _detailsWindow;
    private TextBlock? _compactTotalText;
    private TextBlock? _compactAmountText;
    private readonly Dictionary<TextBlock, double> _baseFontSizes = new();
    private double _backgroundOpacity = 1;
    public double BackgroundOpacity => _backgroundOpacity;
    public double TextOpacity { get; private set; } = 1;
    public double SmallWindowScale { get; private set; } = 1;

    public void ApplyTextOpacity(double value)
    {
        TextOpacity = NormalizeFinite(value, 1, 0, 1);
        ApplyTextAlpha();
        ScheduleSettingsSave();
    }

    private void ApplyTextAlpha()
    {
        foreach (var text in _baseFontSizes.Keys) text.Opacity = TextOpacity;
        if (_fullLegend is not null) _fullLegend.Opacity = TextOpacity;
        if (_compactPanel is not null)
            foreach (var text in VisualDescendants(_compactPanel).OfType<TextBlock>()) text.Opacity = TextOpacity;
    }

    private void InitializeResponsiveLayout()
    {
        foreach (var text in VisualDescendants(DesignSurface).OfType<TextBlock>())
            _baseFontSizes[text] = text.FontSize;
        SizeChanged += (_, _) => UpdateResponsiveLayout();
    }

    public void UpdateResponsiveLayout()
    {
        if (_arrangingResponsive || DesignSurface is null) return;
        _arrangingResponsive = true;
        try
        {
            var width = ActualWidth > 0 ? ActualWidth : Width;
            var height = ActualHeight > 0 ? ActualHeight : Height;
            var micro = width < 300 || height < 240;
            SmallWindowScale = micro ? Math.Min((width - 2) / 298, (height - 2) / 238) : 1;
            var contentWidth = micro ? (width - 2) / SmallWindowScale + 2 : width;
            var contentHeight = micro ? (height - 2) / SmallWindowScale + 2 : height;
            // Hysteresis avoids switching repeatedly while dragging near a breakpoint.
            _narrowLayout = _narrowLayout ? width < 456 : width < 432;
            var narrow = _narrowLayout;
            var fullDiameter = Math.Clamp(width * 0.30, 128, 174);
            PrimaryRing.Width = PrimaryRing.Height = SecondaryRing.Width = SecondaryRing.Height = fullDiameter;
            PrimaryRing.StrokeThickness = SecondaryRing.StrokeThickness = 7;
            foreach (var timeRing in _resetCycleRings)
                if (timeRing is not null) timeRing.Width = timeRing.Height = fullDiameter - 20;
            var quotaHeight = _viewStyle == Models.WidgetViewStyle.Ring
                ? narrow && ShowPrimaryQuota ? 410 : 208
                : narrow && ShowPrimaryQuota ? 266 : 128;
            var tokenHeight = _showTokenUsage ? narrow ? 310 : 182 : 0;
            var outerPadding = AllowsTransparency ? 48 : 28;
            var legendHeight = _viewStyle == Models.WidgetViewStyle.Ring ? 22 : (_showTokenUsage ? 8 : 0);
            var required = 42 + 8 + quotaHeight + legendHeight + tokenHeight + 8 + 26 + outerPadding;
            _compactLayout = micro || (_compactLayout ? height < required + 6 : height < required - 6);

            ReflowPair(CardQuotaView, PrimaryCard, SecondaryCard, narrow, 128);
            ReflowPair(RingQuotaView, (FrameworkElement)RingQuotaView.Children[0],
                (FrameworkElement)RingQuotaView.Children[1], narrow, 198);
            var amountGrid = (Grid)((FrameworkElement)TokenTotalText.Parent).Parent;
            ReflowPair(amountGrid, (FrameworkElement)TokenTotalText.Parent,
                (FrameworkElement)EstimatedCostText.Parent, narrow, 53);
            var categoryGrid = (Grid)((FrameworkElement)RegularInputTokenText.Parent).Parent;
            ReflowPair(categoryGrid, (FrameworkElement)RegularInputTokenText.Parent,
                (FrameworkElement)CachedInputTokenText.Parent, narrow, 42);
            var tokenGrid = (Grid)TokenUsagePanel.Child;
            tokenGrid.RowDefinitions[1].Height = new GridLength(narrow ? 118 : 60);
            tokenGrid.RowDefinitions[3].Height = new GridLength(narrow ? 96 : 44);
            tokenGrid.RowDefinitions[4].Height = new GridLength(narrow ? 36 : 20);
            TokenEstimateNoteText.TextWrapping = TextWrapping.Wrap;
            TokenEstimateNoteText.TextTrimming = TextTrimming.None;
            EstimatedCostText.TextWrapping = TextWrapping.Wrap;
            EstimatedCostText.TextTrimming = TextTrimming.None;
            ServiceTierEstimatedCostText.TextWrapping = TextWrapping.Wrap;
            ServiceTierEstimatedCostText.TextTrimming = TextTrimming.None;
            QuotaContentRow.Height = new GridLength(quotaHeight);
            TokenContentRow.Height = new GridLength(tokenHeight);
            DesignSurface.Height = required - outerPadding;
            PlanBorder.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;

            // Change font sizes, not a transform of the entire UI.
            var fontScale = Math.Clamp(width / 540d, 0.94, 1.12);
            foreach (var (text, originalSize) in _baseFontSizes)
                text.FontSize = originalSize >= 20
                    ? Math.Clamp(originalSize * fontScale, 22, 34)
                    : Math.Clamp(originalSize * fontScale, 11, 18);
            var scroll = (ScrollViewer)DesignSurface.Parent;
            scroll.Visibility = _compactLayout ? Visibility.Collapsed : Visibility.Visible;
            _compactPanel ??= CreateCompactPanel((Grid)scroll.Parent);
            _compactPanel.LayoutTransform = micro ? new ScaleTransform(SmallWindowScale, SmallWindowScale) : Transform.Identity;
            _compactPanel.Width = micro ? contentWidth - 34 : double.NaN;
            _compactPanel.Height = micro ? contentHeight - 24 : double.NaN;
            _compactPanel.Margin = new Thickness(16 * SmallWindowScale, 10 * SmallWindowScale, 16 * SmallWindowScale, 12 * SmallWindowScale);
            _compactPanel.HorizontalAlignment = micro ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
            _compactPanel.VerticalAlignment = micro ? VerticalAlignment.Center : VerticalAlignment.Stretch;
            var summaryFontSize = Math.Clamp(18 + (contentWidth - 300) / 20, 18, 24);
            _compactTotalText!.FontSize = _compactAmountText!.FontSize = summaryFontSize;
            var ringMode = _viewStyle == Models.WidgetViewStyle.Ring;
            foreach (var view in _compactRingViews) view.Visibility = ringMode ? Visibility.Visible : Visibility.Collapsed;
            foreach (var view in _compactCardViews) view.Visibility = ringMode ? Visibility.Collapsed : Visibility.Visible;
            // Statistics fold first; ring mode keeps its circles even at the minimum size.
            var summaryHeight = _showTokenUsage ? 54 : 0;
            var availableRingHeight = contentHeight - 24 - 18 - (ringMode ? 16 : 0) - summaryHeight - 20;
            var stackedRings = ShowPrimaryQuota && contentHeight > contentWidth * 1.45;
            var ringRows = stackedRings ? 2 : 1;
            var diameter = Math.Clamp(Math.Min((availableRingHeight - (stackedRings ? 12 : 0)) / ringRows - 18,
                (contentWidth - 60) / (ShowPrimaryQuota && !stackedRings ? 2 : 1)), 88, 174);
            foreach (var ring in _compactRings) ring.Width = ring.Height = diameter;
            foreach (var text in _compactPercentageTexts) text.FontSize = Math.Clamp(diameter * 0.25, 22, 34);
            foreach (var timeRing in _compactResetCycleRings)
                if (timeRing is not null) timeRing.Width = timeRing.Height = diameter - 16;
            _compactPanel.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            _compactPanel.RowDefinitions[2].Height = new GridLength(ringMode ? 16 : 0);
            _compactPanel.RowDefinitions[3].Height = new GridLength(summaryHeight);
            _compactPanel.Visibility = _compactLayout ? Visibility.Visible : Visibility.Collapsed;
            ApplyQuotaVisibility(narrow, ringMode);
            if (_compactQuotaGrid is not null)
            {
                _compactQuotaGrid.RowDefinitions.Clear();
                _compactQuotaGrid.ColumnDefinitions.Clear();
                _compactQuotaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = !ShowPrimaryQuota ? new GridLength(0) : new GridLength(1, GridUnitType.Star) });
                if (stackedRings)
                {
                    _compactQuotaGrid.RowDefinitions.Add(new RowDefinition());
                    _compactQuotaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
                    _compactQuotaGrid.RowDefinitions.Add(new RowDefinition());
                }
                else _compactQuotaGrid.ColumnDefinitions.Add(new ColumnDefinition());
                var first = (FrameworkElement)_compactQuotaGrid.Children[0];
                var second = (FrameworkElement)_compactQuotaGrid.Children[1];
                Grid.SetColumn(first, 0); Grid.SetRow(first, 0);
                Grid.SetColumn(second, stackedRings ? 0 : 1); Grid.SetRow(second, stackedRings ? 2 : 0);
            }
            ApplyTextAlpha();
            UpdateResetCycle();
        }
        finally { _arrangingResponsive = false; }
    }

    private static void ReflowPair(Grid grid, FrameworkElement first, FrameworkElement second, bool stacked, double rowHeight)
    {
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();
        foreach (var child in grid.Children.OfType<FrameworkElement>())
            if (child != first && child != second) child.Visibility = Visibility.Collapsed;
        first.Visibility = second.Visibility = Visibility.Visible;
        if (stacked)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowHeight) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(rowHeight) });
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        }
        Grid.SetColumn(first, 0); Grid.SetRow(first, 0);
        Grid.SetColumn(second, stacked ? 0 : 2); Grid.SetRow(second, stacked ? 2 : 0);
    }

    private TextBlock BoundText(object source, string path, double fontSize = 12, bool primary = true)
    {
        var text = new TextBlock
        {
            FontFamily = (FontFamily)FindResource("UiFont"), FontSize = fontSize,
            TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, primary ? "TextPrimary" : "TextSecondary");
        text.SetBinding(TextBlock.TextProperty, new Binding(path) { Source = source, Mode = BindingMode.OneWay });
        return text;
    }

    private Grid CreateCompactPanel(Grid parent)
    {
        var panel = new Grid { Margin = new Thickness(16, 10, 16, 12) };
        foreach (var size in new[] { 18d, 108d, 16d, 54d, 20d })
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(size) });
        var header = new DockPanel { Background = Brushes.Transparent, LastChildFill = false };
        header.MouseLeftButtonDown += DragRegion_MouseLeftButtonDown;
        var settings = new Button { Content = "⚙", Style = (Style)FindResource("IconButton") };
        settings.Click += Appearance_Click;
        DockPanel.SetDock(settings, Dock.Right); header.Children.Add(settings);
        _compactLegend = CreateRingLegend();
        _compactLegend.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetRow(_compactLegend, 2);
        panel.Children.Add(_compactLegend);
        panel.Children.Add(header);
        var quotas = new Grid { MaxWidth = 420, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _compactQuotaGrid = quotas;
        quotas.ColumnDefinitions.Add(new ColumnDefinition()); quotas.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(quotas, 1); panel.Children.Add(quotas);
        foreach (var (index, label, percent, reset) in new[]
        {
            (0, PrimaryTitle, PrimaryPercentRun, PrimaryResetText),
            (1, SecondaryTitle, SecondaryPercentRun, SecondaryResetText)
        })
        {
            var block = new StackPanel { Margin = new Thickness(index == 0 ? 0 : 8, 0, 0, 0) };
            block.Children.Add(BoundText(label, "Text", 11, false));
            var value = BoundText(percent, "Text", 26);
            value.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = percent, StringFormat = "{0}% 剩余" });
            block.Children.Add(value); block.Children.Add(BoundText(reset, "Text", 11, false));
            var holder = new Grid { MinWidth = 120 };
            if (index == 0) _compactPrimaryHolder = holder;
            Grid.SetColumn(holder, index); quotas.Children.Add(holder);
            holder.Children.Add(block); _compactCardViews.Add(block);
            var ringBlock = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            var circle = new Grid();
            var original = index == 0 ? PrimaryRing : SecondaryRing;
            var ring = new Controls.CircularProgressRing { Width = 88, Height = 88, StrokeThickness = 6 };
            ring.SetBinding(Controls.CircularProgressRing.ValueProperty, new Binding("Value") { Source = original });
            ring.SetBinding(Controls.CircularProgressRing.ProgressBrushProperty, new Binding("ProgressBrush") { Source = original });
            ring.SetBinding(Controls.CircularProgressRing.TrackBrushProperty, new Binding("TrackBrush") { Source = original });
            circle.Children.Add(ring); _compactRings.Add(ring);
            var timeRing = CreateResetCycleRing();
            _compactResetCycleRings[index] = timeRing;
            circle.Children.Insert(0, timeRing);
            circle.SetBinding(ToolTipProperty, new Binding("ToolTip") { Source = _resetCycleCaptions[index] });
            var inside = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            var ringTitle = BoundText(label, "Text", 11, false); ringTitle.HorizontalAlignment = HorizontalAlignment.Center;
            inside.Children.Add(ringTitle);
            var ringValue = BoundText(percent, "Text", 22);
            _compactPercentageTexts.Add(ringValue);
            ringValue.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = percent, StringFormat = "{0}%" });
            ringValue.HorizontalAlignment = HorizontalAlignment.Center; inside.Children.Add(ringValue);
            var remainingLabel = new TextBlock { Text = "剩余", FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center };
            remainingLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
            inside.Children.Add(remainingLabel);
            circle.Children.Add(inside); ringBlock.Children.Add(circle);
            var ringReset = BoundText(reset, "Text", 11, false); ringReset.HorizontalAlignment = HorizontalAlignment.Center;
            ringReset.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = reset, Converter = new CompactCountdownConverter() });
            ringReset.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = _shortCycleCaptions[index] });
            ringReset.SetBinding(ToolTipProperty, new Binding("ToolTip") { Source = _shortCycleCaptions[index] });
            ringBlock.Children.Add(ringReset); holder.Children.Add(ringBlock); _compactRingViews.Add(ringBlock);
        }
        var summaryBorder = new Border { MaxWidth = 420, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 6, 0, 0) };
        summaryBorder.SetResourceReference(Border.BorderBrushProperty, "PanelBorder");
        Grid.SetRow(summaryBorder, 3); panel.Children.Add(summaryBorder);
        summaryBorder.SetBinding(VisibilityProperty, new Binding("Visibility") { Source = TokenUsagePanel });
        var amounts = new Grid(); summaryBorder.Child = amounts;
        amounts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
        amounts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        amounts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) });
        var tokenColumn = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _compactTotalText = BoundText(TokenTotalText, "Text", 18); _compactTotalText.Name = "CompactTokenTotal";
        tokenColumn.Children.Add(_compactTotalText);
        var tokenCaption = new TextBlock { Text = "总 Token", FontSize = 11, Margin = new Thickness(0, 3, 0, 0) };
        tokenCaption.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary"); tokenColumn.Children.Add(tokenCaption);
        amounts.Children.Add(tokenColumn);
        var amountColumn = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        _compactAmountText = BoundText(EstimatedCostText, "Text", 18); _compactAmountText.Name = "CompactMainAmount";
        amountColumn.Children.Add(_compactAmountText);
        var amountCaption = new TextBlock { Text = "等价估算", FontSize = 11, Margin = new Thickness(0, 3, 0, 0) };
        amountCaption.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary"); amountColumn.Children.Add(amountCaption);
        Grid.SetColumn(amountColumn, 2); amounts.Children.Add(amountColumn);
        var detailButton = new Button { Content = "↗", ToolTip = "展开详细统计", HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(8, 0, 8, 0), Background = Brushes.Transparent, BorderThickness = new Thickness(0), FontSize = 16 };
        var resetCards = BoundText(BadgeText, "Text", 11);
        resetCards.Name = "CompactResetCards";
        resetCards.HorizontalAlignment = HorizontalAlignment.Left;
        resetCards.SetBinding(ToolTipProperty, new Binding("ToolTip") { Source = BadgeBorder });
        var footer = new Grid { MaxWidth = 420 };
        Grid.SetRow(footer, 4); panel.Children.Add(footer);
        footer.Children.Add(resetCards);
        detailButton.SetResourceReference(Control.ForegroundProperty, "TextPrimary");
        detailButton.Click += (_, _) => ShowUsageDetails();
        footer.Children.Add(detailButton);
        parent.Children.Add(panel);
        return panel;
    }

    private void ShowUsageDetails()
    {
        if (_detailsWindow is not null) { _detailsWindow.Activate(); return; }
        var content = new StackPanel { Margin = new Thickness(20) };
        foreach (var (label, source) in new (string, TextBlock)[]
        {
            ("额度 · 窗口一", PrimaryResetText), ("额度 · 窗口二", SecondaryResetText),
            ("窗口一 · 周期已过 / 剩余", _resetCycleCaptions[0]),
            ("窗口二 · 周期已过 / 剩余", _resetCycleCaptions[1]),
            ("Token 总量", TokenTotalText), ("Standard API 等价金额", EstimatedCostText),
            ("速度档位估算", ServiceTierEstimatedCostText), ("普通输入", RegularInputTokenText),
            ("缓存输入", CachedInputTokenText), ("可见输出", VisibleOutputTokenText),
            ("推理输出", ReasoningOutputTokenText), ("估算覆盖情况", TokenEstimateNoteText)
        })
        {
            content.Children.Add(new TextBlock { Text = label, Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 12, 0, 4) });
            var value = BoundText(source, "Text", 16); value.Foreground = Brushes.Black;
            content.Children.Add(value);
        }
        _detailsWindow = new Window { Title = "Codex · 详细统计", Width = 380, Height = 650, MinWidth = 300, MinHeight = 300, Owner = this,
            Topmost = Topmost, Background = Brushes.White, Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        _detailsWindow.Closed += (_, _) => _detailsWindow = null;
        _detailsWindow.Show();
    }

    private sealed class CompactCountdownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (value?.ToString() ?? "待同步").Replace("重置时间未知", "待同步").Replace("后重置", "").Replace("重置：", "").Replace(" ", "");
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => Binding.DoNothing;
    }

    private static IEnumerable<DependencyObject> VisualDescendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (var next in VisualDescendants(child)) yield return next;
        }
    }
}
