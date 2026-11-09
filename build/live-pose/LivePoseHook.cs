using System;
using System.Drawing;

namespace Kinovea.ScreenManager
{
    /// <summary>
    /// Two optional callbacks the Cassette Motion Pro app plugs into the live camera view.
    /// This file is copied into Kinovea.ScreenManager at build time (build/apply-live-pose-hook.ps1).
    /// With nothing plugged in, both calls do nothing.
    /// </summary>
    public static class LivePoseHook
    {
        public static Action<Bitmap> FrameReady;
        public static Action<Graphics, Rectangle, Size> Paint;

        public static void Submit(Bitmap frame)
        {
            Action<Bitmap> handler = FrameReady;
            if (handler == null) return;
            try { handler(frame); } catch (Exception) { }
        }

        public static void Draw(Graphics canvas, Rectangle imageRectangle, Size imageSize)
        {
            Action<Graphics, Rectangle, Size> handler = Paint;
            if (handler == null) return;
            try { handler(canvas, imageRectangle, imageSize); } catch (Exception) { }
        }
    }
}
