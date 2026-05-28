using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using QuickTools.Models;
using QuickTools.ViewModels;

// Resolve WPF ↔ WinForms ambiguities (project uses UseWindowsForms=true)
using UserControl    = System.Windows.Controls.UserControl;
using Image          = System.Windows.Controls.Image;
using Point          = System.Windows.Point;
using Brush          = System.Windows.Media.Brush;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace QuickTools.Views;

public partial class DashboardView : UserControl
{
    // ─── Cards registry ───────────────────────────────────────────────────────────
    private Border[] _cards   = [];
    private Border[] _handles = [];

    // ─── Drag state ───────────────────────────────────────────────────────────────
    private bool    _isDragging;
    private Border? _dragCard;
    private Image?  _ghost;
    private Border? _placeholder;
    private Point   _dragOffset;
    private Border? _highlightTarget;
    private Brush?  _savedTargetBrush;
    private Thickness _savedTargetThickness;

    // ─── Visual constants ─────────────────────────────────────────────────────────
    private static readonly SolidColorBrush DropHighlightBrush =
        new(System.Windows.Media.Color.FromArgb(200, 99, 133, 255));

    private static readonly SolidColorBrush PlaceholderBgBrush =
        new(System.Windows.Media.Color.FromArgb(18, 99, 133, 255));

    private static readonly SolidColorBrush PlaceholderBorderBrush =
        new(System.Windows.Media.Color.FromArgb(100, 99, 133, 255));

    public DashboardView()
    {
        InitializeComponent();
        Loaded             += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    // ─── Initialization ───────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _cards =
        [
            CardAutoClicker, CardQuickToggle, CardPowerMode,
            CardPowerScheduler, CardSystemPerformance
        ];
        _handles =
        [
            HandleAutoClicker, HandleQuickToggle, HandlePowerMode,
            HandlePowerScheduler, HandleSystemPerformance
        ];

        // Attach a single global PreviewMouseLeftButtonDown at the UserControl level.
        // Being at the top of the visual tree it fires first in the tunneling chain —
        // no child element can suppress it. We then check each card's bounds manually.
        PreviewMouseLeftButtonDown += OnGlobalPreviewMouseDown;
        PreviewMouseMove           += OnPreviewMouseMove;
        PreviewMouseLeftButtonUp   += OnPreviewMouseLeftButtonUp;

        if (DataContext is DashboardViewModel vm)
            ApplyCardOrder(vm.CardOrder);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DashboardViewModel old)
            old.CardOrder.CollectionChanged -= OnCardOrderChanged;

        if (e.NewValue is DashboardViewModel vm)
        {
            vm.CardOrder.CollectionChanged += OnCardOrderChanged;
            if (IsLoaded)
                ApplyCardOrder(vm.CardOrder);
        }
    }

    private void OnCardOrderChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            ApplyCardOrder(vm.CardOrder);
    }

    // ─── Flow layout ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes a flow layout where full-width cards always take an entire row
    /// and half-width cards pair up two-per-row.
    /// </summary>
    private static Dictionary<string, (int Row, int Col, int ColSpan)> ComputeLayout(IList<string> order)
    {
        var result = new Dictionary<string, (int, int, int)>();
        int row = 0, i = 0;
        while (i < order.Count)
        {
            var id = order[i];
            if (DashboardCard.IsFullWidth(id))
            {
                result[id] = (row, 0, 3);
                row++; i++;
            }
            else
            {
                // Place first half-width card on the left
                result[id] = (row, 0, 1);
                i++;
                // Pair with next card only if it is also half-width
                if (i < order.Count && !DashboardCard.IsFullWidth(order[i]))
                {
                    result[order[i]] = (row, 2, 1);
                    i++;
                }
                row++;
            }
        }
        return result;
    }

    private void ApplyCardOrder(IList<string> order)
    {
        if (_cards.Length == 0) return;

        var layout = ComputeLayout(order);

        // Sync Grid row definitions to match required row count
        int requiredRows = layout.Count == 0 ? 1 : layout.Values.Max(p => p.Row) + 1;
        while (CardsGrid.RowDefinitions.Count < requiredRows)
            CardsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        while (CardsGrid.RowDefinitions.Count > requiredRows)
            CardsGrid.RowDefinitions.RemoveAt(CardsGrid.RowDefinitions.Count - 1);

        foreach (var (cardId, (r, col, colSpan)) in layout)
        {
            var card = FindCardById(cardId);
            if (card is null) continue;
            Grid.SetRow(card,        r);
            Grid.SetColumn(card,     col);
            Grid.SetColumnSpan(card, colSpan);
        }
    }

    private Border? FindCardById(string id)
    {
        foreach (var card in _cards)
            if (card.Tag is string tag && tag == id)
                return card;
        return null;
    }

    private int SlotIndexOf(Border card)
    {
        if (DataContext is not DashboardViewModel vm) return -1;
        var cardId = card.Tag as string;
        for (var i = 0; i < vm.CardOrder.Count; i++)
            if (vm.CardOrder[i] == cardId)
                return i;
        return -1;
    }

    // ─── Drag start ───────────────────────────────────────────────────────────────

    private void OnGlobalPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging || _cards.Length == 0) return;

        for (var i = 0; i < _cards.Length; i++)
        {
            var card = _cards[i];
            if (card.Visibility != Visibility.Visible) continue;

            var pos = e.GetPosition(card);
            // Restrict to the drag-handle strip (top 28 px) within card bounds.
            if (pos.X >= 0 && pos.X <= card.ActualWidth &&
                pos.Y >= 0 && pos.Y <= 28)
            {
                OnHandleMouseDown(i, e);
                return;
            }
        }
    }

    private void OnHandleMouseDown(int handleIndex, MouseButtonEventArgs e)
    {
        if (_isDragging) return;

        _dragCard   = _cards[handleIndex];
        _dragOffset = e.GetPosition(_dragCard);

        var w = _dragCard.ActualWidth;
        var h = _dragCard.ActualHeight;
        if (w <= 0 || h <= 0) return;

        // ── 1. Ghost image ─────────────────────────────────────────────────────────
        var rtb = new RenderTargetBitmap(
            (int)Math.Ceiling(w), (int)Math.Ceiling(h),
            96, 96, PixelFormats.Pbgra32);
        rtb.Render(_dragCard);

        var scale = new System.Windows.Media.ScaleTransform(0.94, 0.94);
        _ghost = new Image
        {
            Source  = rtb,
            Width   = w,
            Height  = h,
            Opacity = 0,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = scale,
            Effect = new DropShadowEffect
            {
                BlurRadius  = 26,
                ShadowDepth = 8,
                Opacity     = 0.38,
                Color       = Colors.Black
            }
        };

        var cursorPos = e.GetPosition(DragCanvas);
        Canvas.SetLeft(_ghost, cursorPos.X - _dragOffset.X);
        Canvas.SetTop (_ghost, cursorPos.Y - _dragOffset.Y);
        DragCanvas.Children.Add(_ghost);
        DragCanvas.IsHitTestVisible = false;

        // Animate ghost: fade in + lift scale
        var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.18 };
        scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.94, 1.05, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
        scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.94, 1.05, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
        _ghost.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 0.82, TimeSpan.FromMilliseconds(140)));

        // ── 2. Placeholder slot where the card was ─────────────────────────────────
        _placeholder = new Border
        {
            Background      = PlaceholderBgBrush,
            BorderBrush     = PlaceholderBorderBrush,
            BorderThickness = new Thickness(2),
            CornerRadius    = new CornerRadius(20),
            Margin          = _dragCard.Margin,
            MinHeight       = _dragCard.ActualHeight > 0 ? _dragCard.ActualHeight : 176,
            Opacity         = 0
        };
        Grid.SetRow      (_placeholder, Grid.GetRow      (_dragCard));
        Grid.SetColumn   (_placeholder, Grid.GetColumn   (_dragCard));
        Grid.SetColumnSpan(_placeholder, Grid.GetColumnSpan(_dragCard));
        CardsGrid.Children.Add(_placeholder);
        _placeholder.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));

        // Hide the original (keeps layout space, placeholder fills it visually)
        _dragCard.Visibility = Visibility.Hidden;

        _isDragging = true;
        Mouse.Capture(this);
        e.Handled = true;
    }

    // ─── Drag move ────────────────────────────────────────────────────────────────

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _ghost is null || _dragCard is null) return;

        var pos = e.GetPosition(DragCanvas);
        Canvas.SetLeft(_ghost, pos.X - _dragOffset.X);
        Canvas.SetTop (_ghost, pos.Y - _dragOffset.Y);

        var target = HitTestCard(e.GetPosition(CardsGrid));
        if (target != _highlightTarget)
        {
            ClearDropHighlight();
            if (target is not null && target != _dragCard)
            {
                _highlightTarget      = target;
                _savedTargetBrush     = target.BorderBrush;
                _savedTargetThickness = target.BorderThickness;
                target.BorderBrush     = DropHighlightBrush;
                target.BorderThickness = new Thickness(2);
            }
        }
    }

    // ─── Drag end ────────────────────────────────────────────────────────────────

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        EndDrag(e.GetPosition(CardsGrid));
    }

    private void EndDrag(Point dropPos)
    {
        _isDragging = false;
        Mouse.Capture(null);

        // Animate ghost out (quick fade + shrink) then remove
        if (_ghost is not null)
        {
            var g = _ghost;
            var fadeOut = new DoubleAnimation(g.Opacity, 0, TimeSpan.FromMilliseconds(120));
            fadeOut.Completed += (_, _) => DragCanvas.Children.Remove(g);
            g.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            _ghost = null;
        }

        RemovePlaceholder();
        ClearDropHighlight();

        if (_dragCard is not null)
        {
            _dragCard.Visibility = Visibility.Visible;
            _dragCard.Opacity    = 1.0;
        }

        var target = HitTestCard(dropPos);
        if (target is not null && target != _dragCard && DataContext is DashboardViewModel vm)
        {
            var fromIndex = SlotIndexOf(_dragCard!);
            var toIndex   = SlotIndexOf(target);
            if (fromIndex >= 0 && toIndex >= 0)
                vm.MoveCard(fromIndex, toIndex);
        }

        _dragCard = null;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private Border? HitTestCard(Point posInCardsGrid)
    {
        Border? found = null;
        VisualTreeHelper.HitTest(
            CardsGrid,
            potentialHit =>
            {
                if (potentialHit is Image img && img == _ghost)
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                if (potentialHit is Border b && b == _placeholder)
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
                return HitTestFilterBehavior.Continue;
            },
            result =>
            {
                var visual = result.VisualHit as DependencyObject;
                while (visual is not null)
                {
                    if (visual is Border brd && _cards.Contains(brd))
                    {
                        found = brd;
                        return HitTestResultBehavior.Stop;
                    }
                    visual = VisualTreeHelper.GetParent(visual);
                }
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(posInCardsGrid));
        return found;
    }

    private void RemovePlaceholder()
    {
        if (_placeholder is not null)
        {
            CardsGrid.Children.Remove(_placeholder);
            _placeholder = null;
        }
    }

    private void ClearDropHighlight()
    {
        if (_highlightTarget is not null)
        {
            _highlightTarget.BorderBrush     = _savedTargetBrush;
            _highlightTarget.BorderThickness = _savedTargetThickness;
            _highlightTarget  = null;
            _savedTargetBrush = null;
        }
    }
}
