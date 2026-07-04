using AutoWeldSystem.UI.Infrastructure;
using System.Drawing.Drawing2D;

namespace AutoWeldSystem.UI.Controls;

/// <summary>
/// A compact horizontal scrollbar for wide preview tables.
/// It avoids the large native WinForms scrollbar while still exposing direct drag control.
/// </summary>
public sealed class SlimHorizontalScrollBar : Control
{
    private const int DefaultBarHeight = 12;
    private const int TrackHeight = 6;
    private const int TrackPadding = 8;
    private const int MinimumThumbWidth = 32;

    private int _contentWidth;
    private int _viewportWidth;
    private int _value;
    private bool _dragging;
    private bool _hoveringThumb;
    private int _dragOffset;

    public SlimHorizontalScrollBar()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

        Height = DefaultBarHeight;
        MinimumSize = new Size(0, DefaultBarHeight);
        BackColor = Color.LightGray;
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        BackColor = Color.White;
    }

    public event EventHandler? ValueChanged;

    public int Maximum => Math.Max(0, _contentWidth - _viewportWidth);

    public int Value
    {
        get => _value;
        set => SetValue(value, raiseEvent: true);
    }

    /// <summary>
    /// Updates the scroll range from the owner table without raising ValueChanged.
    /// </summary>
    public void SetScrollInfo(int contentWidth, int viewportWidth, int value)
    {
        _contentWidth = Math.Max(0, contentWidth);
        _viewportWidth = Math.Max(0, viewportWidth);
        var shouldBeVisible = Maximum > 0;

        SetValue(value, raiseEvent: false);

        if (Visible != shouldBeVisible)
        {
            Visible = shouldBeVisible;
        }
        else if (Visible)
        {
            Invalidate();
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible && IsHandleCreated)
        {
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.White);

        if (Maximum <= 0 || Width <= TrackPadding * 2 || Height <= 0)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var trackBrush = new SolidBrush(UiColors.Table.GridLineColor);
        using var thumbBrush = new SolidBrush(_dragging || _hoveringThumb
            ? UiColors.Status.Muted
            : Color.FromArgb(148, 163, 184));

        FillRounded(e.Graphics, trackBrush, GetTrackBounds(), TrackHeight / 2);
        FillRounded(e.Graphics, thumbBrush, GetThumbBounds(), TrackHeight / 2);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left || Maximum <= 0)
        {
            return;
        }

        var thumb = GetThumbBounds();
        if (thumb.Contains(e.Location))
        {
            _dragging = true;
            _dragOffset = e.X - thumb.X;
            Capture = true;
            Invalidate();
            return;
        }

        Value += e.X < thumb.X ? -Math.Max(1, _viewportWidth) : Math.Max(1, _viewportWidth);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_dragging)
        {
            Value = ValueFromThumbX(e.X - _dragOffset);
            return;
        }

        var hovering = GetThumbBounds().Contains(e.Location);
        if (_hoveringThumb != hovering)
        {
            _hoveringThumb = hovering;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        if (_dragging || !_hoveringThumb)
        {
            return;
        }

        _hoveringThumb = false;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Capture = false;
        Invalidate();
    }

    private void SetValue(int value, bool raiseEvent)
    {
        var nextValue = Math.Clamp(value, 0, Maximum);
        if (_value == nextValue)
        {
            return;
        }

        _value = nextValue;
        Invalidate();

        if (raiseEvent)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int ValueFromThumbX(int thumbX)
    {
        var track = GetTrackBounds();
        var thumb = GetThumbBounds();
        var movableWidth = Math.Max(1, track.Width - thumb.Width);
        var normalizedX = Math.Clamp(thumbX - track.X, 0, movableWidth);
        return (int)Math.Round(normalizedX * (double)Maximum / movableWidth);
    }

    private Rectangle GetTrackBounds()
    {
        var width = Math.Max(1, Width - TrackPadding * 2);
        return new Rectangle(TrackPadding, (Height - TrackHeight) / 2, width, TrackHeight);
    }

    private Rectangle GetThumbBounds()
    {
        var track = GetTrackBounds();
        var thumbWidth = Math.Max(
            MinimumThumbWidth,
            (int)Math.Round(track.Width * (_viewportWidth / (double)Math.Max(1, _contentWidth))));
        thumbWidth = Math.Min(track.Width, thumbWidth);

        var movableWidth = Math.Max(1, track.Width - thumbWidth);
        var x = track.X + (int)Math.Round(movableWidth * (_value / (double)Math.Max(1, Maximum)));
        return new Rectangle(x, track.Y, thumbWidth, track.Height);
    }

    private static void FillRounded(Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        if (bounds.Width <= radius * 2 || bounds.Height <= radius * 2)
        {
            graphics.FillRectangle(brush, bounds);
            return;
        }

        using var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
