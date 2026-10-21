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

        new("Drone", "Ilgesys", 111,
            "Dark B minor, tuned a little flat (A4 = 436.3 Hz): a steady B3 with its octave and fifth, under a bass and an inner voice that swing between B and D every 6.9 seconds, each bass note swelling and dying away, while a breathy high melody wanders through a long, dark reverb.",
            p => p
                .Set(ParamId.TuningA4, 436.3f)
                .A(1, Waveform.Sine, -1, 0f, 0.32f).A(2, Waveform.Sine, 6, -4f, 0.06f).A(3, Waveform.Sine, 11, -1f, 0.05f).A(4, Waveform.Sine, 18, 2f, 0.025f)
                .A(5, Waveform.Sine, -25, -11f, 0.9f).A(6, Waveform.Sine, -6, -4f, 0.54f)
                .B(1, Waveform.Triangle, 32, 0f, 0.3f).B(2, Waveform.Triangle, 32, 9f, 0.064f).B(3, Waveform.Triangle, 32, -12f, 0.064f)
                .B(4, Waveform.NoiseLp, 0, 0f, 0.71f, noiseCutoff: 3000f).Off(B, 5, 6)
                .Filter(A, 4000f).Filter(B, 1650f, 0.7f, "BP").Combine(0f)
                .MasterFilter(1680f).Drift(15f, 15f, 0.1f).Set(ParamId.SpinLink, true).Resonator("Voices", 0.7f, 0f, on: false)
                .Reverb("Cloud", 0.24f, size: 0.95f, damping: 0.6f).Output(0f, 0.8f)
                .Steps(ParamId.OscA5Semitone, CavesCycle, -25f, -22f).Steps(ParamId.OscA6Semitone, CavesCycle, -6f, -10f).Steps(ParamId.OscA2Semitone, CavesCycle, 6f, 2f)
                .Curve(ParamId.OscA5Level, CavesCycle, phase => 0.9f * Swell(phase * CavesCycle % (CavesCycle / 2f), 1.2f, 6.3f))
                .Steps(ParamId.OscB1Semitone, CavesMelodySeconds, CavesMelody).Steps(ParamId.OscB2Semitone, CavesMelodySeconds, CavesMelody).Steps(ParamId.OscB3Semitone, CavesMelodySeconds, CavesMelody)
                .Steps(ParamId.SectionBCutoff, CavesMelodySeconds, CavesMelody.Select(note => NoteHz(note, 436.3f)).ToArray())
                .Curve(ParamId.SectionBLevel, CavesMelodySeconds, phase => 0.8f * Swell(phase * CavesMelodySeconds % (CavesMelodySeconds / CavesMelody.Length), 1.5f, 3f))),

        new("Drone", "Laumžirgis", 112,
            "A wide, glowing B minor cluster (B, D, E, F#, A, C#) on detuned saws with a resonant edge near 1 kHz, a stretched choir breathing inside it, a chorus that never settles and a sub on B1 that pulses slowly underneath.",
            p => p
                .Set(ParamId.TuningA4, 438.5f)
                .Sample(1, "Choir Breath.wav", rootNote: 57).Sampler(1, "Stretch", amp: 0.15f, dir: 0.5f, pitch: -1).Stretch(6f, "Medium")
                .A(1, Waveform.Saw, -13, -6f, 0.1f).A(2, Waveform.Saw, -10, 7f, 0.028f).A(3, Waveform.Saw, -8, -4f, 0.13f)
                .A(4, Waveform.Saw, -6, 9f, 0.22f).A(5, Waveform.Saw, -1, -8f, 0.14f).A(6, Waveform.Saw, 2, 5f, 0.078f)
                .B(1, Waveform.Sine, -25, -9f, 0.22f).B(2, Waveform.Sine, -25, 10f, 0.08f).B(3, Waveform.Saw, 6, -7f, 0.22f)
                .B(4, Waveform.Saw, 9, 6f, 0.15f).B(5, Waveform.Saw, 11, -3f, 0.17f).B(6, Waveform.Saw, 13, 8f, 0.17f)
                .Filter(A, 815f, 0.45f).Level(A, 1f).Filter(B, 1150f, 0.45f).Combine(0f)
                .MasterFilter(1665f, 0.2f).Eq(0f, -8f).Drift(14f, 14f, 0f, 0.5f).Set(ParamId.SpinLink, true).Resonator("Voices", 0.7f, 0f, on: false)
                .Reverb("Cloud", 0.23f, size: 0.9f, damping: 0.5f).Output(0f, 0.9f)
                .Curve(ParamId.SpinTime1, 24f, phase => 14f + 2f * MathF.Sin(2f * MathF.PI * 17f * phase))
                .Lane(ParamId.OscB3Level, 23f, 0.3f).Lane(ParamId.OscB4Level, 29f, 0.3f).Lane(ParamId.OscB5Level, 19f, 0.3f).Lane(ParamId.OscB6Level, 37f, 0.3f)
                .Lane(ParamId.OscA4Level, 31f, 0.25f).Lane(ParamId.OscA5Level, 41f, 0.25f)),

        new("Drone", "Gervės", 113,
            "A low B drone in a bed of slowed-down wind: B1 beating against itself, B2, and C#4 and A5 above, a suspended chord with no third, while now and then a gliding call rises out of the dark.",
            p => p
                .Set(ParamId.TuningA4, 441f)
                .Sample(1, "Wind.wav").Sampler(1, amp: 0.28f, pitch: -5)
                .A(1, Waveform.Sine, -25, 0f, 0.42f).A(2, Waveform.Sine, -25, 13f, 0.28f).A(3, Waveform.Triangle, -13, 5f, 0.12f)
                .A(4, Waveform.Sine, 1, 8f, 0.42f).A(5, Waveform.Sine, 6, -3f, 0.08f).A(6, Waveform.Sine, 21, 4f, 0.071f)
                .B(1, Waveform.Sine, 13, 2f, 0.04f).B(2, Waveform.Triangle, -24, 0f, 0.25f).B(3, Waveform.Sine, 11, 0f, 0.07f)
                .B(4, Waveform.Sine, 9, -5f, 0.05f).B(5, Waveform.NoiseLp, 0, 0f, 1f, noiseCutoff: 300f).B(6, Waveform.Triangle, -13, 12f, 0.155f)
                .Filter(A, 6000f).Filter(B, 1250f).Combine(0f)
                .MasterFilter(6400f).Eq(0f, -2f).Drift(20f, 20f, 0.2f).Set(ParamId.SpinLink, true).Resonator("Voices", 0.7f, 0f, on: false)
                .Reverb("Chamber", 0.3f, size: 0.85f).Output(0f, 1.5f)
                .Curve(ParamId.OscB2Semitone, CallSeconds, phase => CallPitch(phase * CallSeconds))
                .Curve(ParamId.OscB2Level, CallSeconds, phase => CallLevel(phase * CallSeconds))),

        new("Drone", "Svaigulys", 114,
            "A seasick C# minor pad: detuned saw chords whose pitch sways ±40 cents on a slow tape wow, step up a fourth and back on a 27.6-second loop, and brighten twice a chord as the filters snap open and close again, with a breath of tape hiss on top.",
            p => p
                .Set(ParamId.TuningA4, 439f)
                .A(1, Waveform.Saw, -16, 0f, 0.21f).A(2, Waveform.Saw, -14, 0f, 0.4f).A(3, Waveform.Saw, -11, 0f, 0.24f)
                .A(4, Waveform.Saw, -8, 0f, 0.32f).A(5, Waveform.Saw, -4, 0f, 0.34f).A(6, Waveform.Saw, 1, 0f, 0.055f)
                .B(1, Waveform.Saw, -2, 5f, 0.106f).B(2, Waveform.Saw, 4, -6f, 0.23f).B(3, Waveform.Saw, 8, 4f, 0.14f)
                .B(4, Waveform.Saw, -11, 12f, 0.13f).B(5, Waveform.Saw, -4, -10f, 0.17f).B(6, Waveform.Triangle, 20, 0f, 0.39f)
                .Filter(A, 900f).Filter(B, 900f).Combine(0f)
                .Sample(1, "Tape Hiss.wav").Sampler(1, amp: 0.01f)
                .MasterFilter(5000f).Drift(30f, 30f, 0f).Set(ParamId.SpinLink, true).Resonator("Voices", 0.7f, 0f, on: false)
                .Reverb("Cloud", 0.3f, size: 0.8f).Output(0f, 1f)
                .Pitch(1, 0f, glide: 0.05f).Steps(ParamId.P1Value, WowChordSeconds, 0f, 0f, 5f, 0f)
                .Curve(ParamId.SectionACutoff, WowChordSeconds, phase => Snap(phase * WowChordSeconds % (WowChordSeconds / 8f)))
                .Curve(ParamId.SectionBCutoff, WowChordSeconds, phase => Snap(phase * WowChordSeconds % (WowChordSeconds / 8f)))
                .Curve(ParamId.SpinTime1, 48.3f, phase => 30f + 24f * MathF.Sin(2f * MathF.PI * 10f * phase))),

        new("Drone", "Perkūno miegas", 115,
            "A huge, motionless drone on B minor: B1, D2 and F#2 and their octaves on saws under a very low low-pass, paired so slightly apart that the whole wall breathes every 2.6 seconds, with a little grit and air on top.",
            p => p
                .A(1, Waveform.Saw, -25, -5f, 0.18f).A(2, Waveform.Saw, -25, 5f, 1f).A(3, Waveform.Saw, -22, 8f, 0.36f)
                .A(4, Waveform.Saw, -22, -20f, 0.33f).A(5, Waveform.Saw, -18, 3f, 0.3f).A(6, Waveform.Saw, -13, 0f, 0.51f)
                .B(1, Waveform.Sine, -25, 0f, 0.063f).B(2, Waveform.Saw, -20, 0f, 0.06f).B(3, Waveform.Saw, 2, 0f, 0.042f)
                .B(4, Waveform.Saw, 4, 4f, 0.05f).B(5, Waveform.NoiseLp, 0, 0f, 0.04f, noiseCutoff: 12000f).B(6, Waveform.Saw, 6, -3f, 0.06f)
                .Filter(A, 147f, 0.15f).Level(A, 1f).Filter(B, 1930f).Level(B, 1f).Combine(0f)
                .MasterFilter(18000f).Distortion(0.1f, 0.3f).Drift(20f, 20f, 0.1f).Set(ParamId.SpinLink, true).Resonator("Voices", 0.7f, 0f, on: false)
                .Reverb("Chamber", 0.25f, size: 0.8f).Output(0f, 1.1f)),
    ];

    // Ilgesys: two chords of 6.9 s each, and a melody of eight notes (semitones from middle C) over 56 s.
    private const float CavesCycle = 13.8f;
    private const float CavesMelodySeconds = 56f;
    private static readonly float[] CavesMelody = [32f, 28f, 23f, 20f, 28f, 25f, 22f, 25f];

    // Gervės: the calls — start and length in seconds, then the pitch they rise from, peak at and end on (semitones from middle C).
    private const float CallSeconds = 29f;
    private const float CallRestPitch = -24f;
    private static readonly (float Start, float Length, float From, float Peak, float To)[] Calls =
        [(3f, 1.2f, -36f, -14f, -14f), (9f, 2f, -30f, -18f, -26f), (17f, 0.9f, -34f, -12f, -12f), (23f, 1.5f, -16f, -16f, -30f)];

    // Svaigulys: four chords of 6.9 s, the filters snapping open twice in each.
    private const float WowChordSeconds = 27.6f;

    /// <summary>A note's gain <paramref name="t"/> seconds after it began: a smoothstep rise over <paramref name="attack"/> seconds, then a fall of <paramref name="decayDbPerSecond"/> dB a second.</summary>
    private static float Swell(float t, float attack, float decayDbPerSecond)
    {
        if (t < attack)
        {
            var x = t / attack;

            return x * x * (3f - 2f * x);
        }

        return MathF.Pow(10f, -decayDbPerSecond * (t - attack) / 20f);
    }

    private static float NoteHz(float semitonesFromMiddleC, float a4) => a4 * MathF.Pow(2f, (semitonesFromMiddleC - 9f) / 12f);

    // A cutoff in Hz t seconds after the filters snap open: a quick bright spike over a slower fall back to 500 Hz.
    private static float Snap(float t) => 500f * (1f + 3f * MathF.Exp(-t / 0.6f) + 12f * MathF.Exp(-t / 0.12f));

    private static float CallPitch(float t)
    {
        foreach (var call in Calls)
        {
            if (t < call.Start - 0.2f || t > call.Start + call.Length + 0.2f)
                continue;

            var x = Math.Clamp((t - call.Start) / call.Length, 0f, 1f);
            var u = x < 0.5f ? 2f * x : 2f * x - 1f;
            u = u * u * (3f - 2f * u);

            return x < 0.5f ? call.From + (call.Peak - call.From) * u : call.Peak + (call.To - call.Peak) * u;
        }

        return CallRestPitch;
    }

    private static float CallLevel(float t)
    {
        foreach (var call in Calls)
        {
            if (t >= call.Start && t <= call.Start + call.Length)
                return 0.25f * MathF.Sin(MathF.PI * (t - call.Start) / call.Length);
        }

        return 0f;
    }
}
