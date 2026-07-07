# Installer

The active NSIS definition remains at `src/Installer/kinovea.nsi` so it can use
Kinovea's established relative build paths. It produces a branded
`CassetteMotionPro-0.5.1.exe` installer from the Release x64 build.

GitHub Actions builds and publishes the installer automatically. To build it
locally, install NSIS after compiling `src/CassetteMotionPro.sln`, then run:

`makensis src\Installer\kinovea.nsi`
