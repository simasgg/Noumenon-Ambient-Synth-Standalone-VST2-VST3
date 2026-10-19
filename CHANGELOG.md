# Changelog

Each release has a section here, newest first, under the version that `Directory.Build.props`
sets. `build-release.ps1` copies the section of the version it builds into
`dist/RELEASE-NOTES-<version>.md`, with the download sizes and SHA-256 hashes appended, for the
GitHub release page. Commits are titled `#<version> <summary>`.

## 0.5.0 — 2026-10-20

The first public release: the standalone application, the VST3 plugin (Noumenon and Noumenon FX in
one module) and the optional VST2 plugins, in one installer — everything 0.4.0 added (see its section of CHANGELOG.md), with
the fixes a code review of 0.4.0 found.

### Fixed

- Presets that moved into family folders no longer appear twice in the portable zips: the deploy
  folders now mirror the factory content instead of only adding to it.
- *Factory sample → Wavetables* loads the wavetable onto a key of its own instead of replacing
  the sample the sampler is playing, and switches the oscillators over only once it has loaded.
- An installed plugin finds the installed factory bank before any `content` folder near the DAW
  or its project, and a preset and the factory samples it names always come from the same folder.
- The VST2 plugins' Visual C++ runtime: the installer now bundles the v14 redistributable
  (14.51) and updates any older runtime it finds, instead of accepting any VC++ 2015-era one. A
  prerelease .NET 10 runtime no longer counts as installed.
- The morph target lists the presets in their families, like the preset menu, and marks factory
  presets with ★.
- The VST2 product version can no longer collide once a version component reaches 10.
- Release tooling: signed builds can run the sandboxed install test, which now also checks where
  each installed plugin finds its factory content and no longer meets a real installation on the
  same machine; artifacts left by an earlier run are no longer listed in the release notes; an
  empty changelog section gives empty notes.

## 0.4.0 — 2026-10-19

Release engineering (not published; 0.5.0 is the first public release): the standalone
application, the VST3 plugin (Noumenon and Noumenon FX in one module) and the optional VST2
plugins, in one installer.

### Added

- **Installer** (`Noumenon-0.4.0-Setup.exe`, Inno Setup): the standalone, the VST3 bundle in
  `Common Files\VST3`, optionally the VST2 plugins in `Common Files\VST2`, and the factory content
  once for all of them. It installs the .NET 10 Desktop Runtime (and, for the VST2 plugins, the
  Visual C++ runtime) when they are missing, and needs no internet connection. Portable zips for
  each format are published alongside it.
- **Factory bank**: 50 presets in six families — Drone, Pad, Texture, Rhythmic, Broken and FX —
  each with a description, all levelled to the same loudness (−20 LUFS) so switching presets
  does not jump in volume.
- **Factory samples**: 23 original samples synthesized for Noumenon — sustained tones, plucks and
  bells, field-like textures, 120 BPM loops and a heartbeat, broken digital material — and five
  wavetables for the oscillators' Wavetable wave. Right-click the sample display → *Factory
  sample* loads one.
- **Preset families**: subfolders of the factory and user banks show as submenus of the preset
  menu and of the morph target; `--preset` accepts `Family/Name`.
- **Portable sample references**: a factory sample is saved as `factory:samples/<file>`, so a
  snapshot that uses one opens in the standalone, the VST3 and the VST2 plugin, on any machine.
- An application icon, and the standalone's *Open Factory Content Folder*.

### Changed

- One version for every project and both plugin formats, set in `Directory.Build.props`.
- The VST3 plugin reports the project's web address as its vendor URL.

## 0.3.0 — 2026-10-16

Depth: Granular and Stretch sample modes, the Wavetable and Sub waves, MIDI learn, the modulation
matrix page, the chord sequencer with its scale-aware generator, snapshot morphing from the panel,
and the sidechain envelope follower.

## 0.2.1 — 2026-10-16

The VST3 plugin (NPlug): both identities in one module, sample-accurate parameters, MIDI mapping,
live latency, Steinberg's validator in the tests.

## 0.2.0 — 2026-10-14

The VST2 plugins (VST.NET): the instrument and the FX identity with audio input.

## 0.1.1 — 2026-10-11

The editor's own identity: the dark signal-flow panel with its live displays.

## 0.1.0 — 2026-10-10

The owner-drawn editor, inside the standalone.

## 0.0.5 — 2026-10-10

The standalone: WASAPI and ASIO output, MIDI input, recording and offline bounce.

## 0.0.4 — 2026-10-09

Living faders, the P1/P2 pitch layer with MIDI, the randomizer, presets.

## 0.0.3 — 2026-09-28

The effect chain and the post section.

## 0.0.2 — 2026-09-27

The sampler and the live input.

## 0.0.1 — 2026-09-27

The skeleton, the parameter table and the two oscillator sections.
