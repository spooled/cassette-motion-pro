/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System.Drawing;
using System.Windows.Forms;

namespace CassetteMotionPro
{
    internal static class CassetteMotionTheme
    {
        public static readonly Color Canvas = Color.FromArgb(244, 247, 243);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceSoft = Color.FromArgb(235, 241, 235);
        public static readonly Color SurfaceRaised = Color.FromArgb(249, 251, 248);
        public static readonly Color Header = Color.FromArgb(12, 29, 24);
        public static readonly Color HeaderSoft = Color.FromArgb(24, 48, 40);
        public static readonly Color Ink = Color.FromArgb(18, 38, 31);
        public static readonly Color Muted = Color.FromArgb(91, 108, 99);
        public static readonly Color Border = Color.FromArgb(205, 218, 208);
        public static readonly Color Accent = Color.FromArgb(190, 242, 82);
        public static readonly Color AccentStrong = Color.FromArgb(137, 203, 28);
        public static readonly Color Success = Color.FromArgb(50, 139, 78);
        public static readonly Color Warning = Color.FromArgb(178, 111, 32);

        public static void ApplyForm(Form form)
        {
            form.Font = new Font("Segoe UI", 9.5F);
            form.BackColor = Canvas;
            form.ForeColor = Ink;
        }

        public static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = primary ? AccentStrong : Border;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(204, 249, 108) : SurfaceSoft;
            button.FlatAppearance.MouseDownBackColor = primary ? AccentStrong : Color.FromArgb(221, 228, 222);
            button.BackColor = primary ? Accent : Surface;
            button.ForeColor = Ink;
            button.Font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold);
            button.MinimumSize = new Size(82, 34);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleListView(ListView list)
        {
            list.BackColor = Surface;
            list.ForeColor = Ink;
            list.BorderStyle = BorderStyle.None;
            list.Font = new Font("Segoe UI", 9.5F);
        }

        public static void StyleTextInput(Control input)
        {
            input.BackColor = Surface;
            input.ForeColor = Ink;
            input.Font = new Font("Segoe UI", 9.5F);
        }

        public static void StyleTabs(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Normal;
            tabs.ItemSize = new Size(0, 40);
            tabs.BackColor = Canvas;
            tabs.DrawItem += DrawTab;
        }

        private static void DrawTab(object sender, DrawItemEventArgs e)
        {
            TabControl tabs = sender as TabControl;
            if (tabs == null || e.Index < 0 || e.Index >= tabs.TabPages.Count)
                return;

            bool selected = e.Index == tabs.SelectedIndex;
            Rectangle bounds = e.Bounds;
            Color background = selected ? Surface : SurfaceSoft;
            Color foreground = selected ? Ink : Muted;
            using (SolidBrush backgroundBrush = new SolidBrush(background))
                e.Graphics.FillRectangle(backgroundBrush, bounds);
            if (selected)
            {
                using (SolidBrush accentBrush = new SolidBrush(Accent))
                    e.Graphics.FillRectangle(accentBrush, bounds.Left + 8, bounds.Bottom - 4, bounds.Width - 16, 4);
            }
            using (StringFormat format = new StringFormat())
            using (SolidBrush textBrush = new SolidBrush(foreground))
            using (Font font = new Font("Segoe UI Semibold", 9.25F, FontStyle.Bold))
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(tabs.TabPages[e.Index].Text, font, textBrush, bounds, format);
            }
        }
    }
}
