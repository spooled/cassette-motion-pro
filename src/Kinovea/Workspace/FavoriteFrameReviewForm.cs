/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CassetteMotionPro.Workspace
{
    public sealed class FavoriteFrameReviewForm : Form
    {
        private readonly string[] framePaths;
        private readonly string side;
        private readonly PictureBox preview = new PictureBox();
        private readonly Label frameStatus = new Label();
        private readonly Label shortcutHint = new Label();
        private readonly Button favoriteButton = new Button();
        private readonly Button useButton = new Button();
        private readonly ListBox favorites = new ListBox();
        private readonly HashSet<string> favoritePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int frameIndex;

        public string SelectedFramePath { get; private set; }

        public FavoriteFrameReviewForm(string[] framePaths, string side)
        {
            if (framePaths == null || framePaths.Length == 0)
                throw new ArgumentException("At least one saved frame is required.");
            this.framePaths = framePaths;
            this.side = side;

            Text = "Cassette Motion Pro - Favorite Frame Review";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(240, 243, 241);
            ClientSize = new Size(1220, 780);
            MinimumSize = new Size(940, 640);
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;
            KeyDown += FavoriteFrameReviewForm_KeyDown;
            FormClosed += delegate { if (preview.Image != null) preview.Image.Dispose(); };
            BuildInterface();
            ShowFrame(0);
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.FromArgb(20, 27, 24);
            Label title = new Label();
            title.Text = "Favorite Frame Review · " + side;
            title.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(22, 12);
            Label intro = new Label();
            intro.Text = "Review saved Video Studio frames quickly, mark several favorites, then choose the best report frame.";
            intro.ForeColor = Color.FromArgb(205, 216, 210);
            intro.AutoSize = true;
            intro.Location = new Point(24, 51);
            header.Controls.Add(title);
            header.Controls.Add(intro);
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            Panel previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.Padding = new Padding(14);
            previewPanel.BackColor = Color.FromArgb(31, 36, 34);
            preview.Dock = DockStyle.Fill;
            preview.SizeMode = PictureBoxSizeMode.Zoom;
            preview.BackColor = Color.Black;
            previewPanel.Controls.Add(preview);
            root.Controls.Add(previewPanel, 0, 1);

            Panel favoritePanel = new Panel();
            favoritePanel.Dock = DockStyle.Fill;
            favoritePanel.Padding = new Padding(14);
            favoritePanel.BackColor = Color.White;
            Label favoritesTitle = new Label();
            favoritesTitle.Text = "FAVORITES";
            favoritesTitle.Dock = DockStyle.Top;
            favoritesTitle.Height = 30;
            favoritesTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            favorites.Dock = DockStyle.Fill;
            favorites.HorizontalScrollbar = true;
            favorites.DoubleClick += delegate { UseSelectedFavorite(); };
            Label favoriteHelp = new Label();
            favoriteHelp.Text = "Double-click a favorite to use it. Favorites stay available while this review window is open.";
            favoriteHelp.Dock = DockStyle.Bottom;
            favoriteHelp.Height = 58;
            favoriteHelp.ForeColor = Color.FromArgb(92, 104, 98);
            favoritePanel.Controls.Add(favorites);
            favoritePanel.Controls.Add(favoriteHelp);
            favoritePanel.Controls.Add(favoritesTitle);
            root.Controls.Add(favoritePanel, 1, 1);

            TableLayoutPanel controls = new TableLayoutPanel();
            controls.Dock = DockStyle.Fill;
            controls.Padding = new Padding(14, 8, 14, 8);
            controls.ColumnCount = 6;
            controls.RowCount = 2;
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            controls.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
            controls.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Button previous = MakeButton("← Previous", false);
            previous.Click += delegate { ShowFrame(frameIndex - 1); };
            Button next = MakeButton("Next →", false);
            next.Click += delegate { ShowFrame(frameIndex + 1); };
            favoriteButton.Text = "☆ Add Favorite";
            StyleButton(favoriteButton, false);
            favoriteButton.Click += delegate { ToggleFavorite(); };
            useButton.Text = "Use as " + side + " Image";
            StyleButton(useButton, true);
            useButton.Click += delegate { SelectCurrent(); };
            Button cancel = MakeButton("Cancel", false);
            cancel.Click += delegate { Close(); };

            frameStatus.Dock = DockStyle.Fill;
            frameStatus.TextAlign = ContentAlignment.MiddleLeft;
            frameStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            shortcutHint.Dock = DockStyle.Fill;
            shortcutHint.Text = "Keyboard: ←/→ review · Space favorite · Enter use frame · Home/End jump";
            shortcutHint.ForeColor = Color.FromArgb(92, 104, 98);

            controls.Controls.Add(previous, 0, 0);
            controls.Controls.Add(next, 1, 0);
            controls.Controls.Add(favoriteButton, 2, 0);
            controls.Controls.Add(useButton, 3, 0);
            controls.Controls.Add(frameStatus, 4, 0);
            controls.Controls.Add(cancel, 5, 0);
            controls.Controls.Add(shortcutHint, 0, 1);
            controls.SetColumnSpan(shortcutHint, 6);
            root.Controls.Add(controls, 0, 2);
            root.SetColumnSpan(controls, 2);
            Controls.Add(root);
        }

        private static Button MakeButton(string text, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            StyleButton(button, primary);
            return button;
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(4);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(142, 182, 0) : Color.FromArgb(188, 198, 193);
            button.BackColor = primary ? Color.FromArgb(184, 228, 35) : Color.White;
            button.ForeColor = Color.FromArgb(26, 34, 31);
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        }

        private void ShowFrame(int index)
        {
            if (index < 0) index = 0;
            if (index >= framePaths.Length) index = framePaths.Length - 1;
            frameIndex = index;
            Image nextImage = LoadUnlocked(framePaths[frameIndex]);
            Image oldImage = preview.Image;
            preview.Image = nextImage;
            if (oldImage != null) oldImage.Dispose();
            UpdateStatus();
        }

        private static Image LoadUnlocked(string path)
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image source = Image.FromStream(stream))
                return new Bitmap(source);
        }

        private void ToggleFavorite()
        {
            string path = framePaths[frameIndex];
            if (favoritePaths.Contains(path))
            {
                favoritePaths.Remove(path);
                for (int i = favorites.Items.Count - 1; i >= 0; i--)
                {
                    FavoriteItem item = favorites.Items[i] as FavoriteItem;
                    if (item != null && string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase))
                        favorites.Items.RemoveAt(i);
                }
            }
            else
            {
                favoritePaths.Add(path);
                favorites.Items.Add(new FavoriteItem(path));
            }
            UpdateStatus();
        }

        private void SelectCurrent()
        {
            SelectedFramePath = framePaths[frameIndex];
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UseSelectedFavorite()
        {
            FavoriteItem item = favorites.SelectedItem as FavoriteItem;
            if (item == null)
                return;
            SelectedFramePath = item.Path;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateStatus()
        {
            string path = framePaths[frameIndex];
            bool isFavorite = favoritePaths.Contains(path);
            frameStatus.Text = "Frame " + (frameIndex + 1) + " of " + framePaths.Length + " · " + Path.GetFileName(path) +
                " · " + favoritePaths.Count + " favorite" + (favoritePaths.Count == 1 ? "" : "s");
            favoriteButton.Text = isFavorite ? "★ Remove Favorite" : "☆ Add Favorite";
            favoriteButton.BackColor = isFavorite ? Color.FromArgb(255, 235, 160) : Color.White;
        }

        private void FavoriteFrameReviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) ShowFrame(frameIndex - 1);
            else if (e.KeyCode == Keys.Right) ShowFrame(frameIndex + 1);
            else if (e.KeyCode == Keys.Home) ShowFrame(0);
            else if (e.KeyCode == Keys.End) ShowFrame(framePaths.Length - 1);
            else if (e.KeyCode == Keys.Space) ToggleFavorite();
            else if (e.KeyCode == Keys.Enter) SelectCurrent();
            else return;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private sealed class FavoriteItem
        {
            public string Path { get; private set; }
            public FavoriteItem(string path) { Path = path; }
            public override string ToString() { return System.IO.Path.GetFileName(Path); }
        }
    }
}
