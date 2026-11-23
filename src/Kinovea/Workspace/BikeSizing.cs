/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using System;
using System.Collections.Generic;

namespace CassetteMotionPro.Workspace
{
    // Sizing estimate from body measurements: where the saddle and the hands should be (as Handlebar X / Y,
    // measured from the bottom bracket centre like the rest of the app), and what frame stack and reach puts
    // the hands there with a given cockpit. A side-view stick figure of the seated rider is built from the
    // measurements and posed at a chosen back angle and shoulder angle. It is a starting point for the fit,
    // not a replacement for it. No UI types here so the build can test it.
    // Kept to C# 5 so the build's test script can compile it.
    public sealed class BikeSizingInput
    {
        // Body, in millimetres. ShoulderWidth is optional (0 = not measured).
        public double Height;
        public double Inseam;          // floor to crotch, standing barefoot, book pressed up firmly
        public double SternalNotch;    // floor to the notch at the top of the breastbone, standing barefoot
        public double ArmLength;       // shoulder bony point (acromion) to the centre of a closed fist, arm straight
        public double ShoulderWidth;   // acromion to acromion

        public string BikeType = "Road";        // Road, Gravel, Mountain, Triathlon / TT
        public string Style = "Endurance";      // Performance, Endurance, Comfort
        public string Flexibility = "Average";  // Limited, Average, Good

        // Targets and cockpit. NaN = use the default for the bike type and style.
        public double BackAngle = double.NaN;      // hip to shoulder line, degrees above horizontal
        public double ShoulderAngle = double.NaN;  // hip – shoulder – wrist/hand contact, degrees (the app calls this Body reach)
        public double StemLength = double.NaN;     // mm
        public double StemAngle = double.NaN;      // degrees, negative points down
        public double Spacers = double.NaN;        // mm of spacers under the stem
        public double HeadTubeAngle = double.NaN;  // degrees
        public double BarReach = double.NaN;       // mm, stem clamp to the front of the bar (drop bars); Triathlon / TT: pad centre ahead of the stem clamp (negative = behind)
        public double PadRise = double.NaN;        // Triathlon / TT: top of the elbow pad above the stem clamp centre, mm
    }

    public sealed class BikeSizingResult
    {
        public double SaddleHeight;      // BB centre to saddle top along the seat tube
        public double HandlebarX, HandlebarY;
        public double HandlebarXMin, HandlebarXMax, HandlebarYMin, HandlebarYMax;
        public double SaddleToHandDrop;  // saddle top height minus hand height (positive = hands lower)
        public double FrameStack, FrameReach;
        public double FrameStackMin, FrameStackMax, FrameReachMin, FrameReachMax;
        public double BarWidth;          // 0 when shoulder width was not given or the bike uses a flat bar
        public double BackAngle, ShoulderAngle, StemLength, StemAngle, Spacers, HeadTubeAngle, BarReach;
        public double TorsoLength;
        public double SeatAngle;                 // seat tube angle the estimate assumes
        public double HandForward, HandUp;       // hand contact point relative to the bar clamp centre (not counting bar reach); Triathlon / TT: HandUp is the pad rise
        public bool AeroPads;                    // Triathlon / TT: the contact point is the centre of the elbow pad, not the hands
        public List<string> Warnings = new List<string>();
    }

    public static class BikeSizing
    {
        public const double SpacerToStemCentre = 30.0; // headset top cap plus half the stem's clamp height
        public const double BackSpread = 2.0, ShoulderSpread = 2.5;  // degrees either way for the ranges
        // Triathlon / TT: the pad supports the forearm just ahead of the elbow. Centre of the pad's top surface
        // relative to the elbow joint centre (forearm resting on it).
        public const double PadForwardOfElbow = 60.0, PadBelowElbow = 45.0;

        private sealed class Profile
        {
            public double SeatAngle, HeadTubeAngle, StemLength, StemAngle, Spacers, BarReach;
            public double HandForward, HandUp;  // hand contact point relative to the bar clamp centre (not counting bar reach)
            public double SaddleFactor, ArmFactor, BarWidth;
            public double Performance, Endurance, Comfort; // back angles
            public double ReachPerformance, ReachEndurance, ReachComfort; // body reach angles (hip – shoulder – wrist)
            public bool DropBar, AeroPads;
        }

        private static Profile For(string bikeType)
        {
            Profile p = new Profile();
            if (IsTriathlon(bikeType))
            {
                // Aero position on the elbow pads. ArmFactor is the upper arm (acromion to elbow) as a share of arm length
                // (acromion to fist centre). Body reach is measured hip – shoulder – elbow: 70–75° puts the elbows about
                // under the shoulders at a 14–22° back angle. Pads sit above and a little behind the stem clamp.
                p.SeatAngle = 78; p.HeadTubeAngle = 73; p.StemLength = 70; p.StemAngle = -10; p.Spacers = 20; p.BarReach = -30;
                p.HandForward = 0; p.HandUp = 40;
                p.SaddleFactor = 0.883; p.ArmFactor = 0.49; p.BarWidth = 0;
                p.Performance = 14; p.Endurance = 18; p.Comfort = 22; p.DropBar = false; p.AeroPads = true;
                p.ReachPerformance = 75; p.ReachEndurance = 73; p.ReachComfort = 70;
            }
            else if (Is(bikeType, "Mountain"))
            {
                p.SeatAngle = 76; p.HeadTubeAngle = 65; p.StemLength = 45; p.StemAngle = 0; p.Spacers = 15; p.BarReach = 0;
                p.HandForward = -40; p.HandUp = 25; // grips sit behind and above the clamp (backsweep and rise)
                p.SaddleFactor = 0.875; p.ArmFactor = 0.82; p.BarWidth = 780;
                p.Performance = 40; p.Endurance = 44; p.Comfort = 48; p.DropBar = false;
                p.ReachPerformance = 75; p.ReachEndurance = 73; p.ReachComfort = 70;
            }
            else if (Is(bikeType, "Gravel"))
            {
                p.SeatAngle = 73.5; p.HeadTubeAngle = 71.5; p.StemLength = 90; p.StemAngle = -6; p.Spacers = 30; p.BarReach = 75;
                p.HandForward = 30; p.HandUp = 30; // top of the hood, ahead of and above the bar
                p.SaddleFactor = 0.880; p.ArmFactor = 0.97; p.BarWidth = 0;
                p.Performance = 42; p.Endurance = 46; p.Comfort = 50; p.DropBar = true;
                p.ReachPerformance = 85; p.ReachEndurance = 83; p.ReachComfort = 80;
            }
            else
            {
                p.SeatAngle = 73.5; p.HeadTubeAngle = 73; p.StemLength = 100; p.StemAngle = -6; p.Spacers = 25; p.BarReach = 80;
                p.HandForward = 30; p.HandUp = 30;
                p.SaddleFactor = 0.883; p.ArmFactor = 0.97; p.BarWidth = 0;
                p.Performance = 40; p.Endurance = 44; p.Comfort = 48; p.DropBar = true;
                p.ReachPerformance = 85; p.ReachEndurance = 83; p.ReachComfort = 80;
            }
            return p;
        }

        // Triathlon / TT as Cassette's fit protocols name it, plus the short forms a frame list may use.
        public static bool IsTriathlon(string bikeType)
        {
            string v = bikeType == null ? string.Empty : bikeType.Trim();
            return Is(v, "Triathlon / TT") || Is(v, "Triathlon") || Is(v, "Tri") || Is(v, "TT") || Is(v, "Time Trial");
        }

        private static bool Is(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        // Default back angle for a bike type, style and flexibility (degrees above horizontal).
        public static double DefaultBackAngle(string bikeType, string style, string flexibility)
        {
            Profile p = For(bikeType);
            double angle = Is(style, "Performance") ? p.Performance : Is(style, "Comfort") ? p.Comfort : p.Endurance;
            if (Is(flexibility, "Limited")) angle += 5;
            else if (Is(flexibility, "Good")) angle -= 4;
            return angle;
        }

        // Default body reach angle: road and gravel 80–85°, mountain 70–75° (hip – shoulder – wrist), triathlon / TT 70–75°
        // (hip – shoulder – elbow), more open for performance.
        public static double DefaultShoulderAngle(string bikeType, string style)
        {
            Profile p = For(bikeType);
            return Is(style, "Performance") ? p.ReachPerformance : Is(style, "Comfort") ? p.ReachComfort : p.ReachEndurance;
        }
        public static double DefaultStemLength(string bikeType) { return For(bikeType).StemLength; }
        public static double DefaultStemAngle(string bikeType) { return For(bikeType).StemAngle; }
        public static double DefaultSpacers(string bikeType) { return For(bikeType).Spacers; }
        public static double DefaultHeadTubeAngle(string bikeType) { return For(bikeType).HeadTubeAngle; }
        public static double DefaultBarReach(string bikeType) { return For(bikeType).BarReach; }
        public static double DefaultPadRise(string bikeType) { Profile p = For(bikeType); return p.AeroPads ? p.HandUp : 0; }

        public static bool TryCalculate(BikeSizingInput input, out BikeSizingResult result, out string error)
        {
            result = null;
            error = null;
            if (input == null) { error = "No measurements."; return false; }
            if (!(input.Height >= 1200 && input.Height <= 2200)) { error = "Height should be between 120 and 220 cm."; return false; }
            if (!(input.Inseam >= 0.38 * input.Height && input.Inseam <= 0.56 * input.Height)) { error = "Inseam looks wrong for this height (usually 44–50% of height). Measure floor to crotch with a book pressed up firmly."; return false; }
            if (!(input.SternalNotch >= 0.74 * input.Height && input.SternalNotch <= 0.88 * input.Height)) { error = "Floor to sternal notch looks wrong for this height (usually about 82% of height)."; return false; }
            if (!(input.ArmLength >= 0.30 * input.Height && input.ArmLength <= 0.45 * input.Height)) { error = "Arm length looks wrong for this height (shoulder bony point to centre of a closed fist is usually about 37% of height)."; return false; }

            Profile p = For(input.BikeType);
            BikeSizingResult r = new BikeSizingResult();
            r.BackAngle = Pick(input.BackAngle, DefaultBackAngle(input.BikeType, input.Style, input.Flexibility));
            r.ShoulderAngle = Pick(input.ShoulderAngle, DefaultShoulderAngle(input.BikeType, input.Style));
            r.StemLength = Pick(input.StemLength, p.StemLength);
            r.StemAngle = Pick(input.StemAngle, p.StemAngle);
            r.Spacers = Pick(input.Spacers, p.Spacers);
            r.HeadTubeAngle = Pick(input.HeadTubeAngle, p.HeadTubeAngle);
            r.BarReach = p.DropBar || p.AeroPads ? Pick(input.BarReach, p.BarReach) : 0;
            r.SeatAngle = p.SeatAngle;
            r.HandForward = p.HandForward;
            r.HandUp = p.AeroPads ? Pick(input.PadRise, p.HandUp) : p.HandUp;
            r.AeroPads = p.AeroPads;

            // Saddle height (LeMond 0.883 × inseam for road) and the seated hip joint just above the saddle.
            r.SaddleHeight = p.SaddleFactor * input.Inseam;
            double seat = Radians(p.SeatAngle);
            double saddleX = -r.SaddleHeight * Math.Cos(seat);
            double saddleY = r.SaddleHeight * Math.Sin(seat);
            double hipX = saddleX + 15;
            double hipY = saddleY + 0.05 * input.Height;

            // Hip joint to shoulder: the hip joint sits about 5% of height above the crotch, the shoulder at sternal-notch height.
            r.TorsoLength = input.SternalNotch - input.Inseam - 0.05 * input.Height;
            if (r.TorsoLength < 0.22 * input.Height || r.TorsoLength > 0.36 * input.Height)
                r.Warnings.Add("The torso length worked out from these measurements is unusual. Re-check inseam and floor-to-sternal-notch.");

            // Elbows are slightly bent on the bike (on the pads, only the upper arm counts). With a wide flat bar the hands are also out to the side, which shortens the arm in side view.
            double arm = p.ArmFactor * input.ArmLength;
            if (p.BarWidth > 0)
            {
                double shoulders = input.ShoulderWidth > 0 ? input.ShoulderWidth : 0.23 * input.Height;
                double sideways = Math.Max(0, (p.BarWidth - 60) / 2.0 - shoulders / 2.0);
                arm = Math.Sqrt(Math.Max(arm * arm - sideways * sideways, 1));
            }

            double handX, handY;
            Hand(hipX, hipY, r.TorsoLength, arm, r.BackAngle, r.ShoulderAngle, p.AeroPads, out handX, out handY);
            r.HandlebarX = handX;
            r.HandlebarY = handY;
            r.SaddleToHandDrop = saddleY - handY;

            // Range: back angle ±2° or body reach angle ±2.5°, one at a time (moving both to their extremes at once
            // spans about three frame sizes, which is too wide to choose a frame from).
            r.HandlebarXMin = r.HandlebarYMin = r.FrameStackMin = r.FrameReachMin = double.MaxValue;
            r.HandlebarXMax = r.HandlebarYMax = r.FrameStackMax = r.FrameReachMax = double.MinValue;
            double[] backs = new double[] { r.BackAngle - BackSpread, r.BackAngle + BackSpread, r.BackAngle, r.BackAngle };
            double[] shoulderAngles = new double[] { r.ShoulderAngle, r.ShoulderAngle, r.ShoulderAngle - ShoulderSpread, r.ShoulderAngle + ShoulderSpread };
            for (int k = 0; k < backs.Length; k++)
            {
                double back = backs[k], shoulder = shoulderAngles[k];
                double x, y, stack, reach;
                Hand(hipX, hipY, r.TorsoLength, arm, back, shoulder, p.AeroPads, out x, out y);
                Frame(x, y, r, out stack, out reach);
                r.HandlebarXMin = Math.Min(r.HandlebarXMin, x); r.HandlebarXMax = Math.Max(r.HandlebarXMax, x);
                r.HandlebarYMin = Math.Min(r.HandlebarYMin, y); r.HandlebarYMax = Math.Max(r.HandlebarYMax, y);
                r.FrameStackMin = Math.Min(r.FrameStackMin, stack); r.FrameStackMax = Math.Max(r.FrameStackMax, stack);
                r.FrameReachMin = Math.Min(r.FrameReachMin, reach); r.FrameReachMax = Math.Max(r.FrameReachMax, reach);
            }
            Frame(handX, handY, r, out r.FrameStack, out r.FrameReach);

            // Drop bars: centre-to-centre width about the shoulder width, rounded to the usual 20 mm steps.
            if (p.DropBar && input.ShoulderWidth > 0)
            {
                double width = Math.Round(input.ShoulderWidth / 20.0) * 20.0;
                if (Is(input.BikeType, "Gravel")) width += 20;
                r.BarWidth = Math.Max(340, Math.Min(500, width));
            }

            if (r.SaddleToHandDrop < -60)
                r.Warnings.Add("The hands come out well above the saddle. Check the back angle; a frame this tall may be hard to find.");
            if (p.AeroPads && r.SaddleToHandDrop < 30)
                r.Warnings.Add("The pads are close to saddle height. That is upright for a triathlon / TT position; check the back angle.");
            if (r.FrameReach < 330 || r.FrameStack < (p.AeroPads ? 440 : 470))
                r.Warnings.Add("The frame numbers are at the small end of what is made. A shorter stem or more spacers may be needed instead of a smaller frame.");
            result = r;
            return true;
        }

        // Contact point: the hand (hood or grip), or with aero pads the centre of the elbow pad (arm = upper arm).
        private static void Hand(double hipX, double hipY, double torso, double arm, double backAngle, double shoulderAngle, bool pads, out double x, out double y)
        {
            double back = Radians(backAngle);
            double shoulderX = hipX + torso * Math.Cos(back);
            double shoulderY = hipY + torso * Math.Sin(back);
            // The angle at the shoulder between the line down to the hip and the arm is 180 − back − (arm below horizontal).
            double down = Radians(180 - shoulderAngle - backAngle); // arm direction below horizontal
            x = shoulderX + arm * Math.Cos(down);
            y = shoulderY - arm * Math.Sin(down);
            if (pads)
            {
                x += PadForwardOfElbow;
                y -= PadBelowElbow;
            }
        }

        // Works back from the hand contact point through the bar, stem and spacers to the top of the head tube.
        private static void Frame(double handX, double handY, BikeSizingResult r, out double stack, out double reach)
        {
            double head = Radians(r.HeadTubeAngle);
            double upX = -Math.Cos(head), upY = Math.Sin(head);       // along the steerer, upward
            double perpX = Math.Sin(head), perpY = Math.Cos(head);    // square to the steerer, forward
            double stem = Radians(r.StemAngle);
            double dirX = Math.Cos(stem) * perpX + Math.Sin(stem) * upX;
            double dirY = Math.Cos(stem) * perpY + Math.Sin(stem) * upY;
            double clampX = handX - (r.BarReach + r.HandForward);
            double clampY = handY - r.HandUp;
            double stemX = clampX - r.StemLength * dirX;
            double stemY = clampY - r.StemLength * dirY;
            double along = r.Spacers + SpacerToStemCentre;
            reach = stemX - along * upX;
            stack = stemY - along * upY;
        }

        private static double Pick(double value, double fallback)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? fallback : value;
        }

        private static double Radians(double degrees) { return degrees * Math.PI / 180.0; }
    }
}
