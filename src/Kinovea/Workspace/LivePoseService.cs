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
        private const int IntervalMilliseconds = 250;
        private const int StaleMilliseconds = 1500;
        private static readonly object gate = new object();
        private static readonly Stopwatch clock = Stopwatch.StartNew();
        private static long lastSubmit = -IntervalMilliseconds;
        private static long lastResult;
        private static int busy;
        private static PoseDetection latest;
        private static Size latestSize;

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
                toastUntil = clock.ElapsedMilliseconds + 1400;
                EventHandler handler = EnabledChanged;
                if (handler != null) handler(null, EventArgs.Empty);
            }
        }

        public static bool Available { get { return !PoseEstimator.Unavailable; } }

        public static void Install()
        {
            LivePoseHook.FrameReady = OnFrame;
            LivePoseHook.Paint = OnPaint;
            Application.AddMessageFilter(new KeyFilter());
        }

        // Ctrl+J turns the live overlay on or off from anywhere in the app.
        private class KeyFilter : IMessageFilter
        {
            private const int WM_KEYDOWN = 0x0100;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_KEYDOWN || (Keys)(int)m.WParam != Keys.J) return false;
                if (Control.ModifierKeys != Keys.Control) return false;
                Enabled = !Enabled;
                return true;
            }
        }

        private static void OnFrame(Bitmap frame)
        {
            if (!Enabled || frame == null || PoseEstimator.Unavailable) return;
            long now = clock.ElapsedMilliseconds;
            if (now - lastSubmit < IntervalMilliseconds) return;
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) return;
            lastSubmit = now;

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
                            Smooth(result, latest, DateTime.UtcNow);
                            latest = result;
                            latestSize = copy.Size;
                            lastResult = clock.ElapsedMilliseconds;
                        }
                    }
                }
                catch (Exception) { }
                finally
                {
                    copy.Dispose();
                    Interlocked.Exchange(ref busy, 0);
                }
            });
        }

        // Blend with the previous reading so the dots do not jitter. Skipped when the rider turned around or the reading is old.
        private static void Smooth(PoseDetection now, PoseDetection before, DateTime unused)
        {
            if (before == null || before.FacingRight != now.FacingRight) return;
            if (clock.ElapsedMilliseconds - lastResult > StaleMilliseconds) return;
            for (int i = 0; i < now.Points.Count && i < before.Points.Count; i++)
            {
                PointF a = before.Points[i], b = now.Points[i];
                now.Points[i] = new PointF(a.X * 0.45F + b.X * 0.55F, a.Y * 0.45F + b.Y * 0.55F);
            }
        }

        private static void OnPaint(Graphics g, Rectangle target, Size imageSize)
        {
            if (!Enabled)
            {
                if (clock.ElapsedMilliseconds < toastUntil)
                    using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
                        Label(g, font, new PointF(target.X + 8, target.Y + 22), "LIVE JOINTS \u00B7 off (Ctrl+J)", true, true);
                return;
            }
            PoseDetection pose; Size size;
            lock (gate)
            {
                if (latest == null || clock.ElapsedMilliseconds - lastResult > StaleMilliseconds) return;
                pose = latest; size = latestSize;
            }
            if (size.Width <= 0 || size.Height <= 0 || pose.Points.Count < 8) return;

            Func<PointF, PointF> map = p => new PointF(
                target.X + p.X * target.Width / size.Width,
                target.Y + p.Y * target.Height / size.Height);
            List<PointF> pts = new List<PointF>();
            foreach (PointF p in pose.Points) pts.Add(map(p));

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
                        if (pose.Scores[i] < 0.4F) g.DrawEllipse(warn, pts[i].X - 10, pts[i].Y - 10, 20, 20);
                    }
                }

                using (Font font = new Font("Segoe UI", 10F, FontStyle.Bold))
                {
                    if (pose.Scores[0] >= 0.3F && pose.Scores[1] >= 0.3F && pose.Scores[2] >= 0.3F)
                        Label(g, font, pts[1], "Knee " + Angle(pts[0], pts[1], pts[2]).ToString("0") + "°", pose.FacingRight);
                    if (pose.Scores[0] >= 0.3F && pose.Scores[1] >= 0.3F && pose.Scores[4] >= 0.3F)
                        Label(g, font, pts[0], "Hip " + Angle(pts[4], pts[0], pts[1]).ToString("0") + "°", !pose.FacingRight);
                    Label(g, font, new PointF(target.X + 8, target.Y + 22), "LIVE JOINTS · estimate only", true, true);
                }
            }
            finally { g.SmoothingMode = oldSmoothing; }
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
    }
}
