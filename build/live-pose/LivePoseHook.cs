using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    /// Optional callbacks (each told which camera view it is for) the Cassette Motion Pro app plugs into the live camera view.
    /// This file is copied into Kinovea.ScreenManager at build time (build/apply-live-pose-hook.ps1).
    /// With nothing plugged in, both calls do nothing.
    /// </summary>
    public static class LivePoseHook
    {
        public static Action<object, Bitmap> FrameReady;
        public static Action<object, Graphics, Rectangle, Size> Paint;
        public static Func<object, Point, MouseButtons, Rectangle, bool> MouseDown, MouseMove, MouseUp;

        // Each returns true when the overlay used the mouse event (a dot was grabbed, moved or released).
        public static bool PointerDown(object view, Point location, MouseButtons button, Rectangle imageRectangle)
        {
            return Ask(view, MouseDown, location, button, imageRectangle);
        }

        public static bool PointerMove(object view, Point location, MouseButtons button, Rectangle imageRectangle)
        {
            return Ask(view, MouseMove, location, button, imageRectangle);
        }

        public static bool PointerUp(object view, Point location, MouseButtons button, Rectangle imageRectangle)
        {
            return Ask(view, MouseUp, location, button, imageRectangle);
        }

        private static bool Ask(object view, Func<object, Point, MouseButtons, Rectangle, bool> handler, Point location, MouseButtons button, Rectangle imageRectangle)
        {
            if (handler == null) return false;
            try { return handler(view, location, button, imageRectangle); } catch (Exception) { return false; }
        }

        public static void Submit(object view, Bitmap frame)
        {
            Action<object, Bitmap> handler = FrameReady;
            if (handler == null) return;
            try { handler(view, frame); } catch (Exception) { }
        }

        public static void Draw(object view, Graphics canvas, Rectangle imageRectangle, Size imageSize)
        {
            Action<object, Graphics, Rectangle, Size> handler = Paint;
            if (handler == null) return;
            try { handler(view, canvas, imageRectangle, imageSize); } catch (Exception) { }
        }
    }
}
