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
        public static readonly Color Canvas = Color.FromArgb(242, 245, 241);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceSoft = Color.FromArgb(234, 239, 233);
        public static readonly Color Header = Color.FromArgb(9, 15, 13);
        public static readonly Color Ink = Color.FromArgb(20, 28, 24);
        public static readonly Color Muted = Color.FromArgb(98, 113, 104);
        public static readonly Color Border = Color.FromArgb(209, 218, 211);
        public static readonly Color Accent = Color.FromArgb(184, 243, 74);
        public static readonly Color AccentStrong = Color.FromArgb(139, 214, 0);
        public static readonly Color Success = Color.FromArgb(50, 139, 78);
        public static readonly Color Warning = Color.FromArgb(178, 111, 32);

        public static void ApplyForm(Form form)
        {
            form.Font = new Font("Segoe UI", 9.25F);
            form.BackColor = Canvas;
            form.ForeColor = Ink;
        }

        public static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? AccentStrong : Border;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(198, 250, 96) : SurfaceSoft;
            button.FlatAppearance.MouseDownBackColor = primary ? AccentStrong : Color.FromArgb(221, 228, 222);
            button.BackColor = primary ? Accent : Surface;
            button.ForeColor = Ink;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        public static void StyleListView(ListView list)
        {
            list.BackColor = Surface;
            list.ForeColor = Ink;
            list.BorderStyle = BorderStyle.None;
            list.Font = new Font("Segoe UI", 9.25F);
        }

        public static void StyleTextInput(Control input)
        {
            input.BackColor = Surface;
            input.ForeColor = Ink;
            input.Font = new Font("Segoe UI", 9.25F);
        }

        public static void StyleTabs(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Normal;
            tabs.ItemSize = new Size(0, 38);
            tabs.DrawItem += DrawTab;
        }

        private static void DrawTab(object sender, DrawItemEventArgs e)
        {
            TabControl tabs = sender as TabControl;
            if (tabs == null || e.Index < 0 || e.Index >= tabs.TabPages.Count)
                return;

            bool selected = e.Index == tabs.SelectedIndex;
            Rectangle bounds = e.Bounds;
            Color background = selected ? Header : SurfaceSoft;
            Color foreground = selected ? Color.White : Muted;
            using (SolidBrush backgroundBrush = new SolidBrush(background))
                e.Graphics.FillRectangle(backgroundBrush, bounds);
            if (selected)
            {
                using (SolidBrush accentBrush = new SolidBrush(Accent))
                    e.Graphics.FillRectangle(accentBrush, bounds.Left, bounds.Bottom - 4, bounds.Width, 4);
            }
            using (StringFormat format = new StringFormat())
            using (SolidBrush textBrush = new SolidBrush(foreground))
            using (Font font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(tabs.TabPages[e.Index].Text, font, textBrush, bounds, format);
            }
        }
    }
}
