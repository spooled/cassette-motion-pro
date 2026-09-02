/* Copyright (C) 2026 Cassette Fit Studio. GPL-2.0 */
using System.Collections.Generic;

namespace CassetteMotionPro.Workspace
{
    public class FitProtocolStep
    {
        public string Id { get; set; }
        public string Stage { get; set; }
        public string Title { get; set; }
        public string Guidance { get; set; }
    }

    public class FitProtocol
    {
        public string BikeType { get; set; }
        public string Summary { get; set; }
        public IList<FitProtocolStep> Steps { get; set; }
        public override string ToString() { return BikeType; }
    }

    public static class FitProtocolCatalog
    {
        public static IList<FitProtocol> LoadAll()
        {
            return new List<FitProtocol>
            {
                Protocol("Road", "Performance, sustainable posture, contact points, and repeatable power.",
                    Step("intake", "INTAKE", "Confirm riding demands", "Record event goals, terrain, duration, discomfort, injury history, and priorities."),
                    Step("baseline", "SETUP", "Document the starting bike", "Confirm bike size, crank length, saddle, shoes, cleats, and contact points."),
                    Step("before", "CAPTURE", "Record the Before position", "Capture a steady side view under representative road effort."),
                    Step("saddle", "FIT", "Establish saddle support", "Review height, setback, pelvic stability, knee tracking, and foot control."),
                    Step("cockpit", "FIT", "Review road cockpit", "Check hood reach, bar drop, wrist position, braking access, and torso position."),
                    Step("validate", "VALIDATE", "Validate under load", "Recheck cadence, posture, comfort, control, and repeatability at useful effort."),
                    Step("after", "CAPTURE", "Record the After position", "Use the same view and camera setup as Before."),
                    Step("report", "REPORT", "Finish recommendations", "Save evidence, final measurements, position cues, and follow-up.")),
                Protocol("Gravel", "All-day comfort, mixed-surface control, braking access, and seated stability.",
                    Step("intake", "INTAKE", "Confirm gravel demands", "Record surface, distance, luggage, climbing, descending, and comfort concerns."),
                    Step("baseline", "SETUP", "Document bike and equipment", "Confirm tires, shoes, cleats, cranks, bar shape, flare, and luggage context."),
                    Step("before", "CAPTURE", "Record the Before position", "Capture seated pedaling and the most-used hand positions."),
                    Step("saddle", "FIT", "Establish seated stability", "Review support, height, setback, pressure, and rough-terrain stability."),
                    Step("control", "FIT", "Review control positions", "Check hoods, drops, braking reach, wrists, and position changes."),
                    Step("validate", "VALIDATE", "Validate mixed-surface posture", "Check comfort, breathing, control, and sustainable reach."),
                    Step("after", "CAPTURE", "Record the After position", "Repeat the key views with matching camera alignment."),
                    Step("report", "REPORT", "Finish recommendations", "Document changes, terrain cues, evidence, and follow-up.")),
                Protocol("Mountain", "Seated efficiency, standing movement, braking access, and trail control.",
                    Step("intake", "INTAKE", "Confirm trail demands", "Record discipline, terrain, climbing, descending, confidence, and symptoms."),
                    Step("baseline", "SETUP", "Document bike configuration", "Confirm travel, sag context, cranks, cockpit, controls, shoes, and pedals."),
                    Step("before", "CAPTURE", "Record the Before position", "Capture seated pedaling and a usable standing position."),
                    Step("saddle", "FIT", "Review seated position", "Check support, pedaling clearance, knee tracking, and climbing posture."),
                    Step("control", "FIT", "Review cockpit and controls", "Check bar position, brake reach, wrists, and standing freedom."),
                    Step("validate", "VALIDATE", "Validate seated and standing", "Recheck climbing posture, braking, bike movement, and confidence."),
                    Step("after", "CAPTURE", "Record the After position", "Repeat seated and standing evidence using matching views."),
                    Step("report", "REPORT", "Finish recommendations", "Document changes, suspension context, trail tests, and follow-up.")),
                Protocol("Triathlon / TT", "Sustainable aerodynamics, aerobar support, hip clearance, and race-duration control.",
                    Step("intake", "INTAKE", "Confirm race demands", "Record distance, duration, aero tolerance, handling, power, and run priorities."),
                    Step("baseline", "SETUP", "Document aero contact points", "Confirm saddle, pad stack/reach/width, extensions, cranks, shoes, and cleats."),
                    Step("before", "CAPTURE", "Record the Before aero position", "Capture representative power after the rider settles in aero."),
                    Step("saddle", "FIT", "Establish aero saddle support", "Review pelvic support, hip clearance, stability, and pedaling."),
                    Step("aero", "FIT", "Review aerobar position", "Check pad support, extension reach, head/torso posture, breathing, and control."),
                    Step("validate", "VALIDATE", "Validate race posture", "Confirm sustainable aero posture at useful power without losing control."),
                    Step("after", "CAPTURE", "Record the After aero position", "Repeat the same effort, camera view, and settling time."),
                    Step("report", "REPORT", "Finish recommendations", "Document aero changes, adaptation, outdoor validation, and follow-up.")),
                Protocol("Hybrid / Comfort", "Comfort, confidence, easy control, joint tolerance, and everyday usability.",
                    Step("intake", "INTAKE", "Confirm everyday needs", "Record duration, mobility, mounting, stopping, confidence, and discomfort."),
                    Step("baseline", "SETUP", "Document the starting bike", "Confirm saddle, bar, controls, pedals, shoes, and normal use."),
                    Step("before", "CAPTURE", "Record the Before position", "Capture relaxed pedaling plus starting and stopping if relevant."),
                    Step("support", "FIT", "Establish comfortable support", "Review saddle stability, joints, reach, hand pressure, and control."),
                    Step("controls", "FIT", "Review access and confidence", "Check braking, shifting, steering, mounting, and stopping."),
                    Step("validate", "VALIDATE", "Validate normal riding", "Recheck comfort and control at the rider's expected pace."),
                    Step("after", "CAPTURE", "Record the After position", "Repeat useful views with matching camera placement."),
                    Step("report", "REPORT", "Finish recommendations", "Document changes, simple cues, adaptation, and follow-up."))
            };
        }

        private static FitProtocol Protocol(string bikeType, string summary, params FitProtocolStep[] steps)
        { return new FitProtocol { BikeType = bikeType, Summary = summary, Steps = steps }; }

        private static FitProtocolStep Step(string id, string stage, string title, string guidance)
        { return new FitProtocolStep { Id = id, Stage = stage, Title = title, Guidance = guidance }; }
    }
}
