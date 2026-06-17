using System.Drawing;

namespace AutoWeldSystem.UI.Infrastructure;

/// <summary>
/// Central color palette for UI code.
/// Named colors make intent clear at call sites and keep raw RGB values in one place.
/// </summary>
public static class UiColors
{
    /// <summary>
    /// Status colors used by connection, device, and operation state indicators.
    /// </summary>
    public static class Status
    {
        /// <summary>
        /// blue
        /// </summary>
        public static readonly Color Primary = Color.FromArgb(13, 110, 253);
        /// <summary>
        /// Green
        /// </summary>
        public static readonly Color Success = Color.FromArgb(25, 135, 84);
        /// <summary>
        /// Orange
        /// </summary>
        public static readonly Color Business = Color.FromArgb(180, 83, 9);
        /// <summary>
        /// Yellow
        /// </summary>
        public static readonly Color Warning = Color.FromArgb(255, 193, 7);
        /// <summary>
        /// Red
        /// </summary>
        public static readonly Color Danger = Color.FromArgb(220, 53, 69);
        /// <summary>
        /// Gray
        /// </summary>
        public static readonly Color Muted = Color.FromArgb(108, 117, 125);
    }

    /// <summary>
    /// Neutral table colors shared by DataGridView and AntdUI.Table.
    /// </summary>
    public static class Table
    {
        public static readonly Color HeaderBackColor = Color.FromArgb(248, 250, 252);
        public static readonly Color HeaderForeColor = Color.FromArgb(31, 41, 55);
        public static readonly Color GridLineColor = Color.FromArgb(226, 232, 240);
        public static readonly Color TextColor = Color.FromArgb(33, 37, 41);
        public static readonly Color AlternateRowColor = Color.FromArgb(249, 250, 251);
        public static readonly Color SelectionBackColor = Color.FromArgb(219, 234, 254);
        public static readonly Color SelectionForeColor = Color.FromArgb(17, 24, 39);
    }
}
