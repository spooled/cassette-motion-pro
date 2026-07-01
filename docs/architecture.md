# Architecture

## Guiding rule

Keep the Kinovea video, capture, playback, drawing, and screen-management
projects intact wherever possible. New bike-fitting behavior should enter
through dedicated `CassetteMotionPro.*` namespaces and projects.

## Repository layout

- `src/` - imported Kinovea solution and Cassette Motion Pro integration code
- `branding/` - reproducible brand asset generator and source-size artwork
- `assets/` - general product artwork
- `docs/` - product and engineering documentation
- `installer/` - distribution notes; the active NSIS source remains under
  `src/Installer/` to preserve Kinovea's build layout

## Current integration boundary

Milestone 0.1 changes only the executable assembly, product metadata, window
identity, settings directory, installer identity, and presentation assets.
`CassetteMotionPro.Branding.BrandingAssets` owns the embedded assets so core
video projects do not depend on product-specific resource names.

## Planned modules

- `CassetteMotionPro.Clients` - client, bicycle, session, media, and note models
- `CassetteMotionPro.Workspace` - side, front, rear, and comparison layouts
- `CassetteMotionPro.Measurements` - reusable bike-fit overlays and templates
- `CassetteMotionPro.Reports` - branded PDF, image, and annotated-video exports
- `CassetteMotionPro.Analysis` - future landmark detection and tracking adapters

Persistence and domain interfaces should stay independent of WinForms. UI
projects can depend on those interfaces while the existing Kinovea projects
remain focused on video infrastructure.
