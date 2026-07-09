# Product roadmap

## 0.1 - Branding foundation

- [x] Import complete Kinovea source under `src/`
- [x] Rename executable and application identity
- [x] Replace icon, splash screen, and About dialog branding
- [x] Update installer and portable package identity
- [x] Add a repeatable Windows build workflow

## 0.2 - Client manager

- [x] Client, contact, bicycle, and notes records
- [x] Automatic photos, videos, measurements, notes, and reports folders
- [x] Recent clients and search
- [x] Durable XML storage isolated from the video engine
- [x] Direct navigation into the selected client's Videos folder

## 0.3 - Bike fit workspace

- [x] Persistent bike-fit sessions tied to each client
- [x] Simple left and right video organization
- [x] Automatic video import into the client folder
- [x] Synchronized left/right launch into the dual player
- [x] Rider goals, session notes, status, and before/after bike measurements

## 0.3.2 - Before/after comparison

- [x] Replace Left/Right session slots with Before/After
- [x] Import before and after media into the client folder
- [x] Open both videos in the synchronized dual player
- [x] Migrate existing Left/Right session paths when a session is reopened

## 0.3.3 - Fit measurement refinement

- [x] Add saddle-tip-to-grip reach to the before/after measurement chart

## 0.3.4 - Handlebar coordinates

- [x] Add Handlebar X and Handlebar Y to the before/after measurement chart

## 0.4 - Measurement library

- [x] Guided posture overlay for hip, knee, shoulder, elbow, ankle, and torso
- [x] Before/After body-angle values stored with each fit session
- [x] One-click launch from the fit workspace into the angle overlay
- Bicycle-fit tools: saddle height/setback, handlebar drop/reach, knee-over-pedal,
  plumb line, bottom bracket, pedal spindle, and foot tracking
- Reusable overlay templates

## 0.4.1 - Body-angle activation fix

- [x] Wait for the selected video to finish loading before activating the overlay
- [x] Show clear loading, ready, and failure status messages

## 0.4.2 - Safer workspace saving

- [x] Save the current fit session automatically when closing the workspace
- [x] Keep video launch and body-angle launch saving the session first
- [x] Show a clearer save message in the workspace footer

## 0.5 - Reports

- [x] Basic branded HTML report saved to the client Reports folder
- [x] Include session overview, goals, bike measurements, body angles, and notes
- [x] Open the client Reports folder from the Bike Fit Workspace
- [x] Add print/save-PDF button, before/after placeholders, and change column
- [x] Add before/after report image selection and include selected images in reports
- [x] Add full-width side-by-side report image support
- Branded PDF reports
- Before/after images, measurements, and recommendations
- Annotated image and video export

## 1.0 - Professional release

- Installer signing and release automation
- Production data backup and restore
- Accessibility, localization, performance, and field testing

## Future analysis

- Automatic body-landmark detection
- Automatic angle calculation and tracking
- Assisted report generation with fitter review
