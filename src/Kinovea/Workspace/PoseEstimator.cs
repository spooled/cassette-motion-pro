using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace CassetteMotionPro.Workspace
{
    /// <summary>
    /// Finds the rider's joints in one still frame with MoveNet Thunder (Apache 2.0) running on ONNX Runtime.
    /// The result is only a first guess; the user reviews and drags every point.
    /// </summary>
    internal class PoseDetection
    {
        // In the order the tracking screen uses: hip, knee, ankle, toe, shoulder, elbow, wrist, head.
        public List<PointF> Points = new List<PointF>();
        public List<float> Scores = new List<float>();
        public bool FacingRight;
        public double Confidence;
    }

    internal static class PoseEstimator
    {
        private const int Size = 256;
        private static InferenceSession session;
        private static bool failed;
        private static readonly object gate = new object();

        public static string ModelPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", "movenet_thunder.onnx"); }
        }

        // quick: one pass on the whole frame only (used while a video is playing, when speed matters more than the last bit of accuracy).
        public static PoseDetection Detect(Bitmap frame, bool quick = false)
        {
            try
            {
                lock (gate)
                {
                    if (failed) return null;
                    if (session == null)
                    {
                        if (!File.Exists(ModelPath)) { Fail("Model file not found: " + ModelPath, null); return null; }
                        session = CreateSession();
                    }
                    return Refined(frame, quick);
                }
            }
            catch (Exception exception)
            {
                Fail(exception.GetType().Name + ": " + exception.Message, exception.ToString());
                return null;
            }
        }

        /// <summary>
        /// Live views: follow the rider from one reading to the next. When the last reading found the rider, look only at a
        /// square around where they were (one pass instead of two, and sharper than the whole frame); otherwise search the whole
        /// frame again. crop carries the square from one call to the next and is cleared when the rider is lost.
        /// </summary>
        public static PoseDetection DetectTracked(Bitmap frame, ref RectangleF? crop, bool quick = false)
        {
            try
            {
                lock (gate)
                {
                    if (failed) return null;
                    if (session == null)
                    {
                        if (!File.Exists(ModelPath)) { Fail("Model file not found: " + ModelPath, null); return null; }
                        session = CreateSession();
                    }
                    PoseDetection result = null;
                    if (crop.HasValue)
                    {
                        result = Run(frame, crop.Value);
                        if (result != null && (Average(result) < 0.3 || Found(result) < 5)) result = null; // lost the rider: search again
                    }
                    if (result == null) result = Refined(frame, quick);
                    crop = result == null ? null : CropAround(result, frame, 1.6F);
                    return result;
                }
            }
            catch (Exception exception)
            {
                Fail(exception.GetType().Name + ": " + exception.Message, exception.ToString());
                return null;
            }
        }

        private static int Found(PoseDetection d)
        {
            int n = 0;
            for (int i = 0; i < d.Scores.Count; i++) if (i != 3 && d.Scores[i] >= 0.3F) n++;
            return n;
        }

        // Leave some processor cores free so the video and the screen stay smooth while the model runs.
        private static InferenceSession CreateSession()
        {
            using (SessionOptions options = new SessionOptions())
            {
                options.IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2);
                return new InferenceSession(ModelPath, options);
            }
        }

        private static long lastLog;

        /// <summary>Writes a line to pose-error.txt without turning the pose model off (at most one line every few seconds).</summary>
        public static void Log(string text)
        {
            try
            {
                long now = Environment.TickCount;
                if (now - lastLog < 5000) return;
                lastLog = now;
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cassette Motion Pro");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "pose-error.txt"), DateTime.Now.ToString("s") + "  " + text + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception) { }
        }

        public static bool Unavailable { get { return failed; } }

        /// <summary>Short reason the pose model could not run, shown on screen. Full detail goes to pose-error.txt.</summary>
        public static string LastError { get; private set; }

        private static void Fail(string reason, string detail)
        {
            failed = true;
            LastError = reason;
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cassette Motion Pro");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "pose-error.txt"),
                    DateTime.Now.ToString("s") + "  " + reason + Environment.NewLine + (detail ?? string.Empty) + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception) { }
        }

        public static PoseDetection Detect(string imagePath)
        {
            try
            {
                lock (gate)
                {
                    if (failed) return null;
                    if (session == null)
                    {
                        if (!File.Exists(ModelPath)) { Fail("Model file not found: " + ModelPath, null); return null; }
                        session = CreateSession();
                    }
                    using (Bitmap source = new Bitmap(imagePath))
                        return Refined(source);
                }
            }
            catch (Exception exception)
            {
                Fail(exception.GetType().Name + ": " + exception.Message, exception.ToString());
                return null;
            }
        }

        // The model sees a 256x256 square, so a rider filling a third of a wide frame gets very few pixels.
        // After a first pass on the whole frame, look again at a square cropped around the rider and keep that if it is at least as sure.
        private static PoseDetection Refined(Bitmap source, bool quick = false)
        {
            PoseDetection first = Run(source, null);
            if (first == null || quick) return first;
            RectangleF? crop = CropAround(first, source, 1.4F);
            if (!crop.HasValue) return first;
            PoseDetection second = Run(source, crop.Value);
            if (second == null) return first;
            return Average(second) >= Average(first) - 0.02 ? second : first;
        }

        // A square around the joints found (margin = how much bigger than the joints' extent), or null when the rider already fills most of the frame.
        private static RectangleF? CropAround(PoseDetection pose, Bitmap source, float margin)
        {
            float fullSide = Math.Max(source.Width, source.Height);
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            int used = 0;
            for (int i = 0; i < pose.Points.Count; i++)
            {
                if (i == 3 || pose.Scores[i] < 0.3F) continue;
                PointF p = pose.Points[i];
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                used++;
            }
            if (used < 3) return null;
            float side = Math.Max(maxX - minX, maxY - minY) * margin;
            side = Math.Max(side, fullSide * 0.25F);
            if (side > fullSide * 0.85F) return null;
            float cx = (minX + maxX) / 2F, cy = (minY + maxY) / 2F;
            return new RectangleF(cx - side / 2F, cy - side / 2F, side, side);
        }

        private static double Distance(PointF a, PointF b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        private static double Average(PoseDetection d)
        {
            double sum = 0; int n = 0;
            for (int i = 0; i < d.Scores.Count; i++) if (i != 3) { sum += d.Scores[i]; n++; }
            return n == 0 ? 0 : sum / n;
        }

        // area: the square piece of the picture (in picture pixels, may reach outside it) shown to the model; null = the whole picture.
        private static PoseDetection Run(Bitmap source, RectangleF? area)
        {
            int fullSide = Math.Max(source.Width, source.Height);
            RectangleF view = area ?? new RectangleF(-(fullSide - source.Width) / 2F, -(fullSide - source.Height) / 2F, fullSide, fullSide);
            float side = view.Width;
            float offsetX = -view.X, offsetY = -view.Y;
            int[] values = new int[Size * Size * 3];
            using (Bitmap square = new Bitmap(Size, Size, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(square))
                {
                    g.Clear(Color.Black);
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    float scale = Size / side;
                    // Shrinking a lot needs the slower filter to keep thin limbs visible; a crop around the rider shrinks less and can use the quick one.
                    g.InterpolationMode = scale < 0.4F ? InterpolationMode.HighQualityBilinear : InterpolationMode.Bilinear;
                    g.DrawImage(source, offsetX * scale, offsetY * scale, source.Width * scale, source.Height * scale);
                }
                System.Drawing.Imaging.BitmapData data = square.LockBits(new Rectangle(0, 0, Size, Size),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                try
                {
                    byte[] bytes = new byte[data.Stride * Size];
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                    int k = 0;
                    for (int y = 0; y < Size; y++)
                    {
                        int i = y * data.Stride; // BGR in memory
                        for (int x = 0; x < Size; x++, i += 3)
                        {
                            values[k++] = bytes[i + 2]; values[k++] = bytes[i + 1]; values[k++] = bytes[i];
                        }
                    }
                }
                finally { square.UnlockBits(data); }
            }
            DenseTensor<int> input = new DenseTensor<int>(values, new[] { 1, Size, Size, 3 });

            float[] raw;
            string inputName = session.InputMetadata.Keys.First();
            using (var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, input) }))
                raw = results.First().AsEnumerable<float>().ToArray();
            if (raw.Length < 51) return null;

            // COCO order: 0 nose, 3/4 ears, 5/6 shoulders, 7/8 elbows, 9/10 wrists, 11/12 hips, 13/14 knees, 15/16 ankles.
            Func<int, PointF> pt = i => new PointF(raw[i * 3 + 1] * side - offsetX, raw[i * 3] * side - offsetY);
            Func<int, float> sc = i => raw[i * 3 + 2];

            float left = sc(5) + sc(11) + sc(13) + sc(15);
            float right = sc(6) + sc(12) + sc(14) + sc(16);
            int o = left >= right ? 0 : 1; // 0 = left side of the body, 1 = right side
            int shoulder = 5 + o, elbow = 7 + o, wrist = 9 + o, hip = 11 + o, knee = 13 + o, ankle = 15 + o, ear = 3 + o;

            PointF nose = pt(0), hipP = pt(hip), kneeP = pt(knee), ankleP = pt(ankle);
            bool facingRight = nose.X >= hipP.X;
            float dir = facingRight ? 1F : -1F;
            float shin = (float)Math.Sqrt(Math.Pow(kneeP.X - ankleP.X, 2) + Math.Pow(kneeP.Y - ankleP.Y, 2));
            // MoveNet has no toe point: place a suggested one ahead of the ankle for the user to fix.
            PointF toe = new PointF(ankleP.X + dir * shin * 0.55F, ankleP.Y + shin * 0.12F);
            bool earOk = sc(ear) >= 0.3F;

            PoseDetection d = new PoseDetection { FacingRight = facingRight };
            d.Points.Add(hipP); d.Scores.Add(sc(hip));
            d.Points.Add(kneeP); d.Scores.Add(sc(knee));
            d.Points.Add(ankleP); d.Scores.Add(sc(ankle));
            d.Points.Add(toe); d.Scores.Add(0F);
            d.Points.Add(pt(shoulder)); d.Scores.Add(sc(shoulder));
            d.Points.Add(pt(elbow)); d.Scores.Add(sc(elbow));
            d.Points.Add(pt(wrist)); d.Scores.Add(sc(wrist));
            d.Points.Add(earOk ? pt(ear) : nose); d.Scores.Add(earOk ? sc(ear) : sc(0));
            // A wrist far from where a forearm of about the upper arm's length could reach is probably a wrong guess: mark it unsure.
            double upperArm = Distance(d.Points[4], d.Points[5]), foreArm = Distance(d.Points[5], d.Points[6]);
            if (upperArm > 1 && (foreArm > upperArm * 1.6 || foreArm < upperArm * 0.35)) d.Scores[6] = Math.Min(d.Scores[6], 0.25F);
            d.Confidence = Math.Min(95, d.Scores.Where((s, i) => i != 3).Average() * 100);
            return d;
        }
    }
}
