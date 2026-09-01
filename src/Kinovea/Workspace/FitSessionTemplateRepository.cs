/*
Copyright (C) 2026 Cassette Fit Studio.

This file is part of Cassette Motion Pro and is distributed under the
GNU General Public License version 2.
*/

using Kinovea.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CassetteMotionPro.Workspace
{
    [Serializable]
    public class FitSessionTemplate
    {
        public string Name { get; set; }
        public string BikeType { get; set; }
        public string MeasurementFocus { get; set; }
        public string GoalsPrompt { get; set; }
        public string MainGoalPrompt { get; set; }
        public string RecommendationPrompt { get; set; }
        public string FollowUpPrompt { get; set; }

        [XmlIgnore]
        public bool IsBuiltIn { get; set; }

        public override string ToString()
        {
            return Name ?? "Fit template";
        }
    }

    public class FitSessionTemplateRepository
    {
        private readonly string rootPath;
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(FitSessionTemplate));

        public FitSessionTemplateRepository()
        {
            rootPath = Path.Combine(Software.SettingsDirectory, "Fit Templates");
            Directory.CreateDirectory(rootPath);
        }

        public IList<FitSessionTemplate> LoadAll()
        {
            List<FitSessionTemplate> templates = new List<FitSessionTemplate>(BuildBuiltInTemplates());
            foreach (string path in Directory.GetFiles(rootPath, "*.xml"))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(path))
                    {
                        FitSessionTemplate template = serializer.Deserialize(stream) as FitSessionTemplate;
                        if (template != null && !string.IsNullOrWhiteSpace(template.Name))
                            templates.Add(template);
                    }
                }
                catch (InvalidOperationException) { }
                catch (IOException) { }
            }
            return templates.OrderBy(t => t.IsBuiltIn ? 0 : 1).ThenBy(t => t.Name).ToList();
        }

        public void Save(FitSessionTemplate template)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.Name))
                throw new ArgumentException("A template name is required.", "template");
            string path = Path.Combine(rootPath, MakeSafeFileName(template.Name) + ".xml");
            using (FileStream stream = File.Create(path))
                serializer.Serialize(stream, template);
        }

        public void Delete(FitSessionTemplate template)
        {
            if (template == null || template.IsBuiltIn || string.IsNullOrWhiteSpace(template.Name))
                return;
            string path = Path.Combine(rootPath, MakeSafeFileName(template.Name) + ".xml");
            if (File.Exists(path))
                File.Delete(path);
        }

        private static IList<FitSessionTemplate> BuildBuiltInTemplates()
        {
            return new List<FitSessionTemplate>
            {
                BuiltIn("Road Performance", "Road", "Contact points, sustainable torso position, hood reach, saddle position, and stable pedaling under road effort.", "Clarify event goals, typical ride duration, terrain, comfort limits, and desired balance of performance and endurance.", "Create a sustainable road position that supports the rider’s goals while preserving comfort, control, and repeatable power.", "Document the confirmed contact-point changes and any position cues the rider should use on longer rides.", "Recheck comfort and handling after several representative road rides and repeat the same video/measurement setup if symptoms or control concerns remain."),
                BuiltIn("Gravel / Adventure", "Gravel", "Control on variable terrain, braking access, seated stability, comfort over longer surfaces, and usable reach in multiple hand positions.", "Clarify terrain, event distance, luggage, surface roughness, hand comfort, and confidence while descending or braking.", "Balance all-day comfort and off-road control with an efficient position for the rider’s intended gravel use.", "Document control-focused contact-point changes and the hand positions the rider should test on mixed terrain.", "Reassess after representative mixed-surface rides, including braking, climbing, and rough-terrain feedback."),
                BuiltIn("Mountain Bike", "Mountain", "Standing/seated transitions, cockpit control, braking access, climbing posture, suspension context, and stable knee tracking.", "Clarify trail type, climbing/descending priorities, technical confidence, injury history, and seated versus standing concerns.", "Support confident bike control and efficient climbing while keeping seated and standing positions usable for the rider’s terrain.", "Document cockpit and saddle changes plus the trail situations the rider should use to evaluate them.", "Recheck on familiar trails after the rider has tested climbing, descending, braking, and standing movement."),
                BuiltIn("Triathlon / Time Trial", "Triathlon / TT", "Aerobar contact, hip posture, sustainable head/torso position, saddle support, pedaling clearance, and transition to running.", "Clarify race distance, aerobar tolerance, outdoor handling, power goals, run-off-bike priorities, and existing discomfort.", "Create a sustainable aerodynamic position that the rider can control and maintain for the intended event duration.", "Document aerobar, pad, extension, and saddle changes with the posture cues used during the final validation effort.", "Validate outdoors where appropriate and reassess aero comfort, control, power sustainability, and run response after adaptation."),
                BuiltIn("Hybrid / Comfort", "Hybrid / Comfort", "Easy control, hand comfort, upright support, saddle stability, joint comfort, and confidence starting/stopping.", "Clarify daily use, ride duration, mobility limits, hand/saddle discomfort, traffic confidence, and mounting or stopping concerns.", "Prioritize a comfortable, confidence-inspiring position for the rider’s everyday use and expected ride duration.", "Document the comfort and control changes plus simple posture or hand-position reminders.", "Recheck after normal everyday rides and adjust gradually if comfort or control concerns persist.")
            };
        }

        private static FitSessionTemplate BuiltIn(string name, string bikeType, string focus, string goals, string mainGoal, string recommendation, string followUp)
        {
            return new FitSessionTemplate
            {
                Name = name,
                BikeType = bikeType,
                MeasurementFocus = focus,
                GoalsPrompt = goals,
                MainGoalPrompt = mainGoal,
                RecommendationPrompt = recommendation,
                FollowUpPrompt = followUp,
                IsBuiltIn = true
            };
        }

        private static string MakeSafeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] characters = value.Trim().ToCharArray();
            for (int index = 0; index < characters.Length; index++)
            {
                if (invalid.Contains(characters[index]) || char.IsWhiteSpace(characters[index]))
                    characters[index] = '_';
            }
            return new string(characters).Trim('_');
        }
    }
}
