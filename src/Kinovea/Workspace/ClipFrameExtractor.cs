/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using Kinovea.Video;

namespace CassetteMotionPro.Workspace
{
    internal sealed class ClipProbe
    {
        public double DurationSeconds;
        public double FramesPerSecond;
        public Size FrameSize;
    }

    // Pulls evenly spaced still frames out of a video clip with the video reader that
    // Video Studio already uses, so the fitter does not have to save checkpoint images by hand.
    internal static class ClipFrameExtractor
    {
        public static ClipProbe Probe(string videoPath)
        {
            VideoReader reader = OpenReader(videoPath);
            try
            {
                VideoInfo info = reader.Info;
                double perSecond = info.AverageTimeStampsPerSeconds;
                if (perSecond <= 0)
                    throw new InvalidOperationException("The clip does not report its length.");
                return new ClipProbe
                {
                    DurationSeconds = info.DurationTimeStamps / perSecond,
                    FramesPerSecond = info.FramesPerSeconds,
                    FrameSize = info.ReferenceSize
                };
            }
            finally
            {
                reader.Close();
            }
        }

        // Saves `count` frames between the two times (inclusive) as numbered PNG files and returns their paths in time order.
        public static string[] Extract(string videoPath, double startSeconds, double endSeconds, int count, string outputDirectory, string prefix)
        {
            if (count < 1)
                throw new ArgumentException("At least one frame is needed.");
            if (endSeconds < startSeconds)
                throw new ArgumentException("The end time must not be before the start time.");

            Directory.CreateDirectory(outputDirectory);
            VideoReader reader = OpenReader(videoPath);
            try
            {
                VideoInfo info = reader.Info;
                double perSecond = info.AverageTimeStampsPerSeconds;
                double perFrame = info.AverageTimeStampsPerFrame;
                if (perSecond <= 0)
                    throw new InvalidOperationException("The clip does not report its length.");

                long first = info.FirstTimeStamp;
                long last = first + (long)Math.Max(0, info.DurationTimeStamps - perFrame);
                string[] paths = new string[count];
                long previousTimestamp = long.MinValue;
                int saved = 0;

                for (int i = 0; i < count; i++)
                {
                    double fraction = count == 1 ? 0.5 : (double)i / (count - 1);
                    double seconds = startSeconds + (endSeconds - startSeconds) * fraction;
                    long target = first + (long)Math.Round(seconds * perSecond);
                    target = Math.Max(first, Math.Min(last, target));

                    if (!reader.MoveTo(0, target) || reader.Current == null || reader.Current.Image == null)
                        continue;
                    if (reader.Current.Timestamp == previousTimestamp)
                        continue;
                    previousTimestamp = reader.Current.Timestamp;

                    string path = Path.Combine(outputDirectory, prefix + "-" + (saved + 1).ToString("00", CultureInfo.InvariantCulture) + ".png");
                    using (Bitmap copy = new Bitmap(reader.Current.Image))
                        copy.Save(path, ImageFormat.Png);
                    paths[saved] = path;
                    saved++;
                }

                if (saved == 0)
                    throw new InvalidOperationException("No frames could be read from that part of the clip.");
                string[] result = new string[saved];
                Array.Copy(paths, result, saved);
                return result;
            }
            finally
            {
                reader.Close();
            }
        }

        private static VideoReader OpenReader(string videoPath)
        {
            if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
                throw new FileNotFoundException("The clip could not be found.", videoPath);

            VideoReader reader = VideoTypeManager.GetVideoReader(Path.GetExtension(videoPath));
            if (reader == null)
                throw new InvalidOperationException("This kind of video file is not supported.");

            reader.Options = VideoOptions.Default;
            OpenVideoResult result;
            try
            {
                result = reader.Open(videoPath);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("The clip could not be opened. " + exception.Message, exception);
            }

            if (result != OpenVideoResult.Success)
            {
                try { reader.Close(); } catch (Exception) { }
                throw new InvalidOperationException("The clip could not be opened (" + result + ").");
            }
            return reader;
        }
    }
}
