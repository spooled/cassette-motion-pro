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

        public static PoseDetection Detect(Bitmap frame)
        {
            try
            {
                lock (gate)
                {
                    if (failed) return null;
                    if (session == null)
                    {
                        if (!File.Exists(ModelPath)) { failed = true; return null; }
                        session = new InferenceSession(ModelPath);
                    }
                    return Run(frame);
                }
            }
            catch (Exception)
            {
                failed = true;
                return null;
            }
        }

        public static bool Unavailable { get { return failed; } }

        public static PoseDetection Detect(string imagePath)
        {
            try
            {
                lock (gate)
                {
                    if (failed) return null;
                    if (session == null)
                    {
                        if (!File.Exists(ModelPath)) { failed = true; return null; }
                        session = new InferenceSession(ModelPath);
                    }
                    using (Bitmap source = new Bitmap(imagePath))
                        return Run(source);
                }
            }
            catch (Exception)
            {
                failed = true;
                return null;
            }
        }

        private static PoseDetection Run(Bitmap source)
        {
            int side = Math.Max(source.Width, source.Height);
            float offsetX = (side - source.Width) / 2F, offsetY = (side - source.Height) / 2F;
            DenseTensor<int> input = new DenseTensor<int>(new[] { 1, Size, Size, 3 });
            using (Bitmap square = new Bitmap(Size, Size, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(square))
                {
                    g.Clear(Color.Black);
                    g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    float scale = (float)Size / side;
                    g.DrawImage(source, offsetX * scale, offsetY * scale, source.Width * scale, source.Height * scale);
                }
                System.Drawing.Imaging.BitmapData data = square.LockBits(new Rectangle(0, 0, Size, Size),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                try
                {
                    byte[] bytes = new byte[data.Stride * Size];
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                    for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        int i = y * data.Stride + x * 3; // BGR in memory
                        input[0, y, x, 0] = bytes[i + 2]; input[0, y, x, 1] = bytes[i + 1]; input[0, y, x, 2] = bytes[i];
                    }
                }
                finally { square.UnlockBits(data); }
            }

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
            d.Confidence = Math.Min(95, d.Scores.Where((s, i) => i != 3).Average() * 100);
            return d;
        }
    }
}
