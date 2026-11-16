using System;
using System.Drawing;
using System.Windows.Forms;

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
        public static Func<Point, MouseButtons, Rectangle, bool> MouseDown, MouseMove, MouseUp;

        // Each returns true when the overlay used the mouse event (a dot was grabbed, moved or released).
        public static bool PointerDown(Point location, MouseButtons button, Rectangle imageRectangle)
        {
            return Ask(MouseDown, location, button, imageRectangle);
        }

        public static bool PointerMove(Point location, MouseButtons button, Rectangle imageRectangle)
        {
            return Ask(MouseMove, location, button, imageRectangle);
        }

        public static bool PointerUp(Point location, MouseButtons button, Rectangle imageRectangle)
        {
            return Ask(MouseUp, location, button, imageRectangle);
        }

        private static bool Ask(Func<Point, MouseButtons, Rectangle, bool> handler, Point location, MouseButtons button, Rectangle imageRectangle)
        {
            if (handler == null) return false;
            try { return handler(location, button, imageRectangle); } catch (Exception) { return false; }
        }

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
