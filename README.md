# Cassette Motion Pro

Cassette Motion Pro is professional bike fitting software built on the
[Kinovea](https://github.com/Kinovea/Kinovea) video analysis engine. The project
keeps bike-fit-specific code and branding isolated so upstream Kinovea updates
can be incorporated with minimal changes to the playback and annotation engine.

## Current milestone: 0.15.0 Guided fit workflow navigation

- Complete Kinovea source imported under `src/`
- Application output renamed to `CassetteMotionPro.exe`
- Product, company, window title, application-data folder, and multi-window
  launch behavior branded for Cassette Motion Pro
- New application icon, splash screen, and About dialog artwork
- Branded report header with Cassette Motion Pro logo
- Windows installer and portable artifact names updated
- Windows build workflow provided at `.github/workflows/build.yml`
- Dedicated Client Manager with search and recent clients
- Persistent client, contact, bicycle, and notes records
- Automatic Videos, Photos, Side-by-Side, Reports, Measurements, and Notes folders
- One-click navigation from a client record into the existing video workflow
- Persistent fit sessions attached to each client
- Simple before and after video slots
- Video import into organized client-specific folders
- One-click synchronized before/after comparison
- Rider goals, fit notes, session status, and before/after bike measurements
- Saddle-tip-to-grip reach recorded before and after the fit
- Handlebar X and Handlebar Y recorded before and after the fit
- Guided bike-fit posture overlay for knee, hip, ankle, torso, shoulder, and
  elbow angles
- Reliable automatic overlay activation after the selected video finishes loading
- Persistent Before/After body-angle chart for every fit session
- Workspace sessions save automatically when closing the fit workspace
- Clear save message showing that sessions live in the client Measurements folder
- One-click HTML report generation saved to the client Reports folder
- One-click Reports folder access from the Bike Fit Workspace
- Printable report layout with before/after placeholders and change column
- Before and after report image selection saved with each fit session
- Report images copied into the client Photos folder and shown in reports
- Side-by-side report image selection shown full-width in generated reports
- Bike Metrics tab with Before/After inputs, measurement guidance, and Assist
  placeholders for future image-based capture
- Measurement reference image saved with each fit session and shown in reports
- Image Measurement Assistant foundation opened from Bike Metrics Assist
- One-button Before + After image combine for side-by-side reports and Bike
  Metrics measurement reference images
- Click-to-measure Bike Metrics assistant with image calibration, two-point
  measurement, and save-back to Before or After values
- Negative measurement support for cases like saddle setback
- Side-by-side-only report image workflow so Before/After images are optional
- Saddle setback Assist uses a horizontal-only distance measurement
- Zoom and pan controls in the Image Measurement Assistant for more precise clicks
- Manual signed measurement entry for values such as `-9 mm`
- Bike Metrics Assist opens saddle setback, handlebar X, handlebar Y, and
  saddle-tip-to-grip reach with the correct horizontal/vertical distance mode
- GitHub Actions publishes a combined Windows bundle artifact containing both
  the portable zip and installer executable
- Guided Landmark Capture calculates saddle height, saddle setback, saddle-tip-
  to-grip reach, handlebar X, and handlebar Y from four clicked bike landmarks
- Image Measurement Assistant uses bike-fitting tool labels: Distance, Distance
  (horizontal), and Distance (vertical)
- Windows bundle uploads an installer build status file so missing installer
  artifacts are easier to diagnose in future updates
- Guided Capture has a larger current-point prompt, Undo Last Point, Flip
  Setback Sign, and a save confirmation preview
- GitHub Actions installs NSIS through Chocolatey and calls the exact
  `makensis.exe` path to make installer builds more reliable
- Guided Capture supports an optional level reference line for tilted images,
  Recalculate Values, and explicit saddle setback convention
- Guided Capture saddle setback convention is behind BB = negative and in front
  of BB = positive
- Guided Capture saves measurement trace data and reports capture method, level
  reference status, and saddle setback convention in generated reports
- Fit Summary tab for polished main goal, key findings, changes made,
  recommendations, and follow-up plan
- Generated reports include a dedicated Fit Summary section when summary fields
  are filled in
- Preview Report button opens the generated report immediately for review
- Generated HTML reports include a non-printing review checklist for client name,
  images, bike metrics, and report view before saving/sending the PDF
- Windows builds explicitly package Kinovea's DrawingTools folder so the video
  player shows the drawing, distance, angle, and annotation toolbar.
- Overview tab includes a Fit Workflow checklist with ready/needs-step status
  and shortcuts for videos, analysis, Bike Metrics, report images, and preview.
- Overview tab now starts with a client-first fit path: confirm client details,
  capture/import videos, open Kinovea tools, save Bike Metrics, then generate
  the report from the client folder.
- Overview and Video Analysis wording now emphasize that the actual bike-fit
  measuring happens in the full Kinovea video workspace first, then photos,
  videos, Bike Metrics, and reports are saved back to the client session.
- Bike Fit Workspace bottom controls stay visible on smaller screens using a
  dedicated action button row
- Videos tab now labels video-opening actions as Analyze and explains that the
  drawing tools, timeline, playback controls, and joint controls appear in the
  main video player workspace
- Dedicated Video Analysis tab opens Before, After, or Before + After videos in
  the full player workspace where the bike-fit controls appear
- Report Package button creates a share-ready folder in the client Reports
  folder
- Report packages include `Bike Fit Report.html` and an `Images` folder with
  copied report images so the package can be reviewed from one place
- Zip button creates a zipped report package for easier sharing with
  clients or uploading to cloud storage
- Handoff tab records what to send, client follow-up message, homework/ride
  instructions, next appointment, and internal handoff notes
- Report packages and zipped packages include `Client Handoff Notes.txt` as a
  separate handoff/checklist file
- Review Metrics button on the Bike Metrics tab checks missing key bike
  measurements before reporting
- Review Metrics also flags broad out-of-range final values for saddle height,
  saddle setback, saddle-tip-to-grip reach, and handlebar X/Y
- Review Metrics now explains which side is missing, why a value is flagged,
  and the next action to take
- Saddle setback review explicitly reminds fitters that behind BB should be
  negative
- Report packages and zipped packages include `Bike Metrics Review.txt` with
  ready/needs-review status, missing values, double-check items, and advisory
  reminders
- Report packages and zipped packages include `README - Open This First.txt`
  with clear package-opening instructions
- Report package folder names use clearer separators and Cassette Motion Pro
  labeling
- Report packages and zipped packages include `Session Summary.txt` with a
  quick plain-text overview of the client, bike, fit summary, key bike metrics,
  body angles, notes, and handoff reminder
- Generated reports have a more polished professional layout with an upgraded
  cover/header, stronger section spacing, section cards, cleaner tables,
  improved Fit Summary presentation, and better print/PDF styling
- Generated reports include client-facing `Report prepared by`, studio contact
  placeholder, and confidential bike fit report wording in the header/footer
- Session Summary package text includes the prepared-by line for consistency
- Generated reports now show studio contact placeholders as separate Fitter,
  Phone, Email, and Website lines so the section is easier to customize later
- Session Summary package text also shows Fitter, Phone, Email, and Website
  placeholders
- Generated reports and Session Summary now show `Fitter: Cesar Correa`
- Before/After side-by-side images are saved into a dedicated client
  `Side-by-Side` folder with per-session organization
- Bike Fit Workspace includes a Client Files tab with one-click shortcuts to
  the client folder, Videos, Photos, Side-by-Side, Reports, Measurements, and
  Notes
- Client Manager now shows a simple fit workflow guide and uses a clearer
  Start Fit Session primary action
- Videos, report images, side-by-side images, reports, report packages, and
  zipped packages save into matching per-session client folders
- Startup splash screen now uses Cassette Motion Pro artwork instead of the
  upstream Kinovea splash
- Client Files tab can open the active session folder and active session
  Reports folder directly
- Report Images tab lets the fitter choose Full Cassette logo, CM badge, or no
  logo for generated reports
- Bike Fit Workspace header now shows the active client, active fit session,
  status, and the exact per-session folder where saved work belongs
- Save feedback now confirms the named session record was saved into the
  client’s Measurements → Sessions folder
- Client Files tab now includes active-session shortcuts for the session record,
  videos, photos, side-by-side images, and reports
- Active-session shortcuts save the current session first, create missing
  folders, and then open the exact folder for the current fit
- Client Files tab can add Before/After videos directly into the active fit
  session and update the Videos tab
- Client Files tab can add Before/After report photos directly into the active
  fit session and update the Report Images tab
- Bike Fit Workspace bottom action bar includes a Review button for a session
  readiness check before previewing or generating reports
- Session Review checks required report items such as Before/After videos and
  final Bike Metrics, while listing goals, summary, and report images as
  optional polish
- Bike Fit Workspace bottom controls now use a dedicated button row so Save,
  Review, Reports, Preview, Generate, Package, Zip, and Save & Close stay
  visible on smaller screens
- Opening Before, After, or Before + After analysis from the fit workspace now
  prepares an active session `Analysis Captures` folder so Kinovea captures
  have a clear client/session destination
- Video Analysis now includes an Open Captures Folder shortcut and clearer
  instructions for measuring in Kinovea first, then saving evidence back to the
  active client/session folder
- Fit Workflow now includes an Evidence saved step that turns ready when the
  active session Analysis Captures folder contains saved files
- Video Analysis includes a quick save guide for evidence, final numbers,
  report visuals, and client files
- Report Images tab can show or hide the Measurement Capture Trace section in
  generated reports without deleting the saved guided-capture data
- Body Angles now uses fitter-friendly Body reach and Back angle labels, with
  the elbow value removed from the visible workspace/report fields
- Video Analysis includes a Check Saved Evidence button and live status line so
  fitters can confirm Kinovea screenshots, exports, or clips landed in the
  active session Analysis Captures folder before moving to Bike Metrics/reporting
- Body Angles includes an in-tab guide for knee, hip, ankle, body reach, and
  back angle measurements so fitters know what to measure in Kinovea before
  entering report values
- Bike Metrics includes a workflow guide for opening Kinovea tools, saving
  evidence, recording final Before/After numbers, and reviewing the report
- Overview includes a Start Fit Workflow shortcut bar for jumping through Goals,
  Videos, Analysis, Bike Metrics, Body Angles, Report Images, and Preview

The expanded body-angle measurement library and polished PDF report generator
remain future milestones. See [docs/roadmap.md](docs/roadmap.md).

## Build on Windows

The application targets .NET Framework 4.8 WinForms and includes native and
C++/CLI projects. A Windows development environment is required.

1. Install Visual Studio 2022 with **.NET desktop development** and **Desktop
   development with C++**.
2. Include the .NET Framework 4.8 development tools, MSVC v143 x64/x86 build
   tools, and C++/CLI support.
3. Open `src/CassetteMotionPro.sln`.
4. Set the `Kinovea` project as the startup project.
5. Select `Release` and `x64`, then rebuild the solution.

The executable is produced at:

`src/Kinovea/bin/x64/Release/CassetteMotionPro.exe`

The same build is automated by GitHub Actions. Successful runs publish a
portable application and Windows installer as downloadable artifacts.

## Branding assets

Editable source artwork and its deterministic generator live in `branding/`.
Run `python branding/generate_brand_assets.py` with Pillow installed to rebuild
all PNG and ICO files used by the application.

## License and upstream attribution

Cassette Motion Pro is a modified Kinovea fork and remains licensed under the
GNU General Public License version 2. See [LICENSE](LICENSE). Copyright in the
original Kinovea source remains with Joan Charmant and other contributors.
Cassette Motion Pro additions are copyright 2026 Cassette Fit Studio.
