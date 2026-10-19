using Noumenon.Dsp.Oscillators;
using Noumenon.Engine.Parameters;
using static Noumenon.Engine.Parameters.SectionId;

namespace Noumenon.Factory.Bank;

/// <summary>The Pad family: patches to play from a keyboard — gated with slow envelopes, or chords picked by the keys — with P1 following the notes.</summary>
internal static partial class FactoryBank
{
    private static Recipe[] Pads =>
    [
        new("Pad", "Velvet Field", 201,
            "A soft, wide pad to play: saws and triangles a few cents apart under a gentle low-pass, a slow attack and a long release. P1 follows the keys, so slide between notes.",
            p => p
                .Gated(1.2f, 5f).Pitch(1, 0f, midi: true, glide: 0.15f)
                .A(1, Waveform.Saw, -12, -6f, 0.35f).A(2, Waveform.Saw, -12, 7f, 0.35f).A(3, Waveform.Triangle, 0, -3f, 0.3f).A(4, Waveform.Saw, -24, 0f, 0.25f).Off(A, 5, 6)
                .B(1, Waveform.Triangle, -12, 4f, 0.3f).B(2, Waveform.Saw, 7, -5f, 0.12f).B(3, Waveform.Sine, -24, 0f, 0.3f).Off(B, 4, 5, 6)
                .Filter(A, 2200f, 0.15f).Filter(B, 3000f).Combine(0.08f)
                .Drift(14f, 21f, 0.15f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.35f, size: 0.7f)
                .Lane(ParamId.SectionACutoff, 19f, 0.08f)),

        new("Pad", "Paper Lanterns", 202,
            "The chord sequencer walks I, VI, IV, V in C major, eight seconds a chord; the resonator rings each one over a soft drone, and the sequencer carries its root into P1, so the drone follows.",
            p => p
                .Scale("Major").Sequencer(8f, "Triad", "Open", 3, "P1", "I", "VI", "IV", "V").Pitch(1, 0f, glide: 1.2f)
                .A(1, Waveform.Triangle, -12, 0f, 0.4f).A(2, Waveform.Sine, 0, 0f, 0.3f).A(3, Waveform.Sine, 7, 2f, 0.2f).Off(A, 4, 5, 6)
                .B(1, Waveform.Sine, -24, 0f, 0.35f).B(2, Waveform.Triangle, -5, -3f, 0.2f).Off(B, 3, 4, 5, 6)
                .Filter(A, 2500f).Filter(B, 2000f).Combine(0.05f)
                .Resonator("Voices", 0.88f, 0.45f).Reverb("Hall", 0.4f)),

        new("Pad", "Halo Strings", 203,
            "A string machine to play: the Tape Strings sample looped under detuned saws. Keys move P1, and P1 carries the oscillators and the sample together.",
            p => p
                .Gated(0.8f, 4f).Pitch(1, 0f, midi: true, glide: 0.08f)
                .Sample(1, "Tape Strings.wav").Sampler(1, amp: 0.5f, mod: "P1").Loop(0.1f, 0.9f, 200f)
                .A(1, Waveform.Saw, -12, -8f, 0.3f).A(2, Waveform.Saw, -12, 8f, 0.3f).A(3, Waveform.Saw, 0, 0f, 0.2f).Off(A, 4, 5, 6)
                .B(1, Waveform.Pulse, -12, 0f, 0.2f, shape: 0.35f).Off(B, 2, 3, 4, 5, 6)
                .Filter(A, 3000f).Filter(B, 2500f).Combine(0.05f)
                .Drift(18f, 26f, 0.2f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.3f)),

        new("Pad", "Northern Hymn", 204,
            "Each key picks a chord: the resonator's chord select follows P1 (C#3 and up play chords 1, 2, 3 …) and rings it over a hushed drone that stays where it is. Play slowly.",
            p => p
                .Pitch(1, 0f, midi: true).RoutesAll(false, false).Choose(ParamId.ResochordChordSelect, "P1")
                .A(1, Waveform.Sine, -24, 0f, 0.5f).A(2, Waveform.Triangle, -12, 3f, 0.3f).A(3, Waveform.NoiseLp, 0, 0f, 0.3f, noiseCutoff: 1500f).Off(A, 4, 5, 6)
                .B(1, Waveform.Sine, -17, -2f, 0.3f).B(2, Waveform.NoiseLp, 0, 0f, 0.2f, noiseCutoff: 3000f).Off(B, 3, 4, 5, 6)
                .Filter(A, 2500f).Filter(B, 3500f).Combine(0.05f)
                .Resonator("Voices", 0.92f, 0.55f).Reverb("Cathedral", 0.45f)),

        new("Pad", "Dust Choir", 205,
            "The Vowel Morph wavetable sings from 'ah' to 'oo' as three LFOs scan it; gated, with a slow attack. P1 plays the vowels.",
            p => p
                .Gated(1.5f, 4.5f).Pitch(1, 0f, midi: true, glide: 0.1f)
                .Sample(10, "wavetables/Vowel Morph.wav").Wavetable(10)
                .A(1, Waveform.Wavetable, -12, -5f, 0.35f, shape: 0.3f).A(2, Waveform.Wavetable, -12, 6f, 0.35f, shape: 0.6f).A(3, Waveform.Wavetable, 0, 0f, 0.2f, shape: 0.5f).Off(A, 4, 5, 6)
                .B(1, Waveform.Sine, -24, 0f, 0.3f).B(2, Waveform.Wavetable, 7, 3f, 0.12f, shape: 0.4f).Off(B, 3, 4, 5, 6)
                .Filter(A, 4000f).Filter(B, 3000f).Combine(0.05f)
                .Lfo(1, 0.12f, "Triangle").Lfo(2, 0.07f).Lfo(3, 0.2f, "Smooth Random")
                .Mod("LFO 1", ParamId.OscA1Shape, 0.35f).Mod("LFO 2", ParamId.OscA2Shape, 0.35f).Mod("LFO 3", ParamId.OscA3Shape, 0.3f)
                .Drift(20f, 30f, 0.2f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.35f)),

        new("Pad", "Snowlight", 206,
            "Bright and high: two pulse waves whose widths sway, a sine an octave above, a plate reverb and a slow swell. Best played above middle C.",
            p => p
                .Gated(2.5f, 6f).Pitch(1, 0f, midi: true, glide: 0.1f)
                .A(1, Waveform.Pulse, 0, -4f, 0.3f, shape: 0.3f).A(2, Waveform.Pulse, 0, 5f, 0.3f, shape: 0.6f).A(3, Waveform.Sine, 12, 0f, 0.15f).A(4, Waveform.Triangle, 19, 2f, 0.08f).Off(A, 5, 6)
                .B(1, Waveform.Triangle, -12, 0f, 0.25f).B(2, Waveform.Sine, 12, -6f, 0.12f).Off(B, 3, 4, 5, 6)
                .Filter(A, 7000f).Filter(B, 5000f).Combine(0.05f).Eq(0f, 2f)
                .Lfo(1, 0.3f).Lfo(2, 0.23f)
                .Mod("LFO 1", ParamId.OscA1Shape, 0.25f).Mod("LFO 2", ParamId.OscA2Shape, 0.25f)
                .Resonator("Voices", 0.7f, 0f, on: false).Reverb("Plate", 0.45f, size: 0.8f)),

        new("Pad", "Long Breath", 207,
            "The Breath Flute sample stretched into one sustained breath that P1 transposes, with a quiet sine core under it. A long attack and a longer release.",
            p => p
                .Gated(2f, 7f).Pitch(1, 0f, midi: true, glide: 0.2f)
                .Sample(1, "Breath Flute.wav", rootNote: 62).Sampler(1, "Stretch", amp: 0.7f, dir: 0.5f, mod: "P1").Stretch(12f, "Medium")
                .A(1, Waveform.Sine, 0, 0f, 0.3f).A(2, Waveform.Sine, -12, 3f, 0.2f).Off(A, 3, 4, 5, 6).Silence(B)
                .Filter(A, 3000f).Combine(0f)
                .Drift(35f, 50f, 0.25f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.4f)),

        new("Pad", "Amber Keys", 208,
            "A reed-organ pad: the Reed Organ sample under two sections of soft pulses, with the resonator ringing a major-seventh chord that follows P1.",
            p => p
                .Gated(0.4f, 3f).Pitch(1, 0f, midi: true, glide: 0.05f)
                .Sample(1, "Reed Organ.wav", rootNote: 48).Sampler(1, amp: 0.45f, mod: "P1").Loop(0.05f, 0.95f, 120f)
                .A(1, Waveform.Pulse, -12, 0f, 0.3f, shape: 0.4f).A(2, Waveform.Triangle, 0, 4f, 0.25f).A(3, Waveform.Sine, -24, 0f, 0.3f).Off(A, 4, 5, 6)
                .B(1, Waveform.Pulse, -12, 7f, 0.2f, shape: 0.25f).B(2, Waveform.Sine, 7, 0f, 0.12f).Off(B, 3, 4, 5, 6)
                .Filter(A, 2800f).Filter(B, 2400f).Combine(0.05f)
                .Resonator("Major 7", 0.8f, 0.3f).Choose(ParamId.ResochordModSource, "P1").Reverb("Chamber", 0.35f)),
    ];
}
