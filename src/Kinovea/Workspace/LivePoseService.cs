using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using Kinovea.ScreenManager;

namespace CassetteMotionPro.Workspace
{
    /// <summary>
    /// Live joint overlay for the camera view: a few times a second the current camera frame goes to the
    /// pose model on a background thread, and the latest joints and angles are drawn over the picture.
    /// It is an estimate to glance at while setting the rider up, not a saved measurement.
    /// </summary>
    internal static class LivePoseService
    {
        private const int IntervalMilliseconds = 120;
        private const int StaleMilliseconds = 1500;
        private static readonly object gate = new object();
        private static readonly Stopwatch clock = Stopwatch.StartNew();
        private static Bitmap ghost;
        private static Bitmap ghostScaled;
        private static Size ghostScaledFor;
        private static bool ghostVisible;
        public static string GhostPath { get; private set; }
        public static Func<string> GhostSource; // set by the workspace: the session's Before picture
        private static string toastText;
        public static event EventHandler GhostChanged;

        // Each camera view (left, right, ...) gets its own joints, drags, pins and smoothing.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Channel> channelByView = new System.Runtime.CompilerServices.ConditionalWeakTable<object, Channel>();
        private static readonly List<WeakReference> allChannels = new List<WeakReference>();
        private static readonly object defaultView = new object();

        private static Channel ChannelFor(object view)
        {
            if (view == null) view = defaultView;
            lock (allChannels)
            {
                Channel channel;
                if (channelByView.TryGetValue(view, out channel)) return channel;
                channel = new Channel(view);
                channelByView.Add(view, channel);
                allChannels.RemoveAll(delegate (WeakReference r) { return !r.IsAlive; });
                allChannels.Add(new WeakReference(channel));
                return channel;
            }
        }

        private static void ForEachChannel(Action<Channel> action)
        {
            List<Channel> list = new List<Channel>();
            lock (allChannels)
                foreach (WeakReference r in allChannels) { Channel c = r.Target as Channel; if (c != null) list.Add(c); }
            foreach (Channel c in list) action(c);
        }

        private static bool enabled = true;
        private static long toastUntil;
        public static event EventHandler EnabledChanged;

        public static bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value) return;
                enabled = value;
                if (!enabled) ForEachChannel(delegate (Channel c) { c.ReleaseAll(); });
                toastText = enabled ? "LIVE JOINTS \u00B7 on (Ctrl+J)" : "LIVE JOINTS \u00B7 off (Ctrl+J)";
                toastUntil = clock.ElapsedMilliseconds + 1400;
                EventHandler handler = EnabledChanged;
                if (handler != null) handler(null, EventArgs.Empty);
            }
        }

        public static bool Available { get { return !PoseEstimator.Unavailable; } }

        public static void Install()
        {
            LivePoseHook.FrameReady = delegate (object view, Bitmap frame) { ChannelFor(view).OnFrame(frame); };
            LivePoseHook.PlayerFrameReady = delegate (object view, Bitmap frame, long stamp, bool playing) { ChannelFor(view).OnPlayerFrame(frame, stamp, playing); };
            LivePoseHook.Paint = OnPaint;
            LivePoseHook.MouseDown = delegate (object view, Point l, MouseButtons b, Rectangle r) { return ChannelFor(view).OnMouseDown(l, b, r); };
            LivePoseHook.MouseMove = delegate (object view, Point l, MouseButtons b, Rectangle r) { return ChannelFor(view).OnMouseMove(l, b, r); };
            LivePoseHook.MouseUp = delegate (object view, Point l, MouseButtons b, Rectangle r) { return ChannelFor(view).OnMouseUp(l, b, r); };
            Application.AddMessageFilter(new KeyFilter());
        }

        // Ctrl+J turns the live joint overlay on or off, Ctrl+G shows or hides the ghost image, from anywhere in the app.
        private class KeyFilter : IMessageFilter
        {
            private const int WM_KEYDOWN = 0x0100;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_KEYDOWN || Control.ModifierKeys != Keys.Control) return false;
                Keys key = (Keys)(int)m.WParam;
                if (key == Keys.J) { Enabled = !Enabled; return true; }
                if (key == Keys.G) { ToggleGhost(); return true; }
                return false;
            }
        }

        public static bool HasGhost { get { return ghost != null; } }

        public static bool GhostVisible
        {
            get { return ghostVisible; }
            set
            {
                if (ghostVisible == value) return;
                ghostVisible = value;
                toastText = value ? "GHOST \u00B7 on (Ctrl+G)" : "GHOST \u00B7 hidden (Ctrl+G)";
                toastUntil = clock.ElapsedMilliseconds + 1400;
                EventHandler handler = GhostChanged;
                if (handler != null) handler(null, EventArgs.Empty);
            }
        }

        // Ctrl+G and the workspace button: show or hide the ghost; the first time, load the session's Before picture (or ask for one).
        public static void ToggleGhost()
        {
            if (ghost != null) { GhostVisible = !GhostVisible; return; }
            string path = null;
            try { Func<string> source = GhostSource; if (source != null) path = source(); } catch (Exception) { }
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = "Choose the picture to show faintly over the live view";
                    dialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*";
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    path = dialog.FileName;
                }
            }
            try { SetGhost(path); }
            catch (Exception exception)
            {
                MessageBox.Show("That picture could not be opened: " + exception.Message, "Ghost image", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Loads a saved picture (for example the Before frame) to blend over the live view.
        public static void SetGhost(string path)
        {
            Bitmap loaded = null;
            if (!string.IsNullOrEmpty(path))
                using (Bitmap raw = new Bitmap(path))
                    loaded = new Bitmap(raw); // copy so the file is not locked
            lock (gate)
            {
                if (ghost != null) ghost.Dispose();
                if (ghostScaled != null) ghostScaled.Dispose();
                ghost = loaded;
                ghostScaled = null;
                GhostPath = loaded == null ? null : path;
            }
            ghostVisible = false;
            GhostVisible = loaded != null;
        }

        private static void DrawGhost(Graphics g, Rectangle target)
        {
            lock (gate)
            {
                if (!ghostVisible || ghost == null || target.Width < 8 || target.Height < 8) return;
                if (ghostScaled == null || ghostScaledFor != target.Size)
                {
                    if (ghostScaled != null) ghostScaled.Dispose();
                    ghostScaled = new Bitmap(target.Width, target.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (Graphics sg = Graphics.FromImage(ghostScaled))
                    using (System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes())
                    {
                        float[][] matrix =
                        {
                            new float[] { 1, 0, 0, 0, 0 }, new float[] { 0, 1, 0, 0, 0 }, new float[] { 0, 0, 1, 0, 0 },
                            new float[] { 0, 0, 0, 0.42F, 0 }, new float[] { 0, 0, 0, 0, 1 }
                        };
                        attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(matrix));
                        sg.InterpolationMode = InterpolationMode.HighQualityBilinear;
                        sg.DrawImage(ghost, new Rectangle(0, 0, target.Width, target.Height), 0, 0, ghost.Width, ghost.Height, GraphicsUnit.Pixel, attributes);
                    }
                    ghostScaledFor = target.Size;
                }
                g.DrawImageUnscaled(ghostScaled, target.Location);
            }
        }

        private static void OnPaint(object view, Graphics g, Rectangle target, Size imageSize)
        {
            DrawGhost(g, target);
            if (Enabled && PoseEstimator.Unavailable)
            {
                using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
                    Label(g, font, new PointF(target.X + 8, target.Y + 22), "LIVE JOINTS unavailable: " + Shorten(PoseEstimator.LastError), true, true);
                return;
            }
            if (!Enabled)
            {
                if (clock.ElapsedMilliseconds < toastUntil)
                    using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
                        Label(g, font, new PointF(target.X + 8, target.Y + 22), toastText ?? "LIVE JOINTS \u00B7 off (Ctrl+J)", true, true);
                return;
            }
            ChannelFor(view).DrawJoints(g, target);
        }

        private static string Shorten(string text)
        {
            if (string.IsNullOrEmpty(text)) return "see pose-error.txt";
            return text.Length > 110 ? text.Substring(0, 110) + "..." : text;
        }

        private static void Label(Graphics g, Font font, PointF anchor, string text, bool toTheRight, bool pinned = false)
        {
            SizeF size = g.MeasureString(text, font);
            float x = pinned ? anchor.X : (toTheRight ? anchor.X + 14 : anchor.X - 14 - size.Width);
            float y = pinned ? anchor.Y : anchor.Y - size.Height / 2;
            using (Brush back = new SolidBrush(Color.FromArgb(190, 13, 19, 17)))
            using (Brush fore = new SolidBrush(Color.White))
            {
                g.FillRectangle(back, x - 4, y - 1, size.Width + 8, size.Height + 2);
                g.DrawString(text, font, fore, x, y);
            }
        }

        private static double Angle(PointF a, PointF b, PointF c)
        {
            double v1x = a.X - b.X, v1y = a.Y - b.Y, v2x = c.X - b.X, v2y = c.Y - b.Y;
            double len = Math.Sqrt(v1x * v1x + v1y * v1y) * Math.Sqrt(v2x * v2x + v2y * v2y);
            if (len < 1e-6) return 0;
            double cos = Math.Max(-1, Math.Min(1, (v1x * v2x + v1y * v2y) / len));
            return Math.Acos(cos) * 180.0 / Math.PI;
        }

        private sealed class Channel
        {
            private readonly object gate = new object();
            private long lastSubmit = -IntervalMilliseconds;
            private long lastResult;
            private readonly OneEuro[] filters = NewFilters();
            private Size filterSize;
            private bool filterFacing;
            private List<PointF> shown;
            private long lastPaint;
            private int busy;
            private PoseDetection latest;
            private Size latestSize;
            private readonly SegmentModel segments = new SegmentModel();
            private readonly Dictionary<int, PointF> pinned = new Dictionary<int, PointF>();
            private int dragIndex = -1;
            private readonly WeakReference viewRef;
            private bool sticky, playing;
            private long lastStamp = long.MinValue, latestStamp, currentStamp;

            public Channel(object view) { viewRef = new WeakReference(view); }

            private void RepaintView()
            {
                try
                {
                    Control c = viewRef.Target as Control;
                    if (c != null && c.IsHandleCreated && !c.IsDisposed) c.BeginInvoke(new Action(c.Invalidate));
                }
                catch (Exception) { }
            }

            private static OneEuro[] NewFilters()
            {
                OneEuro[] list = new OneEuro[16];
                for (int i = 0; i < list.Length; i++) list[i] = new OneEuro();
                return list;
            }

            public void ReleaseAll()
            {
                lock (gate) { pinned.Clear(); dragIndex = -1; }
            }

        public void OnFrame(Bitmap frame)
        {
            sticky = false;
            Submit(frame, 0);
        }

        // Video playback: one reading per frame; a paused frame keeps its joints until you move to another frame.
        public void OnPlayerFrame(Bitmap frame, long stamp, bool isPlaying)
        {
            sticky = true;
            playing = isPlaying;
            currentStamp = stamp;
            Submit(frame, stamp);
        }

        private void Submit(Bitmap frame, long stamp)
        {
            if (!Enabled || frame == null || PoseEstimator.Unavailable) return;
            long now = clock.ElapsedMilliseconds;
            if (sticky) { if (stamp == lastStamp) return; }
            else if (now - lastSubmit < IntervalMilliseconds) return;
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) return;
            lastSubmit = now;
            if (sticky)
            {
                lastStamp = stamp;
                lock (gate) { pinned.Clear(); dragIndex = -1; }
            }

            Bitmap copy;
            try { copy = (Bitmap)frame.Clone(); }
            catch (Exception) { Interlocked.Exchange(ref busy, 0); return; }

            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    PoseDetection result = PoseEstimator.Detect(copy);
                    if (result != null)
                    {
                        lock (gate)
                        {
                            segments.Constrain(result, clock.ElapsedMilliseconds);
                            FilterPoints(result, copy.Size);
                            ApplyPinned(result, copy.Size);
                            latest = result;
                            latestSize = copy.Size;
                            latestStamp = stamp;
                            lastResult = clock.ElapsedMilliseconds;
                        }
                    }
                }
                catch (Exception) { }
                finally
                {
                    copy.Dispose();
                    Interlocked.Exchange(ref busy, 0);
                    if (sticky) RepaintView();
                }
            });
        }

        // Dots the user has dragged stay where they were put until released (right-click) or the overlay is turned off.
        private void ApplyPinned(PoseDetection pose, Size size)
        {
            if (pinned.Count == 0) return;
            if (size != latestSize || (latest != null && latest.FacingRight != pose.FacingRight)) { pinned.Clear(); return; }
            foreach (KeyValuePair<int, PointF> pin in pinned)
                if (pin.Key < pose.Points.Count)
                {
                    pose.Points[pin.Key] = pin.Value;
                    pose.Scores[pin.Key] = 1F;
                }
        }

        private int HitDot(Point location, Rectangle target)
        {
            if (latest == null || latestSize.Width <= 0 || latestSize.Height <= 0 || target.Width <= 0) return -1;
            int best = -1; double bestDistance = 18 * 18;
            for (int i = 0; i < latest.Points.Count; i++)
            {
                if (i == 3) continue;
                double dx = target.X + latest.Points[i].X * target.Width / latestSize.Width - location.X;
                double dy = target.Y + latest.Points[i].Y * target.Height / latestSize.Height - location.Y;
                double distance = dx * dx + dy * dy;
                if (distance < bestDistance) { bestDistance = distance; best = i; }
            }
            return best;
        }

        public bool OnMouseDown(Point location, MouseButtons button, Rectangle target)
        {
            if (!Enabled || PoseEstimator.Unavailable) return false;
            lock (gate)
            {
                int hit = HitDot(location, target);
                if (hit < 0) return false;
                if (button == MouseButtons.Right)
                {
                    pinned.Remove(hit);
                    return true;
                }
                if (button != MouseButtons.Left) return false;
                dragIndex = hit;
                pinned[hit] = latest.Points[hit];
                return true;
            }
        }

        public bool OnMouseMove(Point location, MouseButtons button, Rectangle target)
        {
            if (dragIndex < 0) return false;
            if (button != MouseButtons.Left) { dragIndex = -1; return false; }
            lock (gate)
            {
                if (latest == null || target.Width <= 0 || target.Height <= 0) return true;
                float x = Math.Max(0, Math.Min(latestSize.Width, (location.X - target.X) * (float)latestSize.Width / target.Width));
                float y = Math.Max(0, Math.Min(latestSize.Height, (location.Y - target.Y) * (float)latestSize.Height / target.Height));
                PointF point = new PointF(x, y);
                pinned[dragIndex] = point;
                latest.Points[dragIndex] = point;
                latest.Scores[dragIndex] = 1F;
                lastResult = clock.ElapsedMilliseconds;
            }
            return true;
        }

        public bool OnMouseUp(Point location, MouseButtons button, Rectangle target)
        {
            if (dragIndex < 0) return false;
            dragIndex = -1;
            return true;
        }

        // Steadies each dot. A One Euro filter smooths hard while a joint is nearly still and lets go when it moves fast,
        // so the dots stop shaking without lagging behind the pedalling. Unsure joints are smoothed harder, and a sudden
        // large jump of an unsure joint is ignored.
        private void FilterPoints(PoseDetection pose, Size size)
        {
            long now = clock.ElapsedMilliseconds;
            if (size != filterSize || pose.FacingRight != filterFacing || now - lastResult > StaleMilliseconds || (sticky && !playing))
                foreach (OneEuro filter in filters) filter.Reset();
            filterSize = size;
            filterFacing = pose.FacingRight;

            double height = Math.Max(1, size.Height), time = now / 1000.0;
            for (int i = 0; i < pose.Points.Count && i * 2 + 1 < filters.Length; i++)
            {
                OneEuro fx = filters[i * 2], fy = filters[i * 2 + 1];
                PointF p = pose.Points[i];
                float score = i == 3 ? 0.6F : pose.Scores[i]; // the toe is a guess built from the ankle
                if (fx.HasValue && score < 0.4F)
                {
                    double jump = Math.Sqrt(Math.Pow(p.X - fx.Last * height, 2) + Math.Pow(p.Y - fy.Last * height, 2)) / height;
                    if (jump > 0.12) p = new PointF((float)(fx.Last * height), (float)(fy.Last * height));
                }
                double strength = score < 0.4F ? 0.5 : 1.0;
                pose.Points[i] = new PointF((float)(fx.Filter(p.X / height, time, strength) * height), (float)(fy.Filter(p.Y / height, time, strength) * height));
            }
        }

        // Each screen refresh moves the drawn dots part of the way to the newest reading, so they glide between detections instead of stepping.
        private List<PointF> EaseShown(PoseDetection pose)
        {
            long now = clock.ElapsedMilliseconds;
            double dt = Math.Min(500, now - lastPaint);
            lastPaint = now;
            if (shown == null || shown.Count != pose.Points.Count || dt >= 500 || (sticky && !playing))
            {
                shown = new List<PointF>(pose.Points);
                return shown;
            }
            float alpha = (float)(1 - Math.Exp(-dt / 70.0));
            for (int i = 0; i < shown.Count; i++)
            {
                PointF target = pose.Points[i];
                if (pinned.ContainsKey(i)) { shown[i] = target; continue; }
                PointF current = shown[i];
                shown[i] = new PointF(current.X + (target.X - current.X) * alpha, current.Y + (target.Y - current.Y) * alpha);
            }
            return shown;
        }

            public void DrawJoints(Graphics g, Rectangle target)
            {
            if (target.Width < 8 || target.Height < 8) return;
            PoseDetection pose; Size size; List<PointF> drawn;
            lock (gate)
            {
                if (latest == null) return;
                if (sticky && !playing) { if (latestStamp != currentStamp) return; }
                else if (clock.ElapsedMilliseconds - lastResult > StaleMilliseconds) return;
                pose = latest; size = latestSize;
                drawn = EaseShown(pose);
            }
            if (size.Width <= 0 || size.Height <= 0 || pose.Points.Count < 8) return;

            Func<PointF, PointF> map = p => new PointF(
                target.X + p.X * target.Width / size.Width,
                target.Y + p.Y * target.Height / size.Height);
            List<PointF> pts = new List<PointF>();
            foreach (PointF p in drawn) pts.Add(map(p));

            SmoothingMode oldSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            try
            {
                // 0 hip, 1 knee, 2 ankle, 3 toe (guess, not drawn), 4 shoulder, 5 elbow, 6 wrist, 7 head
                int[,] links = { { 0, 1 }, { 1, 2 }, { 0, 4 }, { 4, 5 }, { 5, 6 }, { 4, 7 } };
                using (Pen line = new Pen(Color.FromArgb(210, 184, 243, 74), 3F))
                using (Brush dot = new SolidBrush(Color.FromArgb(242, 126, 44)))
                using (Pen warn = new Pen(Color.FromArgb(255, 214, 64), 2F))
                {
                    for (int i = 0; i < links.GetLength(0); i++)
                        g.DrawLine(line, pts[links[i, 0]], pts[links[i, 1]]);
                    for (int i = 0; i < pts.Count; i++)
                    {
                        if (i == 3) continue;
                        g.FillEllipse(dot, pts[i].X - 5, pts[i].Y - 5, 10, 10);
                        if (pinned.ContainsKey(i)) g.DrawEllipse(Pens.White, pts[i].X - 9, pts[i].Y - 9, 18, 18);
                        else if (pose.Scores[i] < 0.4F) g.DrawEllipse(warn, pts[i].X - 10, pts[i].Y - 10, 20, 20);
                    }
                }

                using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
                {
                    if (pose.Scores[0] >= 0.3F && pose.Scores[1] >= 0.3F && pose.Scores[2] >= 0.3F)
                        Label(g, font, pts[1], "Knee " + Angle(pts[0], pts[1], pts[2]).ToString("0") + "°", pose.FacingRight);
                    if (pose.Scores[0] >= 0.3F && pose.Scores[1] >= 0.3F && pose.Scores[4] >= 0.3F)
                        Label(g, font, pts[0], "Hip " + Angle(pts[4], pts[0], pts[1]).ToString("0") + "°", !pose.FacingRight);
                    Label(g, font, new PointF(target.X + 8, target.Y + 22), "LIVE JOINTS \u00B7 estimate \u00B7 drag a dot to correct it, right-click to release \u00B7 Ctrl+J hides", true, true);
                }
            }
            finally { g.SmoothingMode = oldSmoothing; }
            }
        }
    }

    /// <summary>One Euro filter: smooth when the value barely changes, responsive when it changes fast.</summary>
    internal class OneEuro
    {
        private const double MinCutoff = 1.2, Beta = 30.0, DerivativeCutoff = 1.0;
        private bool has;
        private double previous, previousDerivative, previousTime;

        public bool HasValue { get { return has; } }
        public double Last { get { return previous; } }

        public void Reset() { has = false; }

        public double Filter(double value, double time, double strength)
        {
            if (!has)
            {
                has = true; previous = value; previousDerivative = 0; previousTime = time;
                return value;
            }
            double dt = Math.Max(time - previousTime, 1e-3);
            double derivative = (value - previous) / dt;
            double ad = Alpha(DerivativeCutoff, dt);
            double smoothedDerivative = ad * derivative + (1 - ad) * previousDerivative;
            double cutoff = MinCutoff * strength + Beta * Math.Abs(smoothedDerivative);
            double a = Alpha(cutoff, dt);
            double result = a * value + (1 - a) * previous;
            previous = result; previousDerivative = smoothedDerivative; previousTime = time;
            return result;
        }

        private static double Alpha(double cutoff, double dt)
        {
            double tau = 1.0 / (2 * Math.PI * cutoff);
            return 1.0 / (1.0 + tau / dt);
        }
    }

    /// <summary>
    /// Learns the rider's limb lengths while the joints are detected clearly, then uses them to correct a
    /// knee or elbow the model is unsure about: the joint must sit where both limb lengths fit between its neighbours.
    /// </summary>
    internal class SegmentModel
    {
        private const int Needed = 8, Kept = 40;
        private readonly List<double>[] samples = { new List<double>(), new List<double>(), new List<double>(), new List<double>() };
        private bool facingRight;
        private long lastSeen;

        // 0 hip, 1 knee, 2 ankle, 4 shoulder, 5 elbow, 6 wrist (the order PoseDetection uses).
        private static readonly int[,] Limbs = { { 0, 1, 2 }, { 4, 5, 6 } };

        public void Constrain(PoseDetection pose, long now)
        {
            if (pose == null || pose.Points.Count < 8) return;
            if (pose.FacingRight != facingRight || now - lastSeen > 6000)
                foreach (List<double> list in samples) list.Clear();
            facingRight = pose.FacingRight;
            lastSeen = now;

            for (int limb = 0; limb < 2; limb++)
            {
                int a = Limbs[limb, 0], b = Limbs[limb, 1], c = Limbs[limb, 2];
                List<double> upper = samples[limb * 2], lower = samples[limb * 2 + 1];
                double upperNow = Dist(pose.Points[a], pose.Points[b]), lowerNow = Dist(pose.Points[b], pose.Points[c]);
                bool clear = pose.Scores[a] >= 0.55F && pose.Scores[b] >= 0.55F && pose.Scores[c] >= 0.55F;
                if (clear)
                {
                    Add(upper, upperNow);
                    Add(lower, lowerNow);
                    continue;
                }
                if (upper.Count < Needed || lower.Count < Needed) continue;
                if (pose.Scores[a] < 0.4F || pose.Scores[c] < 0.4F) continue; // the ends must be trusted to place the middle joint

                double upperLen = Median(upper), lowerLen = Median(lower);
                bool offLength = Math.Abs(upperNow - upperLen) > 0.15 * upperLen || Math.Abs(lowerNow - lowerLen) > 0.15 * lowerLen;
                if (pose.Scores[b] >= 0.55F && !offLength) continue;

                pose.Points[b] = PlaceMiddle(pose.Points[a], pose.Points[c], upperLen, lowerLen, pose.Points[b]);
                pose.Scores[b] = Math.Max(pose.Scores[b], 0.5F);
            }
        }

        // The middle joint lies where circles of the two limb lengths around the end points meet; take the meeting point nearest the detection.
        internal static PointF PlaceMiddle(PointF start, PointF end, double upper, double lower, PointF detected)
        {
            double dx = end.X - start.X, dy = end.Y - start.Y;
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 1e-6) return detected;
            double ux = dx / d, uy = dy / d;
            if (d >= upper + lower || d <= Math.Abs(upper - lower))
            {
                double t = upper / (upper + lower); // stretched out or folded: stay on the line
                return new PointF((float)(start.X + dx * t), (float)(start.Y + dy * t));
            }
            double along = (upper * upper - lower * lower + d * d) / (2 * d);
            double height = Math.Sqrt(Math.Max(0, upper * upper - along * along));
            double px = start.X + ux * along, py = start.Y + uy * along;
            PointF one = new PointF((float)(px - uy * height), (float)(py + ux * height));
            PointF two = new PointF((float)(px + uy * height), (float)(py - ux * height));
            return Dist(one, detected) <= Dist(two, detected) ? one : two;
        }

        private static void Add(List<double> list, double value)
        {
            list.Add(value);
            if (list.Count > Kept) list.RemoveAt(0);
        }

        private static double Median(List<double> list)
        {
            List<double> sorted = new List<double>(list);
            sorted.Sort();
            return sorted[sorted.Count / 2];
        }

        private static double Dist(PointF a, PointF b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
