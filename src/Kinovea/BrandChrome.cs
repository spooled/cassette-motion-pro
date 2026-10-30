/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CassetteMotionPro
{
    // The sprocket mark, drawn with GDI+ so it stays sharp at any size.
    internal static class BrandMark
    {
        private static readonly Color Ink = Color.FromArgb(11, 16, 14);
        private static readonly Color Line = Color.FromArgb(42, 56, 50);
        private static PointF[] cog;

        private static PointF[] Cog()
        {
            if (cog != null)
                return cog;
            const int teeth = 14;
            const double outer = 33, inner = 27;
            double[] stops = { 0, 0.12, 0.22, 0.5, 0.60 };
            double[] radii = { inner, inner, outer, outer, inner };
            PointF[] points = new PointF[teeth * stops.Length];
            double step = 2 * Math.PI / teeth;
            int index = 0;
            for (int i = 0; i < teeth; i++)
            {
                double start = step * i - Math.PI / 2;
                for (int s = 0; s < stops.Length; s++)
                {
                    double angle = start + stops[s] * step;
                    points[index++] = new PointF((float)(52 + radii[s] * Math.Cos(angle)), (float)(48 + radii[s] * Math.Sin(angle)));
                }
            }
            cog = points;
            return cog;
        }

        // Draws the mark into a square. With a tile, it sits on a dark rounded square.
        public static void Draw(Graphics g, Rectangle bounds, bool tile)
        {
            SmoothingMode oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float k = bounds.Width / 96f;
            Color lime = CassetteMotionTheme.Accent;

            if (tile)
            {
                using (GraphicsPath path = Rounded(new RectangleF(bounds.X + k, bounds.Y + k, 94 * k, 94 * k), 22 * k))
                using (SolidBrush fill = new SolidBrush(Ink))
                using (Pen edge = new Pen(Line, Math.Max(1f, k)))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(edge, path);
                }
            }

            PointF[] scaled = new PointF[Cog().Length];
            for (int i = 0; i < scaled.Length; i++)
            {
                float x = (cog[i].X - 52) * 0.78f + 58;
                float y = (cog[i].Y - 48) * 0.78f + 48;
                scaled[i] = new PointF(bounds.X + x * k, bounds.Y + y * k);
            }
            using (Pen teethPen = new Pen(lime, 4.7f * k))
            {
                teethPen.LineJoin = LineJoin.Round;
                g.DrawPolygon(teethPen, scaled);
            }
            using (Pen ring = new Pen(lime, 5f * k))
                g.DrawEllipse(ring, bounds.X + 46 * k, bounds.Y + 36 * k, 24 * k, 24 * k);
            using (SolidBrush dot = new SolidBrush(lime))
                g.FillEllipse(dot, bounds.X + 55 * k, bounds.Y + 45 * k, 6 * k, 6 * k);

            int[] ys = { 38, 50, 62 };
            int[] lengths = { 12, 8, 12 };
            int[] alphas = { 255, 153, 90 };
            for (int i = 0; i < 3; i++)
            {
                using (Pen trail = new Pen(Color.FromArgb(alphas[i], lime), 5f * k))
                {
                    trail.StartCap = LineCap.Round;
                    trail.EndCap = LineCap.Round;
                    g.DrawLine(trail, bounds.X + 8 * k, bounds.Y + ys[i] * k, bounds.X + (8 + lengths[i]) * k, bounds.Y + ys[i] * k);
                }
            }
            g.SmoothingMode = oldMode;
        }

        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Brand mark and wordmark for the left end of the menu bar.
    internal sealed class BrandWordmarkItem : ToolStripItem
    {
        public BrandWordmarkItem()
        {
            AutoSize = false;
            Size = new Size(212, 30);
            Margin = new Padding(0, 0, 10, 0);
            Enabled = false;
            Name = "cassetteBrandWordmark";
        }

        public override Size GetPreferredSize(Size constrainingSize)
        {
            return Size;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int mark = Math.Min(Height - 4, 26);
            BrandMark.Draw(g, new Rectangle(4, (Height - mark) / 2, mark, mark), true);

            using (Font name = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold))
            using (Font tag = new Font("Consolas", 7.5F, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, "Cassette", name, new Point(mark + 12, (Height - name.Height) / 2), Color.FromArgb(228, 236, 231), TextFormatFlags.NoPadding);
                int nameWidth = TextRenderer.MeasureText(g, "Cassette", name, new Size(200, 30), TextFormatFlags.NoPadding).Width;
                TextRenderer.DrawText(g, "MOTION PRO", tag, new Point(mark + 12 + nameWidth + 6, (Height - tag.Height) / 2 + 1), CassetteMotionTheme.Accent, TextFormatFlags.NoPadding);
            }
        }
    }

    // One stage in the left navigation rail: a line icon over a short label.
    internal sealed class RailButton : Button
    {
        public enum Glyph { FitDay, Client, Video, Measure, Report }

        private bool active;
        private bool hover;

        public Glyph Icon { get; set; }

        public bool Active
        {
            get { return active; }
            set
            {
                if (active == value)
                    return;
                active = value;
                Invalidate();
            }
        }

        public RailButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(68, 62);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override bool ShowFocusCues { get { return false; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color rail = Color.FromArgb(15, 21, 18);
            g.Clear(rail);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle body = new Rectangle(0, 0, Width - 1, Height - 1);
            if (active || hover)
            {
                using (GraphicsPath path = BrandMark.Rounded(body, 10))
                {
                    using (SolidBrush fill = new SolidBrush(active ? Color.FromArgb(27, 38, 33) : Color.FromArgb(21, 30, 26)))
                        g.FillPath(fill, path);
                    if (active)
                        using (Pen edge = new Pen(Color.FromArgb(42, 56, 50)))
                            g.DrawPath(edge, path);
                }
            }

            Color ink = active ? CassetteMotionTheme.Accent : (hover ? Color.FromArgb(206, 218, 211) : Color.FromArgb(143, 161, 152));
            float k = 1f;
            float left = (Width - 24 * k) / 2f;
            float top = 9;
            using (Pen pen = new Pen(ink, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                DrawGlyph(g, pen, Icon, left, top);
            }

            using (Font font = new Font("Segoe UI", 8.25F, active ? FontStyle.Bold : FontStyle.Regular))
            {
                Rectangle textBounds = new Rectangle(0, 38, Width, Height - 40);
                TextRenderer.DrawText(g, Text, font, textBounds, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }
        }

        private static void DrawGlyph(Graphics g, Pen pen, Glyph glyph, float x, float y)
        {
            switch (glyph)
            {
                case Glyph.FitDay:
                    using (GraphicsPath box = BrandMark.Rounded(new RectangleF(x + 4, y + 5, 16, 15), 2)) g.DrawPath(pen, box);
                    g.DrawLine(pen, x + 4, y + 10, x + 20, y + 10);
                    g.DrawLine(pen, x + 9, y + 3, x + 9, y + 7);
                    g.DrawLine(pen, x + 15, y + 3, x + 15, y + 7);
                    break;
                case Glyph.Client:
                    g.DrawEllipse(pen, x + 8, y + 4, 8, 8);
                    g.DrawBezier(pen, x + 4, y + 20, x + 5, y + 16, x + 8, y + 14, x + 12, y + 14);
                    g.DrawBezier(pen, x + 12, y + 14, x + 16, y + 14, x + 19, y + 16, x + 20, y + 20);
                    break;
                case Glyph.Video:
                    using (GraphicsPath screen = BrandMark.Rounded(new RectangleF(x + 3, y + 5, 18, 14), 2)) g.DrawPath(pen, screen);
                    g.DrawPolygon(pen, new[] { new PointF(x + 10, y + 9.5f), new PointF(x + 15, y + 12), new PointF(x + 10, y + 14.5f) });
                    break;
                case Glyph.Measure:
                    g.DrawLine(pen, x + 4, y + 18, x + 18, y + 4);
                    g.DrawEllipse(pen, x + 3, y + 15, 4, 4);
                    g.DrawEllipse(pen, x + 15, y + 3, 4, 4);
                    break;
                default:
                    g.DrawLines(pen, new[] { new PointF(x + 6, y + 21), new PointF(x + 6, y + 3), new PointF(x + 14, y + 3), new PointF(x + 18, y + 7), new PointF(x + 18, y + 21), new PointF(x + 6, y + 21) });
                    g.DrawLines(pen, new[] { new PointF(x + 14, y + 3), new PointF(x + 14, y + 7), new PointF(x + 18, y + 7) });
                    g.DrawLine(pen, x + 9, y + 12, x + 15, y + 12);
                    g.DrawLine(pen, x + 9, y + 16, x + 15, y + 16);
                    break;
            }
        }
    }
}
