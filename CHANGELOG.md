# Changelog

Each release has a section here, newest first, under the version that `Directory.Build.props`
sets. `build-release.ps1` copies the section of the version it builds into
`dist/Noumenon-<version>/RELEASE-NOTES-<version>.md`, with the download sizes and SHA-256 hashes
appended, for the GitHub release page. Commits are titled `#<version> <summary>`.

## 0.5.2 — 2026-10-22

Installing over an earlier version now removes the presets a release dropped, SAVE asks before it
writes over one of your presets, P1 and P2 keep their sliders with MIDI on, and the panel and the
About box credit Garda.

### Changed

- **SAVE asks first** when it would write over one of your own presets: *Save over “…”?*, with No
  as the default answer. That is the panel's SAVE button, in the standalone and both plugins, and
  the standalone's Ctrl+S. Saving under a new name asks nothing more than the Save dialog does.
- The wordmark on the MAIN page, in the standalone and both plugins, says Noumenon was
  **created by Garda**: click it to open
  [gardaofficial.bandcamp.com](https://gardaofficial.bandcamp.com/). The standalone's
  **Help → About** says the same, with the link.
- Release tooling: the sandboxed install test plants what an earlier install can leave behind —
  dropped presets, a portable copy's content, a preset of the player's own — and checks that only
  the first two go and that the installed content is exactly the release's; it uninstalls the
  sandbox even when a check fails.

### Fixed

- **Installing over an earlier version** removes the factory presets a release dropped — so far
  *Living Faders* and the FX family — once the install has succeeded. Only those files go:
  anything of your own in the installation folder stays. (0.5.1's notes promised this, but its
  installer did not do it.)
- A **portable copy** unpacked where the installer puts the plugins no longer hides the installed
  factory bank: the installer removes the `content` folder it left beside them, so the plugins
  list the same presets as the standalone.
- The standalone's **Save** dialog starts in your own folder, `Documents\Noumenon\Presets`, instead
  of the factory folder after *File → Open* on a factory preset.
- **P1 and P2 with MIDI on**: once a note had been played, the slider did nothing until MIDI was
  switched off. Now whichever moved last sets the pitch: a note takes the P fader over, and moving
  the slider takes it back until the next note. The bend wheel bends P whenever MIDI is on, even
  before the first note, and switching MIDI on no longer jumps to a note played earlier.
- Faders redraw their bar whenever what they play moves (modulation, MIDI, bend, glide), not
  only when the fader itself is moved, so the bar always shows where the value really is. While
  something else plays a fader — its lane, or a MIDI note on P1 or P2 — the white cap follows it
  too, and grabbing the cap there takes over from that point.

## 0.5.1 — 2026-10-22

A sample library on the SAMPLE page, five new drones and a fix for modulation-matrix slots aimed
at P1 or P2; the standalone retires its Play button and Party mode.

### Added

- A **sample library** on the SAMPLE page: every factory sample and wavetable, and your own
  samples in `Documents\Noumenon\Samples`, listed whatever preset is loaded. Click one to load it
  onto the sampler's key (a wavetable onto a key of its own, as the oscillators' table); drop
  files on the list to copy them into your folder, which every host shares. The waveform's
  right-click menu offers the same library as *Factory samples* and *My samples*.
- **Five drones** with Lithuanian names in the Drone family: *Ilgesys* (a dark, flat-tuned B
  minor drone whose bass and inner voice swing between two chords under a drifting high
  melody), *Laumžirgis* (a glowing B minor cluster with a stretched choir inside), *Gervės* (a low
  B drone in slowed-down wind, with gliding calls), *Svaigulys* (a C# minor pad swaying on a tape
  wow, its filters snapping open twice a chord) and *Perkūno miegas* (a motionless wall of low
  saws).

### Changed

- The factory bank drops *Living Faders* and the FX family and gains the five drones: 48
  presets, Init and five families.
  Noumenon FX processes a track with any preset once the sampler's S/IN is turned to the input.
- The SAMPLE page's lower row is WAVETABLE, LIBRARY and KEYS, under PLAYBACK, GRANULAR and
  STRETCH.
- The standalone's **Play** button (and F8) is gone: it only fed *Restart On Host Play*, which
  needs a DAW, and the RESTART button already realigns the loops. The **BPM** box stays, as the
  tempo for Host Sync and bounces. The readout under the wordmark now says **SYNC 120 BPM** when
  Host Sync follows a tempo without a running transport.
- The play-state readout in TRANSPORT is gone; it repeated the one under the wordmark.
- The standalone's *Party* mode (randomize on a timer, F10) is gone. **RANDOMIZE** (R) and
  *Presets → Random Factory Preset* (Ctrl+R) stay.
- While recording, the standalone's **● Rec** button turns red, shows the elapsed time and reads
  **Save Rec**: clicking it stops and saves the WAV.
- Release tooling: each release goes into a folder of its own, `dist\Noumenon-<version>\`, so the
  artifacts of earlier releases stay apart.

### Fixed

- A modulation-matrix slot aimed at **P1** or **P2** now bends what the fader reaches — the
  oscillators routed to it, the sampler and the RESONATOR (pitch and chord select). The engine
  read the P faders before the matrix, so such a slot moved the fader's display but not the
  sound; the P readout on MAIN now includes the matrix too.

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
