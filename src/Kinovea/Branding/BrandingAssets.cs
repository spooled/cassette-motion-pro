/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System.Drawing;
using System.IO;
using System.Reflection;

namespace CassetteMotionPro.Branding
{
    /// <summary>
    /// Provides Cassette Motion Pro assets without coupling the core video engine
    /// to product-specific resource names.
    /// </summary>
    internal static class BrandingAssets
    {
        public static Image Logo { get { return LoadImage("CassetteMotionPro.Brand.Logo.png"); } }

        public static Image Splash { get { return LoadImage("CassetteMotionPro.Brand.Splash.png"); } }

        public static Icon ApplicationIcon
        {
            get
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CassetteMotionPro.Brand.Application.ico"))
                {
                    if (stream == null)
                        return null;

                    using (Icon icon = new Icon(stream))
                        return (Icon)icon.Clone();
                }
            }
        }

        private static Image LoadImage(string resourceName)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;

                using (Image image = Image.FromStream(stream))
                    return new Bitmap(image);
            }
        }
    }
}
