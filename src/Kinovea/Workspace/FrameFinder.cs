/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CassetteMotionPro.Workspace
{
    // One frame size from the frame list (published geometry, millimetres and degrees).
    public sealed class FrameGeometry
    {
        public string BikeType = "Road";   // Road, Gravel, Mountain
        public string Brand = string.Empty, Model = string.Empty, Size = string.Empty;
        public double Stack, Reach, HeadTubeAngle, SeatTubeAngle;
        public string Source = string.Empty;

        public string Name
        {
            get { return (Brand + " " + Model + " " + Size).Trim(); }
        }
    }

    // How one frame gets the hands to the sizing estimate.
    public sealed class FrameMatch
    {
        public FrameGeometry Frame;
        public double ExactStem, ExactSpacers;   // the parts that put the hands exactly on the estimate
        public double Stem, Spacers;             // nearest parts you can buy (10 mm stems, 5 mm spacers; 5 mm stems on mountain bikes)
        public double HandMissX, HandMissY;      // where the hands end up with those parts, minus the estimate (+ = further forward / higher)
        public double SetbackChange;             // extra saddle setback (mm) the post needs on this frame's seat angle; + = more setback
        public bool InFrameRange;                // stack and reach both inside the estimate's frame range
        public double Score;                     // lower is closer to the preferred cockpit
    }

    // The frame list is a CSV file the fitter can open and add to in Excel or Notepad. Lines starting with # are notes.
    // Columns: BikeType,Brand,Model,Size,Stack,Reach,HeadTubeAngle,SeatTubeAngle,Source
    // Kept to C# 5 and to the usings the build's test script provides.
    public static class FrameCatalog
    {
        public const string Header = "BikeType,Brand,Model,Size,Stack,Reach,HeadTubeAngle,SeatTubeAngle,Source";

        // Starter list: only frames whose numbers were copied from the maker's own geometry chart (checked 23 Nov 2026).
        // Stack and reach are the frame values (not "effective" values that include a stem).
        public static readonly string StarterCsv = string.Join(Environment.NewLine, new string[]
        {
            "# Cassette Motion Pro frame list. Add a line for each frame size you want Bike Sizing to consider.",
            "# Copy stack, reach and angles from the maker's geometry chart (frame stack/reach, not 'effective' values).",
            "# BikeType is Road, Gravel or Mountain. Numbers use a dot for decimals, for example 72.5.",
            Header,
            "Road,Canyon,Endurace CF,2XS,525,382,70.3,73.5,canyon.com Endurace CF 7 geometry (checked 2026-11-23)",
            "Road,Canyon,Endurace CF,XS,545,386,71,73.5,canyon.com Endurace CF 7 geometry (checked 2026-11-23)",
            "Road,Canyon,Endurace CF,S,563,390,71.8,73.5,canyon.com Endurace CF 7 geometry (checked 2026-11-23)",
            "Road,Canyon,Endurace CF,M,584,392,72.5,73.5,canyon.com Endurace CF 7 geometry (checked 2026-11-23)",
            "Road,Canyon,Endurace CF,L,605,401,72.5,73.5,canyon.com Endurace CF 7 geometry (checked 2026-11-23)",
            "Road,Canyon,Endurace CF,XL,629,409,72.8,73.5,canyon.com Endurace CF 7 geometry (checked 2026-11-23)",
            "Road,Canyon,Endurace CF,2XL,649,427,72.8,73.5,canyon.com Endurace CF 7 geometry (checked 2026-11-23)",
            "Road,Giant,Defy Advanced,S,541,375,71.8,74,giant-bicycles.com Defy Advanced 0 2026 geometry (checked 2026-11-23)",
            "Road,Giant,Defy Advanced,M,558,380,72.5,73.5,giant-bicycles.com Defy Advanced 0 2026 geometry (checked 2026-11-23)",
            "Road,Giant,Defy Advanced,ML,577,384,72.5,73,giant-bicycles.com Defy Advanced 0 2026 geometry (checked 2026-11-23)",
            "Road,Giant,Defy Advanced,L,596,393,72.5,73,giant-bicycles.com Defy Advanced 0 2026 geometry (checked 2026-11-23)",
            "Road,Giant,Defy Advanced,XL,615,402,72.5,73,giant-bicycles.com Defy Advanced 0 2026 geometry (checked 2026-11-23)",
            "Gravel,Canyon,Grizl CF,2XS,550,369,68.8,73.5,canyon.com Grizl CF 7 geometry (checked 2026-11-23)",
            "Gravel,Canyon,Grizl CF,XS,559,380,69.5,73.5,canyon.com Grizl CF 7 geometry (checked 2026-11-23)",
            "Gravel,Canyon,Grizl CF,S,578,388,70.5,73.5,canyon.com Grizl CF 7 geometry (checked 2026-11-23)",
            "Gravel,Canyon,Grizl CF,M,596,404,71,73.5,canyon.com Grizl CF 7 geometry (checked 2026-11-23)",
            "Gravel,Canyon,Grizl CF,L,619,420,71,73.5,canyon.com Grizl CF 7 geometry (checked 2026-11-23)",
            "Gravel,Canyon,Grizl CF,XL,638,429,71,73.5,canyon.com Grizl CF 7 geometry (checked 2026-11-23)",
            "Gravel,Canyon,Grizl CF,2XL,660,448,71,73.5,canyon.com Grizl CF 7 geometry (checked 2026-11-23)",
        }) + Environment.NewLine;

        // Reads the list. Lines that cannot be read are skipped and described in 'problems' (line number and reason).
        public static List<FrameGeometry> Parse(string text, List<string> problems)
        {
            List<FrameGeometry> frames = new List<FrameGeometry>();
            if (string.IsNullOrEmpty(text)) return frames;
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                string line = lines[n].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                if (line.StartsWith("BikeType,", StringComparison.OrdinalIgnoreCase)) continue; // header
                string[] cells = SplitCsv(line);
                FrameGeometry f = new FrameGeometry();
                string reason = null;
                if (cells.Length < 8) reason = "needs 8 columns (" + Header + ")";
                else
                {
                    f.BikeType = NormaliseType(cells[0]);
                    f.Brand = cells[1].Trim(); f.Model = cells[2].Trim(); f.Size = cells[3].Trim();
                    f.Source = cells.Length > 8 ? cells[8].Trim() : string.Empty;
                    if (f.BikeType == null) reason = "bike type must be Road, Gravel or Mountain";
                    else if (!Read(cells[4], 400, 750, out f.Stack)) reason = "stack should be a number of mm between 400 and 750";
                    else if (!Read(cells[5], 300, 550, out f.Reach)) reason = "reach should be a number of mm between 300 and 550";
                    else if (!Read(cells[6], 60, 76, out f.HeadTubeAngle)) reason = "head tube angle should be between 60 and 76 degrees";
                    else if (!Read(cells[7], 70, 80, out f.SeatTubeAngle)) reason = "seat tube angle should be between 70 and 80 degrees";
                }
                if (reason != null)
                {
                    if (problems != null) problems.Add("Line " + (n + 1).ToString(CultureInfo.InvariantCulture) + ": " + reason + ".");
                    continue;
                }
                frames.Add(f);
            }
            return frames;
        }

        private static string NormaliseType(string value)
        {
            string v = value == null ? string.Empty : value.Trim();
            foreach (string type in new string[] { "Road", "Gravel", "Mountain" })
                if (string.Equals(v, type, StringComparison.OrdinalIgnoreCase)) return type;
            if (string.Equals(v, "MTB", StringComparison.OrdinalIgnoreCase)) return "Mountain";
            return null;
        }

        private static bool Read(string text, double min, double max, out double value)
        {
            // Accept a dot or a comma as the decimal mark.
            string t = (text ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= min && value <= max;
        }

        // Splits one CSV line; a cell in double quotes may contain commas (Excel writes "4,5" for a comma decimal).
        private static string[] SplitCsv(string line)
        {
            List<string> cells = new List<string>();
            string current = string.Empty;
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current += '"'; i++; }
                    else quoted = !quoted;
                }
                else if (c == ',' && !quoted) { cells.Add(current); current = string.Empty; }
                else current += c;
            }
            cells.Add(current);
            return cells.ToArray();
        }
    }

    // Finds frames that put the hands on the sizing estimate with parts you can buy.
    public static class FrameFinder
    {
        public const double DropBarStemMin = 70, DropBarStemMax = 130, DropBarSpacersMax = 40;
        public const double FlatBarStemMin = 35, FlatBarStemMax = 80, FlatBarSpacersMax = 30;

        public static List<FrameMatch> Find(BikeSizingResult estimate, string bikeType, IEnumerable<FrameGeometry> frames)
        {
            List<FrameMatch> matches = new List<FrameMatch>();
            if (estimate == null || frames == null) return matches;
            bool mountain = string.Equals(bikeType, "Mountain", StringComparison.OrdinalIgnoreCase);
            double stemMin = mountain ? FlatBarStemMin : DropBarStemMin;
            double stemMax = mountain ? FlatBarStemMax : DropBarStemMax;
            double spacersMax = mountain ? FlatBarSpacersMax : DropBarSpacersMax;
            double stemStep = mountain ? 5 : 10;

            // Bar clamp centre the hands need (the hand contact point minus the bar and hood offsets).
            double clampX = estimate.HandlebarX - (estimate.BarReach + estimate.HandForward);
            double clampY = estimate.HandlebarY - estimate.HandUp;

            foreach (FrameGeometry f in frames)
            {
                if (f == null || !string.Equals(f.BikeType, bikeType, StringComparison.OrdinalIgnoreCase)) continue;
                double head = Radians(f.HeadTubeAngle);
                double upX = -Math.Cos(head), upY = Math.Sin(head);       // along the steerer, upward
                double perpX = Math.Sin(head), perpY = Math.Cos(head);    // square to the steerer, forward
                double stem = Radians(estimate.StemAngle);
                double dirX = Math.Cos(stem) * perpX + Math.Sin(stem) * upX;
                double dirY = Math.Cos(stem) * perpY + Math.Sin(stem) * upY;

                // Top of the head tube to the clamp = (spacers + top cap) along the steerer + stem length along the stem.
                double vx = clampX - f.Reach, vy = clampY - f.Stack;
                double det = upX * dirY - upY * dirX;
                if (Math.Abs(det) < 1e-9) continue;
                double along = (vx * dirY - vy * dirX) / det;
                double length = (upX * vy - upY * vx) / det;
                double exactSpacers = along - BikeSizing.SpacerToStemCentre;
                if (length < stemMin - 5 || length > stemMax + 5 || exactSpacers < -3 || exactSpacers > spacersMax + 3) continue;

                FrameMatch m = new FrameMatch();
                m.Frame = f;
                m.ExactStem = length;
                m.ExactSpacers = exactSpacers;
                m.Stem = Clamp(Math.Round(length / stemStep) * stemStep, stemMin, stemMax);
                // With that stem, the spacer height that gets the hands closest to the estimate, in 5 mm steps.
                double bestAlong = (vx - m.Stem * dirX) * upX + (vy - m.Stem * dirY) * upY;
                m.Spacers = Clamp(Math.Round((bestAlong - BikeSizing.SpacerToStemCentre) / 5.0) * 5.0, 0, spacersMax);
                double a = m.Spacers + BikeSizing.SpacerToStemCentre;
                m.HandMissX = a * upX + m.Stem * dirX - vx;
                m.HandMissY = a * upY + m.Stem * dirY - vy;
                m.SetbackChange = estimate.SaddleHeight * (Math.Cos(Radians(estimate.SeatAngle)) - Math.Cos(Radians(f.SeatTubeAngle)));
                m.InFrameRange = f.Stack >= estimate.FrameStackMin - 0.5 && f.Stack <= estimate.FrameStackMax + 0.5
                    && f.Reach >= estimate.FrameReachMin - 0.5 && f.Reach <= estimate.FrameReachMax + 0.5;
                // Closest to the cockpit the fitter chose, then closest hand position with real parts.
                m.Score = Math.Abs(length - estimate.StemLength) + Math.Abs(exactSpacers - estimate.Spacers)
                    + 0.5 * Math.Sqrt(m.HandMissX * m.HandMissX + m.HandMissY * m.HandMissY);
                matches.Add(m);
            }
            matches.Sort(delegate (FrameMatch x, FrameMatch y) { return x.Score.CompareTo(y.Score); });
            return matches;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static double Radians(double degrees) { return degrees * Math.PI / 180.0; }
    }
}
