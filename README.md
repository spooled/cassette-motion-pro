# Cassette Motion Pro

Cassette Motion Pro is professional bike fitting software built on the
[Kinovea](https://github.com/Kinovea/Kinovea) video analysis engine. The project
keeps bike-fit-specific code and branding isolated so upstream Kinovea updates
can be incorporated with minimal changes to the playback and annotation engine.

## Current milestone: 0.58.0 Backup, Restore, and Client-data Portability

- Options now includes Backup & Data Transfer for full-studio backups and single-client
  transfer packages.
- Full backups include client folders, sessions, videos, images, measurements, reports,
  follow-ups, studio settings, custom branding, fit templates, and camera profiles.
- Restore accepts only validated Cassette Motion Pro full-backup ZIPs and automatically
  creates a dated safety backup before replacing current data.
- A selected client can be exported with their complete folder and imported on another
  Cassette Motion Pro computer.
- Duplicate imports offer Replace, Keep Both, or Cancel without silently overwriting a
  client.
- ZIP extraction checks file paths before writing so malformed packages cannot write
  outside the temporary restore area.

## Previous milestone: 0.57.0 Studio Settings and Report Branding

- Options now includes a simple Studio Settings screen for the studio name, fitter
  name, phone, email, website, and report subtitle.
- Contact details are saved once for the studio and reused across newly generated
  reports, summaries, handoff files, and client packages.
- Optional contact fields can be left blank and are omitted from client-facing output
  instead of showing unfinished placeholders.
- A custom PNG or JPG studio logo can replace the built-in Cassette Motion Pro report
  logo while each fit still controls Full Logo, CM badge, or No Logo.
- Settings persist between app launches without changing client folders or fit data.

## Previous milestone: 0.56.0 Fit Search, Filters, and Client Management

- Client Fits now searches client names, bikes, email, phone, fit titles, fit status,
  bike type, and follow-up status.
- Filters highlight follow-ups due, clients needing attention, fits in progress, and
  clients with completed fits.
- Sorting supports recently opened clients, newest fit, next follow-up date, and name.
- Each row shows the latest fit date and follow-up state, with overdue or attention
  items visually highlighted.
- Follow-ups can be added directly to a client's latest fit from Client Fits.
- Clients can be archived and restored without deleting their folders, sessions,
  videos, measurements, reports, or follow-up history.

## Previous milestone: 0.55.0 Client-ready Report and PDF Polish

- Reports now include a compact final-position snapshot for the five key bike
  contact-point measurements.
- Missing image slots are omitted from client reports instead of printing unfinished
  "Image not added yet" placeholders.
- Before and After images use cleaner matched presentation and client-facing labels.
- Measurement changes use neutral styling so an increase or decrease is not presented
  as automatically good or bad.
- Letter-size print rules improve margins, color reproduction, table continuity, image
  sizing, and page-break behavior when using Print / Save PDF.
- Reports include a preparation date and short report reference for easier client filing.
- Internal "review before sending" prompts and empty optional sections are omitted from
  the client-facing file, keeping exported PDFs clean and intentional.

## Previous milestone: 0.54.0 Client Follow-up and Adaptation Tracking

- Client History can now add dated follow-up entries to any saved fit.
- Each check-in records adaptation status, comfort score, rides completed, rider
  feedback, symptoms or concerns, fitter actions, and an optional next check-in.
- Multiple check-ins remain attached to the original fit instead of replacing the
  previous notes.
- The latest adaptation status appears in the history list, while the full follow-up
  timeline appears in the selected fit summary.
- Follow-up records stay inside the client's saved fit data and do not alter videos,
  measurements, evidence, or the original report.

## Previous milestone: 0.53.0 Guided Fit Protocols

- Session Setup includes guided fit protocols for Road, Gravel, Mountain,
  Triathlon / TT, and Hybrid / Comfort bikes.
- Each protocol follows intake, setup, Before capture, bike-specific fitting,
  validation, After capture, and reporting.
- Every step includes concise guidance and a visible progress count.
- Progress is stored with the active client fit session and returns when reopened.
- A fitting template can select its matching protocol for a new session without
  replacing a protocol already chosen by the fitter.

## Previous milestone: 0.52.0 Camera Setup Profiles

- Video Studio now includes reusable dual-camera profiles for common Side + Front,
  Drive + Non-drive, and Side + Rear fitting setups.
- Each profile records the left/right camera roles, device labels, resolution, frame
  rate, and setup notes while leaving Kinovea's native camera controls intact.
- Fitters can save and delete custom studio profiles and reuse them with any client.
- Record Before and Record After each open two capture screens, save both camera
  angles into the selected phase folder, and create clear camera-role filenames.
- The active profile and screen roles are saved with the fit session, and a camera
  setup note is placed alongside the captured media for repeatable setup.

## Previous milestone: 0.51.0 Fit Day Dashboard and Navigation Cleanup

- Fit Day now opens on a simple dashboard with four stages: Client + Session,
  Video Studio, Measurements, and Report.
- One primary action changes automatically to the next useful step for the active fit.
- A compact readiness summary shows session, Before, After, evidence, metrics, and
  report-image progress without opening several tabs.
- Session details, goals, and fitting templates now live together on a focused
  Session Setup screen.
- Duplicate command-center controls were removed from the setup screen, while folder
  shortcuts remain available under More Options + Folders.
- The simplified dashboard scrolls on smaller laptops and keeps all Video Studio
  capture, playback, drawing, and measurement tools unchanged.

- A shared Cassette Motion Pro design system now controls product colors,
  typography, surfaces, borders, tabs, lists, inputs, and button states.
- Client Fits, New Client, and Fit Day use a consistent dark-and-lime identity
  with cleaner spacing and polished product headers.
- Fit Day has a redesigned top navigation with high-contrast selected tabs and a
  visible lime progress accent.
- Client and session lists, search, forms, and action buttons now read as one
  cohesive application instead of separate Kinovea add-on screens.
- Kinovea capture, playback, drawing, and measurement tools remain unchanged and
  fully available inside Video Studio.

- A Repeat Fit shortcut creates and saves a fresh session from the active fit.
- Client History also offers Start Repeat Fit from any selected saved session.
- The previous template, rider goals, recommendations, and follow-up context carry
  forward so the fitter can prepare quickly.
- Videos, images, and measurements stay blank so every repeat fit uses fresh evidence.
- The new session remembers which previous fit it came from and immediately has its
  client-specific save folders ready for Video Studio.

- A read-only Client History workspace lists the client’s other saved fit sessions.
- Fitters can compare the selected previous fit’s final bike and rider measurements
  against the active fit’s Before and final values.
- Previous goals, changes, recommendations, follow-up, template, date, and status
  stay visible beside the measurement comparison.
- Previous sessions and their folders can be opened directly from the history view.

- Fit Day includes reusable fitting templates for Road, Gravel, Mountain Bike,
  Triathlon, and Hybrid/Comfort workflows.
- Applying a template can replace the current Fit Summary draft or fill only
  empty fields, while preserving all client videos, images, and measurements.
- Fitters can save their current summary language as a custom global template,
  reuse it with another client, and remove custom templates when no longer needed.
- Each fit session remembers which template was applied for consistent follow-up.

- Fit Day and Client Manager now use a visible CM badge and Cassette Motion Pro branded headers.
- The main analysis area is presented to fitters as Video Studio instead of Kinovea Video.
- Fitter-facing workflow guidance now uses Cassette Motion Pro language from client setup through measurement and report output.
- Client Manager is presented as Client Fits and its video shortcut opens Video Studio.
- Kinovea remains credited as the underlying open-source engine in licensing and attribution while the product interface leads with Cassette Motion Pro.

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
- Analyze buttons now force Kinovea into playback analysis mode instead of
  leaving live capture screens open
- Packaged drawing tools quiet technical point/segment labels so measurement
  tools do not show construction labels such as P0/S0 over the bike
- Video workflow includes Use Latest Both so the newest Before and After live
  recordings can be selected together before side-by-side playback analysis
- Dual Live Capture now opens Kinovea as two actual capture screens for the
  active session's Before and After recording folders
- Before/After Record Live shortcuts now route into the same two-screen live
  capture setup so fit recording stays in the dual-camera workflow
- Video Capture + Analysis shows the active session's Before and After
  recording folders directly in the workspace before opening live capture
- Recording folder guide waits until the fit session is loaded, preventing
  startup crashes when opening a client fit session
- Video Capture + Analysis now shows a five-step Fit Day Path guide: Client,
  Record, Analyze, Save, and Report
- Overview workflow wording now more clearly separates recording/analyzing in
  Kinovea from saving evidence, Bike Metrics, and report content in the
  workspace
- Fit Command Center on the Overview tab puts Record Before, Use Latest Before,
  Record After, Use Latest After, side-by-side analysis, capture folders, and
  report image shortcuts in one simple fit-day dashboard
- Before/After video rows now include Record Live shortcuts that open Kinovea
  capture pointed at the active session’s Before or After video folder
- Before/After video rows include Use Latest buttons that select the newest
  recording saved in that session folder without browsing through files
- Client Files is organized into client folders, active fit session folders,
  and quick actions with direct Before/After video, report image, reports, and
  package folder shortcuts
- Next recommended step now includes a stage-specific folder shortcut so the
  active Before/After video folder, Analysis Captures, report images, session
  record, or Reports folder is one click away
- Overview workflow path now highlights the live-fit sequence: record live, use
  latest, analyze in Kinovea, save Bike Metrics, then generate the report
- Windows builds explicitly package Kinovea's DrawingTools folder so the video
  player shows the drawing, distance, angle, and annotation toolbar.
- Overview tab includes a Fit Workflow checklist with ready/needs-step status
  and shortcuts for videos, analysis, Bike Metrics, report images, and preview.
- Overview tab now starts with a client-first fit path: confirm client details,
  capture/import videos, open Kinovea tools, save Bike Metrics, then generate
  the report from the client folder.
- Overview and video workflow wording now emphasize that the actual bike-fit
  measuring happens in the full Kinovea video workspace first, then photos,
  videos, Bike Metrics, and reports are saved back to the client session.
- Bike Fit Workspace bottom controls stay visible on smaller screens using a
  dedicated action button row
- Video Capture + Analysis tab labels video-opening actions as Analyze and explains that the
  drawing tools, timeline, playback controls, and joint controls appear in the
  main video player workspace
- Video Capture + Analysis opens Before, After, or Before + After videos in
  the full player workspace where the bike-fit controls appear, while keeping
  Record Live, Browse, Analyze, comparison, and saved-evidence actions together
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
  session and update the Video Capture + Analysis tab
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
- Video Capture + Analysis now includes an Open Captures Folder shortcut and clearer
  instructions for measuring in Kinovea first, then saving evidence back to the
  active client/session folder
- Fit Workflow now includes an Evidence saved step that turns ready when the
  active session Analysis Captures folder contains saved files
- Video Capture + Analysis includes a quick save guide for evidence, final numbers,
  report visuals, and client files
- Report Images tab can show or hide the Measurement Capture Trace section in
  generated reports without deleting the saved guided-capture data
- Body Angles now uses fitter-friendly Body reach and Back angle labels, with
  the elbow value removed from the visible workspace/report fields
- Video Capture + Analysis includes a Check Saved Evidence button and live status line so
  fitters can confirm Kinovea screenshots, exports, or clips landed in the
  active session Analysis Captures folder before moving to Bike Metrics/reporting
- Body Angles includes an in-tab guide for knee, hip, ankle, body reach, and
  back angle measurements so fitters know what to measure in Kinovea before
  entering report values
- Bike Metrics includes a workflow guide for opening Kinovea tools, saving
  evidence, recording final Before/After numbers, and reviewing the report
- Overview includes a simplified four-stage workflow shortcut bar: Client Info,
  Capture + Measure, Fit Results, and Report
- Overview now shows a Next recommended step coach that updates as goals,
  videos, saved evidence, Bike Metrics, and report images are completed
- The Next recommended step coach now includes a single action button that jumps
  directly to Goals, Video Capture + Analysis, Bike Metrics, Report Images, or Preview
  so the fit workspace feels less cluttered and easier to follow
- The Fit Workflow checklist is grouped under the same four stages so the
  workspace feels closer to the real fitting path: client setup, Kinovea capture,
  results entry, then report review
- Video Capture + Analysis now has a dedicated Prepare Capture Folder button, and the
  workflow/analysis shortcuts prepare the active session’s Analysis Captures
  folder before measuring so saved screenshots, exports, and clips have a
  clearer client/session destination
- Videos and Video Analysis are merged into one Video Capture + Analysis tab so
  recording live clips, choosing final Before/After videos, analyzing in
  Kinovea, comparing side-by-side, and checking saved evidence happen in one
  place
- Dual Live Capture and Dual Playback Analysis shortcuts prepare the session's
  Before/After recording folders and open the before/after analysis flow from
  the command center and video workflow.

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
