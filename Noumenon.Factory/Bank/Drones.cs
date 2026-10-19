using Noumenon.Dsp.Oscillators;
using Noumenon.Engine.Parameters;
using static Noumenon.Engine.Parameters.SectionId;

namespace Noumenon.Factory.Bank;

/// <summary>The Drone family: patches that play themselves from the moment they load, each with its own slow lanes so the parts drift in and out of phase.</summary>
internal static partial class FactoryBank
{
    private static Recipe[] Drones =>
    [
        new("Drone", "Basalt", 101,
            "Dark and heavy: a sub and sine octaves under a slowly breathing low-pass, a fifth beating against itself in section B, a long cathedral tail.",
            p => p
                .A(1, Waveform.Sub, -24, 0f, 0.6f, shape: 0.6f).A(2, Waveform.Sine, -12, 2f, 0.35f).A(3, Waveform.Sine, -5, -3f, 0.25f).A(4, Waveform.Triangle, -12, 5f, 0.2f).Off(A, 5, 6)
                .B(1, Waveform.Sine, -17, 4f, 0.45f).B(2, Waveform.Sine, -17, -4f, 0.45f).B(3, Waveform.Triangle, -5, 1f, 0.2f).Off(B, 4, 5, 6)
                .Filter(A, 700f, 0.15f).Filter(B, 900f, 0.2f).Combine(0.25f)
                .MasterFilter(4000f).Drift(180f, 260f, 0.4f).Resonator("Fifths", 0.6f, 0.12f, pitch: -12)
                .Reverb("Cathedral", 0.45f, size: 0.85f, damping: 0.6f).Speed("0.5x")
                .Lane(ParamId.SectionACutoff, 24f, 0.12f).Lane(ParamId.SectionBCutoff, 31f, 0.12f).Lane(ParamId.SectionBLevel, 19f, 0.15f)
                .Lane(ParamId.OscA3Level, 27f, 0.2f).Lane(ParamId.MixCrossfade, 37f, 0.1f)),

        new("Drone", "Ashfall", 102,
            "Grey and grainy: detuned saws through a resonant low-pass whose cutoff wanders, the drift times gliding a few milliseconds so the whole field bends in pitch.",
            p => p
                .A(1, Waveform.Saw, -24, 0f, 0.4f).A(2, Waveform.Saw, -24, 9f, 0.35f).A(3, Waveform.Saw, -12, -7f, 0.3f).A(4, Waveform.Triangle, -5, 3f, 0.2f).Off(A, 5).A(6, Waveform.NoiseLp, 0, 0f, 0.06f, noiseCutoff: 900f)
                .B(1, Waveform.Saw, -17, -5f, 0.3f).B(2, Waveform.Saw, -17, 6f, 0.3f).B(3, Waveform.Sine, -24, 0f, 0.35f).Off(B, 4, 5, 6)
                .Filter(A, 1100f, 0.35f).Filter(B, 1500f, 0.25f).Combine(0.15f)
                .Distortion(0.15f, 0.3f).Drift(220f, 310f, 0.45f).Resonator("Minor", 0.75f, 0.2f, pitch: -12).Reverb("Hall", 0.4f, size: 0.75f)
                .Lane(ParamId.SectionACutoff, 29f, 0.15f).Lane(ParamId.SectionBCutoff, 23f, 0.15f)
                .Lane(ParamId.SpinTime1, 41f, 0.03f).Lane(ParamId.SpinTime2, 37f, 0.03f)),

        new("Drone", "Glass Horizon", 103,
            "High and glassy: the Odd Glass wavetable on four slots, each scanned by its own slow LFO, over a sine floor; a plate reverb keeps it airy.",
            p => p
                .Sample(10, "wavetables/Odd Glass.wav").Wavetable(10)
                .A(1, Waveform.Wavetable, 0, 0f, 0.35f, shape: 0.2f).A(2, Waveform.Wavetable, 7, 4f, 0.3f, shape: 0.5f).A(3, Waveform.Wavetable, 12, -3f, 0.25f, shape: 0.8f).A(4, Waveform.Sine, -12, 0f, 0.35f).Off(A, 5, 6)
                .B(1, Waveform.Sine, -24, 0f, 0.4f).B(2, Waveform.Sine, -17, 3f, 0.25f).B(3, Waveform.Wavetable, 19, 2f, 0.15f, shape: 0.4f).Off(B, 4, 5, 6)
                .Filter(A, 6000f).Filter(B, 3000f).Combine(0.1f).Eq(0f, 2f)
                .Lfo(1, 0.03f, "Smooth Random").Lfo(2, 0.05f).Lfo(3, 0.02f, "Drift")
                .Mod("LFO 1", ParamId.OscA1Shape, 0.3f).Mod("LFO 2", ParamId.OscA2Shape, 0.35f).Mod("LFO 3", ParamId.OscA3Shape, 0.3f)
                .Drift(90f, 140f, 0.3f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Plate", 0.45f, size: 0.8f, damping: 0.3f)
                .Lane(ParamId.SectionALevel, 33f, 0.12f).Lane(ParamId.OscB3Level, 21f, 0.2f)),

        new("Drone", "Undertow", 104,
            "Ring modulation pulling against the sum: two detuned sine groups meet in the A×B path, and the crossfade drifts between clean and metallic over most of a minute.",
            p => p
                .A(1, Waveform.Sine, -12, 0f, 0.5f).A(2, Waveform.Sine, -5, 2f, 0.4f).A(3, Waveform.Sine, 0, -3f, 0.3f).Off(A, 4, 5, 6)
                .B(1, Waveform.Sine, -12, 7f, 0.5f).B(2, Waveform.Sine, -2, -4f, 0.35f).B(3, Waveform.Triangle, 3, 0f, 0.2f).Off(B, 4, 5, 6)
                .Filter(A, 5000f).Filter(B, 4000f).Combine(0.55f, ring: 1.2f)
                .Drift(300f, 410f, 0.5f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Cloud", 0.5f, size: 0.9f)
                .Lane(ParamId.MixCrossfade, 47f, 0.3f).Lane(ParamId.SectionBCutoff, 26f, 0.2f)),

        new("Drone", "Lantern", 105,
            "Warm and consonant: triangles and sines on a just-intoned C major, the resonator's strings ringing above, a hall around it all.",
            p => p
                .Scale("Just Major")
                .A(1, Waveform.Triangle, -24, 0f, 0.45f).A(2, Waveform.Sine, -12, 0f, 0.4f).A(3, Waveform.Triangle, -8, 0f, 0.3f).A(4, Waveform.Sine, -5, 0f, 0.3f).A(5, Waveform.Sine, 4, 0f, 0.15f).Off(A, 6)
                .B(1, Waveform.Sine, -17, 0f, 0.35f).B(2, Waveform.Triangle, 0, 0f, 0.25f).B(3, Waveform.Sine, 7, 2f, 0.12f).Off(B, 4, 5, 6)
                .Filter(A, 3500f).Filter(B, 2500f).Combine(0.05f)
                .Resonator("Just Major", 0.85f, 0.3f).Reverb("Hall", 0.4f, size: 0.75f)
                .Lane(ParamId.SectionALevel, 35f, 0.1f).Lane(ParamId.OscA5Level, 23f, 0.3f).Lane(ParamId.OscB3Level, 29f, 0.3f).Lane(ParamId.ResochordMix, 41f, 0.15f)),

        new("Drone", "Monolith", 106,
            "A wall of octaves with fold-back distortion breathing in and out, while the master filter takes half a minute to open it from muffled to bright.",
            p => p
                .A(1, Waveform.Saw, -24, 0f, 0.45f).A(2, Waveform.Square, -12, 3f, 0.3f).A(3, Waveform.Saw, -36, 0f, 0.35f).A(4, Waveform.Pulse, -12, -4f, 0.25f, shape: 0.3f).Off(A, 5, 6)
                .B(1, Waveform.Saw, -24, 6f, 0.35f).B(2, Waveform.Saw, -12, -6f, 0.3f).B(3, Waveform.Sub, -24, 0f, 0.4f).Off(B, 4, 5, 6)
                .Filter(A, 2500f, 0.2f).Filter(B, 1800f).Combine(0.2f).Eq(2f, -2f)
                .MasterFilter(1500f, 0.25f).Distortion(0.35f, 0.4f, "Fold").Drift(60f, 85f, 0.2f)
                .Resonator("Voices", 0.7f, 0f, on: false).Reverb("Chamber", 0.3f)
                .Lane(ParamId.DistortionDrive, 27f, 0.25f).Lane(ParamId.DistortionMix, 33f, 0.15f).Lane(ParamId.MasterFilterCutoff, 31f, 0.25f)),

        new("Drone", "Saltmarsh", 107,
            "The Wind sample stretched thirty times over a low sine drone; the sampler's direction lane slows the gusts to a standstill and turns them back.",
            p => p
                .Sample(1, "Wind.wav").Sampler(1, "Stretch", amp: 0.6f, dir: 0.6f).Stretch(30f)
                .A(1, Waveform.Sine, -24, 0f, 0.4f).A(2, Waveform.Sine, -17, 3f, 0.3f).A(3, Waveform.Triangle, -12, -2f, 0.2f).Off(A, 4, 5, 6)
                .B(1, Waveform.Sine, -12, 5f, 0.3f).B(2, Waveform.Sine, -5, -4f, 0.2f).Off(B, 3, 4, 5, 6)
                .Level(A, 0.6f).Level(B, 0.6f).Filter(A, 1200f).Filter(B, 1600f).Combine(0.1f)
                .MasterFilter(6000f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Hall", 0.35f)
                .Lane(ParamId.SamplerDir, 45f, 0.2f).Lane(ParamId.SamplerAmp, 27f, 0.15f)),

        new("Drone", "Choir of Wires", 108,
            "The Choir Breath sample torn into soft grains and spread across the stereo field, over detuned sines on A; the grain position wanders through the recording.",
            p => p
                .Scale("Minor", "A")
                .Sample(1, "Choir Breath.wav").Sampler(1, "Granular", amp: 0.6f).Grains(180f, 14f, 0.3f, 0.25f, window: 0.5f, width: 0.8f)
                .A(1, Waveform.Sine, -15, 0f, 0.4f).A(2, Waveform.Sine, -3, 4f, 0.25f).A(3, Waveform.Sine, 4, -3f, 0.15f).Off(A, 4, 5, 6)
                .B(1, Waveform.Sine, -15, 6f, 0.35f).B(2, Waveform.Sine, -8, -2f, 0.2f).Off(B, 3, 4, 5, 6)
                .Filter(A, 3000f).Filter(B, 2500f).Combine(0.1f)
                .Resonator("Minor", 0.6f, 0.15f, pitch: -3).Reverb("Cathedral", 0.5f, size: 0.85f)
                .Lane(ParamId.GrainPosition, 39f, 0.3f).Lane(ParamId.GrainSize, 23f, 0.15f)),

        new("Drone", "Tidal Organ", 109,
            "Registrations from the Organ Stops wavetable, scanned by the slowest LFOs in the bank and heard through a closed door — an organ breathing in the next room.",
            p => p
                .Sample(10, "wavetables/Organ Stops.wav").Wavetable(10)
                .A(1, Waveform.Wavetable, -12, 0f, 0.45f, shape: 0.1f).A(2, Waveform.Wavetable, -5, 2f, 0.3f, shape: 0.5f).A(3, Waveform.Wavetable, -24, 0f, 0.35f, shape: 0.3f).Off(A, 4, 5, 6)
                .B(1, Waveform.Wavetable, 0, -3f, 0.25f, shape: 0.7f).B(2, Waveform.Sine, -24, 0f, 0.3f).Off(B, 3, 4, 5, 6)
                .Filter(A, 2200f).Filter(B, 1800f).Combine(0.05f)
                .Lfo(1, 0.015f).Lfo(2, 0.023f, "Triangle").Lfo(3, 0.011f, "Smooth Random")
                .Mod("LFO 1", ParamId.OscA1Shape, 0.4f).Mod("LFO 2", ParamId.OscA2Shape, 0.4f).Mod("LFO 3", ParamId.OscB1Shape, 0.35f)
                .Drift(25f, 40f, 0.1f).Resonator("Voices", 0.7f, 0f, on: false).Reverb("Chamber", 0.35f, size: 0.6f)
                .Lane(ParamId.SectionALevel, 43f, 0.15f).Lane(ParamId.SectionBLevel, 37f, 0.15f).Lane(ParamId.MasterFilterCutoff, 51f, 0.15f)),

        new("Drone", "Polar Night", 110,
            "Very slow and very dark: a minor drone that hardly moves, the resonator's deep minor chord ringing faintly inside a cloud of reverb.",
            p => p
                .A(1, Waveform.Sine, -24, 0f, 0.45f).A(2, Waveform.Sine, -21, 3f, 0.3f).A(3, Waveform.Sine, -17, -2f, 0.3f).A(4, Waveform.Triangle, -9, 0f, 0.15f).Off(A, 5, 6)
                .B(1, Waveform.Sine, -12, 4f, 0.3f).B(2, Waveform.Sine, -14, -3f, 0.2f).B(3, Waveform.NoiseLp, 0, 0f, 0.04f, noiseCutoff: 400f).Off(B, 4, 5, 6)
                .Filter(A, 900f).Filter(B, 700f).Combine(0.05f)
                .Resonator("Deep Minor", 0.9f, 0.2f).Reverb("Cloud", 0.55f, size: 1f, damping: 0.7f).Speed("0.5x", 0.6f)
                .Lane(ParamId.SectionACutoff, 53f, 0.1f).Lane(ParamId.SectionBLevel, 41f, 0.12f)),
    ];
}
