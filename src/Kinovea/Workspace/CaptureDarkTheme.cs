/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    // Dark, technical look for the guided capture screen: near-black panels, one lime accent,
    // amber for things to check. Everything here changes colours and grouping only.
    internal static class CaptureDark
    {
        public static readonly Color Bg = Color.FromArgb(11, 16, 14);
        public static readonly Color Panel = Color.FromArgb(17, 25, 22);
        public static readonly Color Card = Color.FromArgb(23, 32, 28);
        public static readonly Color CardBorder = Color.FromArgb(42, 56, 50);
        public static readonly Color Field = Color.FromArgb(11, 16, 14);
        public static readonly Color Text = Color.FromArgb(228, 236, 231);
        public static readonly Color Soft = Color.FromArgb(182, 196, 188);
        public static readonly Color Muted = Color.FromArgb(143, 161, 152);
        public static readonly Color Accent = Color.FromArgb(191, 244, 74);
        public static readonly Color AccentText = Color.FromArgb(16, 26, 8);
        public static readonly Color Amber = Color.FromArgb(255, 176, 74);
        public static readonly Color Red = Color.FromArgb(255, 122, 110);
        public static readonly Color Button = Color.FromArgb(31, 44, 39);
        public static readonly Color ButtonBorder = Color.FromArgb(52, 70, 63);

        public static Font Mono(float size, FontStyle style)
        {
            return new Font("Consolas", size, style);
        }

        // The form was written for a light background and sets some colours at run time. This maps
        // those light-theme colours to their dark-theme equivalents; any other colour is unchanged.
        public static Color MapForeColor(Color color)
        {
            if (color.IsEmpty)
                return color;
            int r = color.R, g = color.G, b = color.B;
            if (r == 60 && g == 145 && b == 76)
                return Accent;
            if (r == 85 && g == 122 && b == 18)
                return Accent;
            if (r == 166 && g == 92 && b == 34)
                return Amber;
            if (r == 176 && g == 52 && b == 52)
                return Red;
            if ((r == 92 && g == 104 && b == 98) || (r == 74 && g == 87 && b == 81))
                return Muted;
            if ((r == 24 && g == 31 && b == 29) || (r == 13 && g == 19 && b == 17))
                return Text;
            if (r == 31 && g == 41 && b == 36)
                return Text;
            return color;
        }

        public static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? Accent : ButtonBorder;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(206, 249, 112) : Color.FromArgb(40, 56, 50);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(160, 214, 50) : Color.FromArgb(24, 34, 30);
            button.BackColor = primary ? Accent : Button;
            button.ForeColor = primary ? AccentText : Text;
            button.Font = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        private static bool IsLight(Color color)
        {
            return color.A == 255 && color.GetBrightness() > 0.55f;
        }

        // Recolours every control below root. Buttons are styled where they are created.
        public static void ApplyTree(Control root)
        {
            foreach (Control control in root.Controls)
            {
                Label label = control as Label;
                CheckBox check = control as CheckBox;
                if (label != null && !(label is ResultsTable))
                {
                    label.ForeColor = MapForeColor(label.ForeColor);
                    label.ForeColorChanged += RemapForeColor;
                    if (IsLight(label.BackColor))
                        label.BackColor = Color.Transparent;
                }
                else if (check != null)
                {
                    check.ForeColor = Text;
                    check.BackColor = Color.Transparent;
                    check.ForeColorChanged += RemapForeColor;
                }
                else if (control is NumericUpDown || control is TextBox)
                {
                    control.BackColor = Field;
                    control.ForeColor = Text;
                }
                else if (control is ComboBox)
                {
                    ComboBox combo = (ComboBox)control;
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.BackColor = Field;
                    combo.ForeColor = Text;
                }
                else if (control is TableLayoutPanel || control is FlowLayoutPanel || control is Panel)
                {
                    if (!(control is CaptureCard) && IsLight(control.BackColor))
                        control.BackColor = Card;
                }

                if (control.HasChildren)
                    ApplyTree(control);
            }
        }

        private static void RemapForeColor(object sender, EventArgs e)
        {
            Control control = sender as Control;
            if (control == null)
                return;
            Color mapped = MapForeColor(control.ForeColor);
            if (mapped != control.ForeColor)
                control.ForeColor = mapped;
        }
    }

    // A titled group of related controls. One column, grows to fit, optionally collapsible.
    internal class CaptureCard : TableLayoutPanel
    {
        private readonly Label header;
        private readonly bool collapsible;
        private readonly string title;
        private bool collapsed;

        public CaptureCard(string title, bool collapsible, bool startCollapsed)
        {
            this.title = title;
            this.collapsible = collapsible;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            Dock = DockStyle.Top;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ColumnCount = 1;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            BackColor = CaptureDark.Card;
            Padding = new Padding(12, 8, 12, 6);
            Margin = new Padding(0, 0, 0, 10);

            if (!string.IsNullOrEmpty(title))
            {
                header = new Label();
                header.Dock = DockStyle.Fill;
                header.Height = 24;
                header.ForeColor = CaptureDark.Muted;
                header.BackColor = Color.Transparent;
                header.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
                header.TextAlign = ContentAlignment.MiddleLeft;
                header.Margin = new Padding(0, 0, 0, 4);
                if (collapsible)
                {
                    header.Cursor = Cursors.Hand;
                    header.Click += delegate { SetCollapsed(!collapsed); };
                }
                RowCount = 1;
                RowStyles.Add(new RowStyle(SizeType.AutoSize));
                Controls.Add(header, 0, 0);
            }

            collapsed = collapsible && startCollapsed;
            UpdateHeaderText();
        }

        public void AddRow(Control control)
        {
            AddRow(control, 6);
        }

        public void AddRow(Control control, int bottomMargin)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 0, 0, bottomMargin);
            int row = RowCount;
            RowCount = row + 1;
            RowStyles.Add(new RowStyle(collapsed ? SizeType.Absolute : SizeType.AutoSize, 0));
            Controls.Add(control, 0, row);
            control.Visible = !collapsed;
        }

        private void SetCollapsed(bool value)
        {
            collapsed = value;
            SuspendLayout();
            for (int row = 1; row < RowStyles.Count; row++)
            {
                Control control = GetControlFromPosition(0, row);
                if (control != null)
                    control.Visible = !collapsed;
                RowStyles[row] = collapsed ? new RowStyle(SizeType.Absolute, 0) : new RowStyle(SizeType.AutoSize);
            }
            UpdateHeaderText();
            ResumeLayout(true);
            Invalidate();
        }

        private void UpdateHeaderText()
        {
            if (header == null)
                return;
            string glyph = !collapsible ? string.Empty : (collapsed ? "▸  " : "▾  ");
            header.Text = glyph + title;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : CaptureDark.Panel);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle body = new Rectangle(0, 0, Width - 1, Height - 1);
            if (body.Width < 8 || body.Height < 8)
                return;
            using (GraphicsPath path = Rounded(body, 8))
            using (SolidBrush fill = new SolidBrush(BackColor))
            using (Pen border = new Pen(CaptureDark.CardBorder, 1F))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }

        internal static GraphicsPath Rounded(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Five segments, filled up to the current step.
    internal class StepStrip : Control
    {
        private int step = 1;
        private const int Segments = 5;

        public StepStrip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            Height = 10;
            TabStop = false;
        }

        public int Step
        {
            get { return step; }
            set
            {
                int clamped = Math.Max(1, Math.Min(Segments, value));
                if (clamped == step)
                    return;
                step = clamped;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : CaptureDark.Panel);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int gap = 4;
            int width = (Width - gap * (Segments - 1)) / Segments;
            if (width < 4)
                return;
            int top = (Height - 4) / 2;
            for (int index = 0; index < Segments; index++)
            {
                Rectangle bar = new Rectangle(index * (width + gap), top, width, 4);
                Color color = index < step ? CaptureDark.Accent : CaptureDark.CardBorder;
                using (GraphicsPath path = CaptureCard.Rounded(bar, 2))
                using (SolidBrush brush = new SolidBrush(color))
                    e.Graphics.FillPath(brush, path);
            }
        }
    }

    // The metrics text drawn as a table: label on the left, value right-aligned in a monospaced
    // face. It is still a Label, so the rest of the form keeps assigning Text as before. Lines in
    // the form "Name: value" become rows; other lines are shown as wrapped notes.
    internal class ResultsTable : Label
    {
        private sealed class Row
        {
            public string Key;
            public string Value;
            public bool Stacked;
            public bool Note;
            public bool Gap;
            public int Height;
        }

        private static readonly Font KeyFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        private static readonly Font ValueFont = new Font("Consolas", 9.5F, FontStyle.Bold);
        private static readonly Font NoteFont = new Font("Segoe UI", 9F, FontStyle.Regular);
        private const int Pad = 2;

        public ResultsTable()
        {
            AutoSize = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            BackColor = Color.Transparent;
        }

        private List<Row> BuildRows(int width)
        {
            List<Row> rows = new List<Row>();
            string[] lines = (Text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            bool first = true;
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (first)
                {
                    first = false;
                    // The first line is the section title; the card already has one.
                    if (line.EndsWith(":", StringComparison.Ordinal))
                        continue;
                }

                if (line.Length == 0)
                {
                    rows.Add(new Row { Gap = true, Height = 6 });
                    continue;
                }

                int split = line.IndexOf(": ", StringComparison.Ordinal);
                if (split <= 0)
                {
                    Row note = new Row { Key = line, Note = true };
                    note.Height = MeasureWrapped(line, NoteFont, width) + 4;
                    rows.Add(note);
                    continue;
                }

                string key = line.Substring(0, split);
                string value = line.Substring(split + 2).Trim();
                Row row = new Row { Key = key, Value = value };
                int keyWidth = TextRenderer.MeasureText(key, KeyFont, new Size(width, 100), TextFormatFlags.NoPadding).Width;
                int valueWidth = TextRenderer.MeasureText(value, ValueFont, new Size(width, 100), TextFormatFlags.NoPadding).Width;
                if (keyWidth + valueWidth + 16 > width)
                {
                    row.Stacked = true;
                    row.Height = 18 + MeasureWrapped(value, ValueFont, width) + 6;
                }
                else
                {
                    row.Height = 22;
                }
                rows.Add(row);
            }
            return rows;
        }

        private static int MeasureWrapped(string text, Font font, int width)
        {
            return TextRenderer.MeasureText(text, font, new Size(Math.Max(40, width), 2000), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
        }

        private int ContentWidth
        {
            get { return Math.Max(60, ClientSize.Width - Pad * 2); }
        }

        private void FitHeight()
        {
            int total = Pad * 2;
            foreach (Row row in BuildRows(ContentWidth))
                total += row.Height;
            total = Math.Max(48, total);
            if (Height != total)
                Height = total;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            FitHeight();
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            FitHeight();
            Invalidate();
        }

        private static Color ValueColor(string value)
        {
            string lower = value.ToLowerInvariant();
            if (lower.Contains("agrees") || lower.Contains("high") || lower.Contains("verified") || lower.Contains("applied"))
                return CaptureDark.Accent;
            if (lower.Contains("not ") || lower.Contains("review") || lower.Contains("off") || lower.Contains("skipped") || lower.Contains("estimate"))
                return CaptureDark.Amber;
            return CaptureDark.Text;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : CaptureDark.Card);
            int width = ContentWidth;
            int y = Pad;
            foreach (Row row in BuildRows(width))
            {
                if (row.Gap)
                {
                    y += row.Height;
                    continue;
                }

                if (row.Note)
                {
                    Rectangle noteBounds = new Rectangle(Pad, y, width, row.Height);
                    TextRenderer.DrawText(e.Graphics, row.Key, NoteFont, noteBounds, CaptureDark.Soft,
                        TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                    y += row.Height;
                    continue;
                }

                Color valueColor = ValueColor(row.Value);
                if (row.Stacked)
                {
                    TextRenderer.DrawText(e.Graphics, row.Key, KeyFont, new Rectangle(Pad, y, width, 18), CaptureDark.Soft,
                        TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                    TextRenderer.DrawText(e.Graphics, row.Value, ValueFont, new Rectangle(Pad, y + 18, width, row.Height - 18), valueColor,
                        TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, row.Key, KeyFont, new Rectangle(Pad, y, width, row.Height), CaptureDark.Soft,
                        TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                    TextRenderer.DrawText(e.Graphics, row.Value, ValueFont, new Rectangle(Pad, y, width, row.Height), valueColor,
                        TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
                }

                using (Pen line = new Pen(Color.FromArgb(34, 46, 41), 1F))
                    e.Graphics.DrawLine(line, Pad, y + row.Height - 1, Pad + width, y + row.Height - 1);
                y += row.Height;
            }
        }
    }
}
