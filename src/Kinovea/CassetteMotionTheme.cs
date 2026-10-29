/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
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
            StyleButtonColors(button, primary);
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.MinimumSize = new Size(82, 38);
            ApplyButtonPainting(button);
        }

        // Colours and cursor only: no font, size or layout changes, so it is safe on fixed-size dialogs.
        private static void StyleButtonColors(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = primary ? AccentStrong : Border;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(204, 249, 108) : SurfaceSoft;
            button.FlatAppearance.MouseDownBackColor = primary ? AccentStrong : Color.FromArgb(221, 228, 222);
            button.BackColor = primary ? Accent : Surface;
            button.ForeColor = Ink;
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

        // ---------------------------------------------------------------------------------
        // Rounded, state-aware buttons. Painted over the flat button, so size, font and layout
        // are untouched. Fill, border and text colours come from the button itself, which keeps
        // any colour a screen sets deliberately (selected stage, approve/danger buttons).
        // ---------------------------------------------------------------------------------
        private const int ButtonRadius = 8;

        private sealed class ButtonLook
        {
            public bool Hover;
            public bool Down;
            public bool KeyboardFocus;
        }

        private static readonly ConditionalWeakTable<Button, ButtonLook> ButtonLooks = new ConditionalWeakTable<Button, ButtonLook>();

        public static void ApplyButtonPainting(Button button)
        {
            if (button == null || button.Image != null || button.BackgroundImage != null)
                return;

            ButtonLook existing;
            if (ButtonLooks.TryGetValue(button, out existing))
                return;

            ButtonLook look = new ButtonLook();
            ButtonLooks.Add(button, look);
            button.FlatStyle = FlatStyle.Flat;
            button.Paint += PaintButton;
            button.MouseEnter += delegate { look.Hover = true; button.Invalidate(); };
            button.MouseLeave += delegate { look.Hover = false; look.Down = false; button.Invalidate(); };
            button.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    look.Down = true;
                    button.Invalidate();
                }
            };
            button.MouseUp += delegate { look.Down = false; button.Invalidate(); };
            // Show the focus ring for keyboard navigation only, not after a mouse click.
            button.GotFocus += delegate
            {
                look.KeyboardFocus = (Control.MouseButtons & MouseButtons.Left) == 0;
                button.Invalidate();
            };
            button.LostFocus += delegate { look.KeyboardFocus = false; button.Invalidate(); };
        }

        private static void PaintButton(object sender, PaintEventArgs e)
        {
            Button button = sender as Button;
            ButtonLook look;
            if (button == null || !ButtonLooks.TryGetValue(button, out look))
                return;

            Rectangle client = button.ClientRectangle;
            if (client.Width < 6 || client.Height < 6)
                return;

            Color surround = ResolveBackground(button.Parent);
            Color back = button.BackColor;
            if (back.A < 255)
                back = surround;

            Color fill = back;
            if (!button.Enabled)
                fill = Mix(back, surround, 0.5);
            else if (look.Down)
                fill = PickState(button.FlatAppearance.MouseDownBackColor, ControlPaint.Dark(back, 0.08f));
            else if (look.Hover)
                fill = PickState(button.FlatAppearance.MouseOverBackColor, ControlPaint.Light(back, 0.35f));

            Color border = Color.Empty;
            if (button.FlatAppearance.BorderSize > 0)
            {
                border = button.FlatAppearance.BorderColor.IsEmpty ? Border : button.FlatAppearance.BorderColor;
                if (button.Enabled && look.Hover)
                    border = AccentStrong;
                else if (!button.Enabled)
                    border = Mix(border, surround, 0.5);
            }

            Color text = button.Enabled ? button.ForeColor : Color.FromArgb(146, 157, 151);

            Graphics g = e.Graphics;
            using (SolidBrush surroundBrush = new SolidBrush(surround))
                g.FillRectangle(surroundBrush, client);

            SmoothingMode previousSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle body = new Rectangle(0, 0, client.Width - 1, client.Height - 1);
            int radius = Math.Max(2, Math.Min(ButtonRadius, Math.Min(body.Width, body.Height) / 2));
            using (GraphicsPath path = RoundedRectangle(body, radius))
            {
                using (SolidBrush fillBrush = new SolidBrush(fill))
                    g.FillPath(fillBrush, path);
                if (!border.IsEmpty)
                    using (Pen borderPen = new Pen(border, 1F))
                        g.DrawPath(borderPen, path);
                if (button.Enabled && look.KeyboardFocus)
                {
                    Rectangle ring = Rectangle.Inflate(body, -3, -3);
                    if (ring.Width > 6 && ring.Height > 6)
                        using (GraphicsPath ringPath = RoundedRectangle(ring, Math.Max(2, radius - 3)))
                        using (Pen ringPen = new Pen(Color.FromArgb(170, Ink), 1F))
                        {
                            ringPen.DashStyle = DashStyle.Dot;
                            g.DrawPath(ringPen, ringPath);
                        }
                }
            }
            g.SmoothingMode = previousSmoothing;

            Padding padding = button.Padding;
            Rectangle textBounds = new Rectangle(
                client.Left + 8 + padding.Left,
                client.Top + 2 + padding.Top,
                client.Width - 16 - padding.Horizontal,
                client.Height - 4 - padding.Vertical);
            if (textBounds.Width > 0 && textBounds.Height > 0 && !string.IsNullOrEmpty(button.Text))
            {
                TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.HidePrefix | TextFormatFlags.NoPadding | AlignmentFlags(button.TextAlign);
                TextRenderer.DrawText(g, button.Text, button.Font, textBounds, text, flags);
            }
        }

        private static Color ResolveBackground(Control parent)
        {
            while (parent != null)
            {
                Color color = parent.BackColor;
                if (color.A == 255)
                    return color;
                parent = parent.Parent;
            }
            return Canvas;
        }

        private static Color PickState(Color requested, Color fallback)
        {
            return requested.IsEmpty ? fallback : requested;
        }

        private static Color Mix(Color from, Color to, double amount)
        {
            int r = (int)Math.Round(from.R + (to.R - from.R) * amount);
            int g = (int)Math.Round(from.G + (to.G - from.G) * amount);
            int b = (int)Math.Round(from.B + (to.B - from.B) * amount);
            return Color.FromArgb(Clamp(r), Clamp(g), Clamp(b));
        }

        private static int Clamp(int value)
        {
            return value < 0 ? 0 : value > 255 ? 255 : value;
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private static TextFormatFlags AlignmentFlags(ContentAlignment alignment)
        {
            TextFormatFlags flags = 0;
            switch (alignment)
            {
                case ContentAlignment.TopLeft: flags = TextFormatFlags.Left | TextFormatFlags.Top; break;
                case ContentAlignment.TopCenter: flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.Top; break;
                case ContentAlignment.TopRight: flags = TextFormatFlags.Right | TextFormatFlags.Top; break;
                case ContentAlignment.MiddleLeft: flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter; break;
                case ContentAlignment.MiddleRight: flags = TextFormatFlags.Right | TextFormatFlags.VerticalCenter; break;
                case ContentAlignment.BottomLeft: flags = TextFormatFlags.Left | TextFormatFlags.Bottom; break;
                case ContentAlignment.BottomCenter: flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom; break;
                case ContentAlignment.BottomRight: flags = TextFormatFlags.Right | TextFormatFlags.Bottom; break;
                default: flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter; break;
            }
            return flags;
        }

        // ---------------------------------------------------------------------------------
        // Dialog theming. One line in a dialog's constructor, Attach(this), brings a plain
        // default-grey WinForms dialog in line with the rest of the app. It changes colours and
        // button painting only: no fonts, sizes or layout, so fixed-size dialogs cannot clip.
        // It is idempotent and leaves any colour a dialog set explicitly alone.
        // ---------------------------------------------------------------------------------
        public static void Attach(Form form)
        {
            if (form == null)
                return;
            form.Load += DialogLoad;
        }

        private static void DialogLoad(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form == null)
                return;
            form.Load -= DialogLoad;
            form.Shown += DialogShown;
            ApplyToDialog(form);
        }

        private static void DialogShown(object sender, EventArgs e)
        {
            Form form = sender as Form;
            if (form == null)
                return;
            form.Shown -= DialogShown;
            // Catches controls a dialog added after Load.
            ApplyToDialog(form);
        }

        private static void ApplyToDialog(Form form)
        {
            if (form.BackColor == SystemColors.Control)
                form.BackColor = Canvas;
            if (form.ForeColor == SystemColors.ControlText)
                form.ForeColor = Ink;
            ThemeDialogControls(form, form);
        }

        private static void ThemeDialogControls(Form form, Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                Button button = control as Button;
                if (button != null)
                {
                    bool defaultLook = button.FlatStyle == FlatStyle.Standard && button.UseVisualStyleBackColor &&
                        button.Image == null && button.BackgroundImage == null;
                    if (defaultLook)
                    {
                        bool primary = object.ReferenceEquals(form.AcceptButton, button) ||
                            button.DialogResult == DialogResult.OK || button.DialogResult == DialogResult.Yes;
                        StyleButtonColors(button, primary);
                    }
                    ApplyButtonPainting(button);
                }

                ListView list = control as ListView;
                if (list != null)
                {
                    if (list.BackColor == SystemColors.Window)
                        list.BackColor = Surface;
                    if (list.ForeColor == SystemColors.WindowText)
                        list.ForeColor = Ink;
                }

                if (control.HasChildren)
                    ThemeDialogControls(form, control);
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
