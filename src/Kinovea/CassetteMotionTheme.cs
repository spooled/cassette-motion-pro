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
        public static readonly Color Canvas = Color.FromArgb(239, 243, 240);
        public static readonly Color Surface = Color.FromArgb(252, 253, 252);
        public static readonly Color SurfaceSoft = Color.FromArgb(228, 235, 230);
        public static readonly Color SurfaceRaised = Color.White;
        public static readonly Color Header = Color.FromArgb(13, 20, 18);
        public static readonly Color HeaderSoft = Color.FromArgb(28, 40, 36);
        public static readonly Color Rail = Color.FromArgb(17, 27, 24);
        public static readonly Color RailSelected = Color.FromArgb(45, 62, 55);
        public static readonly Color Ink = Color.FromArgb(19, 31, 27);
        public static readonly Color Muted = Color.FromArgb(92, 105, 98);
        public static readonly Color Border = Color.FromArgb(198, 210, 202);
        public static readonly Color Accent = Color.FromArgb(191, 244, 74);
        public static readonly Color AccentSoft = Color.FromArgb(224, 250, 169);
        public static readonly Color AccentStrong = Color.FromArgb(126, 190, 23);
        public static readonly Color Success = Color.FromArgb(42, 142, 78);
        public static readonly Color Warning = Color.FromArgb(190, 119, 29);

        public static void ApplyForm(Form form)
        {
            form.Font = new Font("Segoe UI", 9.25F);
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
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.MinimumSize = new Size(82, 38);
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

        public static void StyleApplicationMenu(MenuStrip menu)
        {
            if (menu == null)
                return;
            menu.BackColor = Header;
            menu.ForeColor = Color.White;
            menu.Renderer = new ToolStripProfessionalRenderer(new CassetteMotionColorTable());
            menu.Padding = new Padding(8, 2, 4, 2);
            menu.AutoSize = false;
            menu.Height = 31;
            StyleToolStripItems(menu.Items);
        }

        public static void StyleApplicationToolbar(ToolStrip toolbar)
        {
            if (toolbar == null)
                return;
            toolbar.CanOverflow = true;
            toolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            toolbar.BackColor = HeaderSoft;
            toolbar.ForeColor = Color.White;
            toolbar.Renderer = new ToolStripProfessionalRenderer(new CassetteMotionColorTable());
            toolbar.GripStyle = ToolStripGripStyle.Hidden;
            toolbar.Padding = new Padding(10, 4, 8, 4);
            toolbar.AutoSize = false;
            toolbar.Height = 43;
            toolbar.ImageScalingSize = new Size(22, 22);
            StyleToolStripItems(toolbar.Items);
        }

        public static void StyleApplicationStatus(ToolStrip status)
        {
            if (status == null)
                return;
            status.BackColor = Header;
            status.ForeColor = Color.FromArgb(211, 222, 216);
            status.Renderer = new ToolStripProfessionalRenderer(new CassetteMotionColorTable());
            status.GripStyle = ToolStripGripStyle.Hidden;
            StyleToolStripItems(status.Items);
        }

        private static void StyleToolStripItems(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (!(item is ToolStripSeparator))
                    item.ForeColor = Color.White;
                ToolStripDropDownItem dropDown = item as ToolStripDropDownItem;
                if (dropDown != null)
                {
                    dropDown.DropDown.BackColor = HeaderSoft;
                    dropDown.DropDown.ForeColor = Color.White;
                    dropDown.DropDown.Renderer = new ToolStripProfessionalRenderer(new CassetteMotionColorTable());
                    StyleToolStripItems(dropDown.DropDownItems);
                }
            }
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

        private sealed class CassetteMotionColorTable : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin { get { return Header; } }
            public override Color MenuStripGradientEnd { get { return Header; } }
            public override Color ToolStripGradientBegin { get { return HeaderSoft; } }
            public override Color ToolStripGradientMiddle { get { return HeaderSoft; } }
            public override Color ToolStripGradientEnd { get { return HeaderSoft; } }
            public override Color StatusStripGradientBegin { get { return Header; } }
            public override Color StatusStripGradientEnd { get { return Header; } }
            public override Color MenuItemSelected { get { return RailSelected; } }
            public override Color MenuItemSelectedGradientBegin { get { return RailSelected; } }
            public override Color MenuItemSelectedGradientEnd { get { return RailSelected; } }
            public override Color MenuItemPressedGradientBegin { get { return HeaderSoft; } }
            public override Color MenuItemPressedGradientMiddle { get { return HeaderSoft; } }
            public override Color MenuItemPressedGradientEnd { get { return HeaderSoft; } }
            public override Color ToolStripDropDownBackground { get { return HeaderSoft; } }
            public override Color ImageMarginGradientBegin { get { return HeaderSoft; } }
            public override Color ImageMarginGradientMiddle { get { return HeaderSoft; } }
            public override Color ImageMarginGradientEnd { get { return HeaderSoft; } }
            public override Color ButtonSelectedGradientBegin { get { return RailSelected; } }
            public override Color ButtonSelectedGradientEnd { get { return RailSelected; } }
            public override Color ButtonPressedGradientBegin { get { return AccentStrong; } }
            public override Color ButtonPressedGradientEnd { get { return AccentStrong; } }
            public override Color MenuBorder { get { return Border; } }
            public override Color MenuItemBorder { get { return Accent; } }
            public override Color SeparatorDark { get { return RailSelected; } }
            public override Color SeparatorLight { get { return RailSelected; } }
        }
    }
}
