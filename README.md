# Cassette Motion Pro

Cassette Motion Pro is professional bike fitting software built on the
[Kinovea](https://github.com/Kinovea/Kinovea) video analysis engine. The project
keeps bike-fit-specific code and branding isolated so upstream Kinovea updates
can be incorporated with minimal changes to the playback and annotation engine.

## Current milestone: 0.7.14 version label cleanup

- Complete Kinovea source imported under `src/`
- Application output renamed to `CassetteMotionPro.exe`
- Product, company, window title, application-data folder, and multi-window
  launch behavior branded for Cassette Motion Pro
- New application icon, splash screen, and About dialog artwork
- Windows installer and portable artifact names updated
- Windows build workflow provided at `.github/workflows/build.yml`
- Dedicated Client Manager with search and recent clients
- Persistent client, contact, bicycle, and notes records
- Automatic Videos, Photos, Reports, Measurements, and Notes folders
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
